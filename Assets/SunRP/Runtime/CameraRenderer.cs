using UnityEngine;
using UnityEngine.Rendering;

namespace SunRP.Runtime
{
    public partial class CameraRenderer
    {
        private ScriptableRenderContext _context;
        private Camera _camera;

        private const string bufferName = "Render Camera";
        private CommandBuffer _buffer = new CommandBuffer { name = bufferName };

        private CullingResults _cullingResults;
        private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
        private static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");
        
        private readonly Lighting _lighting = new Lighting();

#if UNITY_EDITOR
        private string SampleName { get; set; }
#else
        private const string SampleName = bufferName;
#endif
        
        public void Render(ScriptableRenderContext context, Camera camera,bool useDynamicBatching,bool useGPUInstancing,ShadowSettings shadowSettings)
        {
            _context = context;
            _camera = camera;
            PrepareBuffer();
            PrepareForSceneWindow();
            if (!Cull(shadowSettings.maxDistance))
            {
                return;
            }
            _buffer.BeginSample(SampleName);
            ExecuteBuffer();
            _lighting.Setup(context,_cullingResults,shadowSettings);
            _buffer.EndSample(SampleName);
            Setup();
            DrawVisibleGeometry(useDynamicBatching,useGPUInstancing);
            DrawUnsupportedShaders();
            DrawGizmos();
            _lighting.Cleanup();
            Submit();
        }


        private void DrawVisibleGeometry(bool useDynamicBatching,bool useGPUInstancing)
        {
            var sortingSettings = new SortingSettings(_camera) { criteria = SortingCriteria.CommonOpaque };
            var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
            {
                enableDynamicBatching = useDynamicBatching,
                enableInstancing = useGPUInstancing,
                perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume
            };
            drawingSettings.SetShaderPassName(1,litShaderTagId);
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);

            _context.DrawSkybox(_camera);

            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;
            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }
        
        
        private bool Cull(float maxShadowDistance)
        {
            if (_camera.TryGetCullingParameters(out var p))
            {
                p.shadowDistance = Mathf.Min(maxShadowDistance,_camera.farClipPlane) ;
                _cullingResults = _context.Cull(ref p);
                return true;
            }

            return false;
        }

        private void Setup()
        {
            _context.SetupCameraProperties(_camera);
            var flags = _camera.clearFlags;
            
            _buffer.ClearRenderTarget(  flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color,flags == CameraClearFlags.Color ? _camera.backgroundColor.linear : Color.clear);
            _buffer.BeginSample(SampleName);
            ExecuteBuffer();
        }

        private void Submit()
        {
            _buffer.EndSample(SampleName);
            ExecuteBuffer();
            _context.Submit();
        }

        private void ExecuteBuffer()
        {
            _context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }
        
        partial void DrawGizmos();
        partial void DrawUnsupportedShaders();

        partial void PrepareForSceneWindow();

        partial void PrepareBuffer();

    }
}