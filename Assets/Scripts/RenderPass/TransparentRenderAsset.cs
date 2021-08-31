using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace frp
{
    [CreateAssetMenu(fileName = "TransparentRenderAsset", menuName = "FRP/RenderPass/transparentPass")]
    public class TransparentRenderAsset : FRenderPassAsset
    {
        public override FRenderPass CreateRenderPass()
        {
            return new TransparentRenderPass(this);
        }
    }

    public class TransparentRenderPass : FRenderPassRender<TransparentRenderAsset>
    {
        private const string BUFFER_NAME = "TransparentRender";
        FRPRenderSettings settings;
        CommandBuffer buffer = new CommandBuffer() { name = BUFFER_NAME };

        ShaderTagId transParentShaderPass1TagID = new ShaderTagId("FRP_TRANS_NORMAL");
        ShaderTagId transParentShaderPass2TagID = new ShaderTagId("FRP_TRANS_NORMAL1");

        ShaderTagId depthPeelingPass1TagID = new ShaderTagId("FRP_TRANS_DEPTH_PEELING");
        ShaderTagId depthPeelingPass2TagID = new ShaderTagId("FRP_TRANS_DEPTH_PEELING1");
        RenderTargetIdentifier[] mrtID = new RenderTargetIdentifier[2];
        Material transparentMat ;
        private class ShaderPropertyID
        {
            public int depthRenderBufferID;
            public int depthRenderIndexID;
            public int detphTextureID;
            public ShaderPropertyID()
            {
                depthRenderBufferID = Shader.PropertyToID("_DepthRenderBuffer");
                depthRenderIndexID = Shader.PropertyToID("_DepthRenderedIndex");
                detphTextureID = Shader.PropertyToID("_DepthTex");
            }
        }
        private readonly ShaderPropertyID shaderPropertyID = new ShaderPropertyID();
        public TransparentRenderPass(TransparentRenderAsset asset) : base(asset)
        {

        }
        public override void Render()
        {
            buffer.BeginSample(BUFFER_NAME);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();

            settings = renderingData.settings;
            buffer.SetRenderTarget(renderingData.ColorTarget, renderingData.DepthTarget);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
            DrawTransparentRenders();

            buffer.EndSample(BUFFER_NAME);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        private void DrawTransparentRenders()
        {
            if (settings.useDepthPeeling == false)
            {
                RenderTransparentByNormal();
            }
            else
            {
                RenderTransparentByDepthPeeling();
            }
        }

        private void RenderTransparentByNormal()
        {
            SortingSettings sortingSettings = new SortingSettings(camera);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
            DrawingSettings drawingSettings = new DrawingSettings(transParentShaderPass1TagID, sortingSettings);

            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            drawingSettings.SetShaderPassName(0, transParentShaderPass1TagID);
            drawingSettings.SetShaderPassName(1, transParentShaderPass2TagID);
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;
            RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            //stateBlock.depthState = new DepthState(true,CompareFunction.Greater);
            context.DrawRenderers(renderingData.cullingResults, ref drawingSettings, ref filteringSettings, ref stateBlock);
        }

        private void RenderTransparentByDepthPeeling()
        {
            SortingSettings sortingSettings = new SortingSettings(camera);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            DrawingSettings drawingSettings = new DrawingSettings(depthPeelingPass1TagID, sortingSettings);
            drawingSettings.SetShaderPassName(0, depthPeelingPass1TagID);
            //drawingSettings.SetShaderPassName(1,transParentShaderPass2TagID);
            //filteringSettings.renderQueueRange = RenderQueueRange.transparent;
            RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            stateBlock.mask = RenderStateMask.Depth;
            DepthState ddd =  new DepthState();
            ddd.writeEnabled = true;
            ddd.compareFunction = CompareFunction.LessEqual;
            stateBlock.depthState = ddd;
            var peelingCmd = CommandBufferPool.Get("Depth Peeling");
            //using (new ProfilingScope(peelingCmd, new ProfilingSampler("Depth Peeling")))
            {
                Profiler.BeginSample("Depth Peeling");
                peelingCmd.BeginSample("Depth Peeling");
                context.ExecuteCommandBuffer(peelingCmd);
                peelingCmd.Clear();
                List<int> colorRTs = new List<int>(settings.peelingDepth);
                List<int> depthRTs = new List<int>(settings.peelingDepth);
                peelingCmd.SetGlobalTexture("_CameraDepthTex",renderingData.DepthTarget);
                for (int i = 0; i < settings.peelingDepth; i++)
                {
                    peelingCmd.SetGlobalInt(shaderPropertyID.depthRenderIndexID,i);
                    depthRTs.Add(Shader.PropertyToID($"_DepthPeelingDepth{i}"));
                    colorRTs.Add(Shader.PropertyToID($"_DepthPeelingColor{i}"));
                    peelingCmd.GetTemporaryRT(colorRTs[i], camera.pixelWidth, camera.pixelHeight, 0);
                    peelingCmd.GetTemporaryRT(depthRTs[i], camera.pixelWidth, camera.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.RFloat);
                    if(i>0)
                    {
                        peelingCmd.SetGlobalTexture(shaderPropertyID.depthRenderBufferID, depthRTs[i-1]);
                    }
                    mrtID[0] = colorRTs[i];
                    mrtID[1] = depthRTs[i];
                    peelingCmd.SetRenderTarget(mrtID, depthRTs[i]);
                    peelingCmd.ClearRenderTarget(true, true, Color.black);
                    
                    
                    context.ExecuteCommandBuffer(peelingCmd);
                    peelingCmd.Clear();
                    context.DrawRenderers(renderingData.cullingResults, ref drawingSettings, ref filteringSettings, ref stateBlock);

                }
                peelingCmd.SetRenderTarget(renderingData.ColorTarget, renderingData.DepthTarget);
                if(transparentMat == null) transparentMat = MaterialPool.GetMaterial("Unlit/FRPTransparent");
                for (var i = settings.peelingDepth - 1; i >= 0; i--)
                {
                    peelingCmd.SetGlobalTexture(shaderPropertyID.detphTextureID, depthRTs[i]);
                    peelingCmd.Blit(colorRTs[i], renderingData.ColorTarget, transparentMat, 3);
                    peelingCmd.ReleaseTemporaryRT(depthRTs[i]);
                    peelingCmd.ReleaseTemporaryRT(colorRTs[i]);
                }
                peelingCmd.SetRenderTarget(renderingData.ColorTarget, renderingData.DepthTarget);

                context.ExecuteCommandBuffer(peelingCmd);
                peelingCmd.Clear();

                peelingCmd.EndSample("Depth Peeling");
                context.ExecuteCommandBuffer(peelingCmd);
                peelingCmd.Clear();
                Profiler.EndSample();
            }
            CommandBufferPool.Release(peelingCmd);
        }

    }
}
