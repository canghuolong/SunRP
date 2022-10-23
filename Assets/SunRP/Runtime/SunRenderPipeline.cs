using UnityEngine;
using UnityEngine.Rendering;

namespace SunRP.Runtime
{
    public class SunRenderPipeline : RenderPipeline
    {
        private readonly CameraRenderer _cameraRenderer = new CameraRenderer();

        private bool _useDynamicBatching;
        private bool _useGPUInstancing;
        private ShadowSettings _shadowSettings;
        public SunRenderPipeline(bool useDynamicBatching,bool useGPUInstancing,bool useSRPBatcher,ShadowSettings shadows)
        {
            _useDynamicBatching = useDynamicBatching;
            _useGPUInstancing = useGPUInstancing;
            GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
            GraphicsSettings.lightsUseLinearIntensity = true;
            _shadowSettings = shadows;
        }
        
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (var cam in cameras)
            {
                _cameraRenderer.Render(context,cam,_useDynamicBatching,_useGPUInstancing,_shadowSettings);
            }
        }
    }
}
