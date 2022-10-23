using UnityEngine;
using UnityEngine.Rendering;

namespace SunRP.Runtime
{
    public class Shadows
    {
        private struct ShadowedDirectionalLight
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float nearPlaneOffset;
        }
        
        private const string bufferName = "Shadows";
        private const int maxShadowedDirectionalLightCount = 4;
        private const int maxCascades = 4;
        private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
        private static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
        private static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
        private static int cascadeCullingSphereId = Shader.PropertyToID("_CascadeCullingSpheres");
        private static int cascadeDataId = Shader.PropertyToID("_CascadeData");
        private static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
        private static int shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");
        

        private static string[] directionalFilterKeywords =
        {
            "_DIRECTIONAL_PCF3",
            "_DIRECTIONAL_PCF5",
            "_DIRECTIONAL_PCF7",
        };

        private static string[] cascadeBlendKeywords =
        {
            "_CASCADE_BLEND_SOFT",
            "_CASCADE_BLEND_DITHER",
        };

        private static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount*maxCascades];

        private static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];
        private static Vector4[] cascadeData = new Vector4[maxCascades];

        private CommandBuffer _buffer = new CommandBuffer() { name = bufferName };

        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
        private ShadowSettings _shadowSettings;

        private int ShadowedDirectionalLightCount;
        
        private ShadowedDirectionalLight[] ShadowedDirectionalLights =
            new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

        public void Setup(ScriptableRenderContext context,CullingResults cullingResults,ShadowSettings settings)
        {
            _context = context;
            _cullingResults = cullingResults;
            _shadowSettings = settings;
            ShadowedDirectionalLightCount = 0;
        }

        public Vector3 ReserveDirectionalShadows(Light light, int visibleLightIndex)
        {
            if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount && light.shadows != LightShadows.None && light.shadowStrength > 0f &&
                _cullingResults.GetShadowCasterBounds(visibleLightIndex,out var b))
            {
                ShadowedDirectionalLights[ShadowedDirectionalLightCount] = new ShadowedDirectionalLight()
                {
                    visibleLightIndex = visibleLightIndex ,
                    slopeScaleBias = light.shadowBias,
                    nearPlaneOffset = light.shadowNearPlane,
                };
                return new Vector3(light.shadowStrength,_shadowSettings.directional.cascadeCount * ShadowedDirectionalLightCount++,light.shadowNormalBias);
            }
            return Vector3.zero;
        }
        
        private void ExecuteBuffer()
        {
            _context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }

        public void Render()
        {
            if (ShadowedDirectionalLightCount > 0)
            {
                RenderDirectionalShadows();
            }
        }

        public void Cleanup()
        {
            if (ShadowedDirectionalLightCount > 0)
            {
                _buffer.ReleaseTemporaryRT(dirShadowAtlasId);
                ExecuteBuffer();
            }
        }

        private void RenderDirectionalShadows(int index,int split, int tileSize)
        {
            ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
            var shadowSettings = new ShadowDrawingSettings(_cullingResults, light.visibleLightIndex);

            int cascadeCount = _shadowSettings.directional.cascadeCount;
            int tileOffset = index * cascadeCount;
            Vector3 ratios = _shadowSettings.directional.CascadeRatios;

            float cullingFactor = 1f - _shadowSettings.directional.cascadeFade;
            
            for (int i = 0; i < cascadeCount; i++)
            {
                _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, i,cascadeCount,
                    ratios, tileSize, light.nearPlaneOffset,
                    out var viewMatrix, out var projectionMatrix, out ShadowSplitData splitData);
                splitData.shadowCascadeBlendCullingFactor = cullingFactor;
                shadowSettings.splitData = splitData;

                if (index == 0)
                {
                    SetCascadeData(i,splitData.cullingSphere,tileSize);
                }
                
                int tileIndex = tileOffset + i;
                dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix,SetTileViewport(tileIndex,split,tileSize),split);
                _buffer.SetViewProjectionMatrices(viewMatrix,projectionMatrix);
                _buffer.SetGlobalDepthBias(0f,light.slopeScaleBias);
                ExecuteBuffer();
                _context.DrawShadows(ref shadowSettings);
                _buffer.SetGlobalDepthBias(0f,0f);
            }
        }
        private void RenderDirectionalShadows()
        {
            SetKeywords(directionalFilterKeywords,(int)_shadowSettings.directional.filter-1 );
            SetKeywords(cascadeBlendKeywords,(int)_shadowSettings.directional.cascadeBlendMode-1);
            
            int atlasSize = (int)_shadowSettings.directional.atlasSize;
            int tiles = ShadowedDirectionalLightCount * _shadowSettings.directional.cascadeCount;
            
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            
            int tileSize = atlasSize / split;
            _buffer.GetTemporaryRT(dirShadowAtlasId,atlasSize,atlasSize,32,UnityEngine.FilterMode.Bilinear,RenderTextureFormat.Shadowmap);
            _buffer.SetRenderTarget(dirShadowAtlasId,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
            _buffer.ClearRenderTarget(true,false,Color.clear);
            
            _buffer.BeginSample(bufferName);
            ExecuteBuffer();
            for (int i = 0; i < ShadowedDirectionalLightCount; i++)
            {
                RenderDirectionalShadows(i, split,tileSize);
            }

            float f = 1f - _shadowSettings.directional.cascadeFade;
            _buffer.SetGlobalVector(shadowDistanceFadeId,new Vector4(1f/_shadowSettings.maxDistance,1/_shadowSettings.distanceFade,1f/(1f-f*f)));
            _buffer.SetGlobalInt(cascadeCountId,_shadowSettings.directional.cascadeCount);
            _buffer.SetGlobalVectorArray(cascadeCullingSphereId,cascadeCullingSpheres);
            _buffer.SetGlobalVectorArray(cascadeDataId,cascadeData);
            _buffer.SetGlobalMatrixArray(dirShadowMatricesId,dirShadowMatrices);
            
            _buffer.SetGlobalVector(shadowAtlasSizeId,new Vector4(atlasSize,1f/atlasSize));
            _buffer.EndSample(bufferName);
            ExecuteBuffer();
        }

        private void SetKeywords(string[] keywords,int enableIndex)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                if (i == enableIndex)
                {
                    _buffer.EnableShaderKeyword(keywords[i]);
                }
                else
                {
                    _buffer.DisableShaderKeyword(keywords[i]);
                }
            }
        }

        private Vector2 SetTileViewport(int index, int split, float tileSize)
        {
            Vector2 offset = new Vector2(index % split, index / split);
            _buffer.SetViewport(new Rect(offset.x * tileSize,offset.y * tileSize,tileSize,tileSize));
            return offset;
        }

        private void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
        {
            float texelSize = 2f * cullingSphere.w / tileSize;
            cullingSphere.w *= cullingSphere.w;
            cascadeCullingSpheres[index] = cullingSphere;
            
            cascadeData[index] = new Vector4(1f / cullingSphere.w,texelSize*1.4142136f) ;
        }
        
        private Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                m.m20 = -m.m20;
                m.m21 = -m.m21;
                m.m22 = -m.m22;
                m.m23 = -m.m23;
            }

            float scale = 1f / split;
            m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
            m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
            m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
            m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
            m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
            m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
            m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
            m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
            m.m20 = 0.5f * (m.m20 + m.m30);
            m.m21 = 0.5f * (m.m21 + m.m31);
            m.m22 = 0.5f * (m.m22 + m.m32);
            m.m23 = 0.5f * (m.m23 + m.m33);
            return m;
        }
    }
}