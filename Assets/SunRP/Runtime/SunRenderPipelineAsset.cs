using UnityEngine;
using UnityEngine.Rendering;

namespace SunRP.Runtime
{
    [CreateAssetMenu(menuName = "SunRP/Sun Render Pipeline")]
    public class SunRenderPipelineAsset : RenderPipelineAsset
    {
        [SerializeField] private bool useDynamicBatching = true;
        [SerializeField] private bool useGPUInstancing = true;
        [SerializeField] private bool useSRPBatcher = true;

        [SerializeField] private ShadowSettings shadows = default;
        
        protected override RenderPipeline CreatePipeline()
        {
            return new SunRenderPipeline(useDynamicBatching,useGPUInstancing,useSRPBatcher,shadows);
        }
    }
}
