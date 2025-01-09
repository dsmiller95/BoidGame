using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Rendering
{
    public class FullScreenSDFPass : ScriptableRenderPass
    {
        private string _profilerTag;
        private Material _sdfMaterial;
        
        private static readonly int horizontalBlurId = Shader.PropertyToID("_HorizontalBlur");
        private static readonly int verticalBlurId = Shader.PropertyToID("_VerticalBlur");
        private const string k_BlurTextureName = "_BlurTexture";
        private const string k_SdfPassName = "SdfRenderPass";
        private const string k_HorizontalPassName = "HorizontalBlurRenderPass";
    
        // Optionally, track the pass event, e.g. "AfterRendering"
        public FullScreenSDFPass(
            string profilerTag,
            RenderPassEvent renderPassEvent,
            Material sdfMaterial)
        {
            this._profilerTag = profilerTag;
            this.renderPassEvent = renderPassEvent;
            this._sdfMaterial = sdfMaterial;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            base.Execute(context, ref renderingData);
        }
        
        

        public override void RecordRenderGraph(
            RenderGraph renderGraph,
            ContextContainer frameData)
        {
            // using var builder = renderGraph.AddRast1
            
        }
    }
}