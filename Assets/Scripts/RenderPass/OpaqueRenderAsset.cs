using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace frp
{
    [CreateAssetMenu(fileName = "OpaqueRenderAsset", menuName = "FRP/RenderPass/opaquePass")]
    public class OpaqueRenderAsset : FRenderPassAsset
    {
        public override FRenderPass CreateRenderPass()
        {
            return new OpaqueRenderPass(this);
        }
    }

    public class OpaqueRenderPass : FRenderPassRender<OpaqueRenderAsset>
    {
        private const string BUFFER_NAME = "ObjectRender";
        private const string FRP_BASE = "FRP_BASE";
        ShaderTagId baseShaderTagID = new ShaderTagId(FRP_BASE);
        CommandBuffer buffer = new CommandBuffer() { name = BUFFER_NAME };

        public OpaqueRenderPass(OpaqueRenderAsset asset) : base(asset)
        {

        }
        public override void Render()
        {
            buffer.BeginSample(BUFFER_NAME);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
            
            buffer.ClearRenderTarget(true,true,Color.clear);
            // int test = Shader.PropertyToID("hhh");
            // buffer.GetTemporaryRT(test,1024,1024,32,FilterMode.Bilinear,UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
            // buffer.SetRenderTarget(test);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
            DrawOpaqueRenders();

            buffer.EndSample(BUFFER_NAME);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        private void DrawOpaqueRenders()
        {
            SortingSettings sortingSettings = new SortingSettings(camera);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
            DrawingSettings drawingSettings = new DrawingSettings(baseShaderTagID, sortingSettings);
            RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.opaque;
            drawingSettings.perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.ReflectionProbes;
            context.DrawRenderers(renderingData.cullingResults, ref drawingSettings, ref filteringSettings, ref stateBlock);
        }
        public override void Cleanup()
        {

        }
    }

}
