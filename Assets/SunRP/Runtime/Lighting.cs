using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace SunRP.Runtime
{
    public class Lighting
    {
        private const string bufferName = "Lighting";
        private const int maxDirLightCount = 4;
        
        private CommandBuffer _buffer = new CommandBuffer() { name = bufferName };

        private static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
        private static int dirLightColorId = Shader.PropertyToID("_DirectionalLightColors");
        private static int dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirections");
        private static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

        private static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
        private static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];
        private static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];

        private CullingResults _cullingResults;

        private Shadows _shadows = new Shadows();
        public void Setup(ScriptableRenderContext context,CullingResults cullingResults,ShadowSettings shadowSettings)
        {
            _cullingResults = cullingResults;
            _buffer.BeginSample(bufferName);
            _shadows.Setup(context,cullingResults,shadowSettings);
            SetupLights();
            _shadows.Render();
            _buffer.EndSample(bufferName);
            context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }

        void SetupLights()
        {
            NativeArray<VisibleLight> visibleLights = _cullingResults.visibleLights;
            int dirLightCount = 0;
            for (int i = 0; i < visibleLights.Length; i++)
            {
                VisibleLight visibleLight = visibleLights[i];
                if (visibleLight.lightType == LightType.Directional)
                {
                    SetupDirectionalLight(dirLightCount++,ref visibleLight);
                    if (dirLightCount >= maxDirLightCount)
                    {
                        break;
                    }
                }
            }
            _buffer.SetGlobalInt(dirLightCountId,dirLightCount);
            _buffer.SetGlobalVectorArray(dirLightColorId,dirLightColors);
            _buffer.SetGlobalVectorArray(dirLightDirectionId,dirLightDirections);
            _buffer.SetGlobalVectorArray(dirLightShadowDataId,dirLightShadowData);
        }

        public void Cleanup()
        {
            _shadows.Cleanup();
        }
        private void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
        {
            dirLightColors[index] = visibleLight.finalColor;
            dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
            dirLightShadowData[index] =_shadows.ReserveDirectionalShadows(visibleLight.light,index);
        }

    }
}