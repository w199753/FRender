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
            var peelingCmd = CommandBufferPool.Get("Depth Peeling");
            //using (new ProfilingScope(peelingCmd, new ProfilingSampler("Depth Peeling")))
            {
                Profiler.BeginSample("Depth Peeling");
                peelingCmd.BeginSample("Depth Peeling");
                context.ExecuteCommandBuffer(peelingCmd);
                peelingCmd.Clear();
                List<int> colorRTs = new List<int>(settings.peelingDepth);
                List<int> depthRTs = new List<int>(settings.peelingDepth);
                for (int i = 0; i < settings.peelingDepth; i++)
                {
                    peelingCmd.SetGlobalInt(shaderPropertyID.depthRenderIndexID,i);
                    depthRTs.Add(Shader.PropertyToID($"_DepthPeelingDepth{i}"));
                    colorRTs.Add(Shader.PropertyToID($"_DepthPeelingColor{i}"));
                    peelingCmd.GetTemporaryRT(colorRTs[i], camera.pixelWidth, camera.pixelHeight, 0);
                    peelingCmd.GetTemporaryRT(depthRTs[i], camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Point, RenderTextureFormat.RFloat);
                    if(i>0)
                    {
                        peelingCmd.SetGlobalTexture(shaderPropertyID.depthRenderBufferID, depthRTs[i-1]);
                    }

                    peelingCmd.SetRenderTarget(new RenderTargetIdentifier[] { colorRTs[i], depthRTs[i] }, depthRTs[i]);
                    peelingCmd.ClearRenderTarget(true, true, Color.black);
                    
                    context.ExecuteCommandBuffer(peelingCmd);
                    peelingCmd.Clear();
                    context.DrawRenderers(renderingData.cullingResults, ref drawingSettings, ref filteringSettings, ref stateBlock);

                }
                var transparentMat = new Material(Shader.Find("Unlit/FRPTransparent"));
                for (var i = settings.peelingDepth - 1; i >= 0; i--)
                {
                    peelingCmd.SetGlobalTexture(shaderPropertyID.detphTextureID, depthRTs[i]);
                    peelingCmd.Blit(colorRTs[i], BuiltinRenderTextureType.CameraTarget, transparentMat, 3);
                    peelingCmd.ReleaseTemporaryRT(depthRTs[i]);
                    peelingCmd.ReleaseTemporaryRT(colorRTs[i]);
                }

                //var mat1 = new Material(Shader.Find("Unlit/SSAO"));
                //buffer.Blit(camera.targetTexture,BuiltinRenderTextureType.CameraTarget,mat1);
                context.ExecuteCommandBuffer(peelingCmd);
                peelingCmd.Clear();



                //int colorBufferID = Shader.PropertyToID("_ColorBuffer");
                //int detphBufferID = Shader.PropertyToID("_DepthBuffer");
                //RenderTargetIdentifier[] mrt = new RenderTargetIdentifier[2]{
                //    new RenderTargetIdentifier(colorBufferID),
                //    new RenderTargetIdentifier(detphBufferID),
                //};
                //int depthRenderBufferTagID = Shader.PropertyToID("_DepthRenderBuffer");
                //int depthRenderedIndexTagID = Shader.PropertyToID("_DepthRenderedIndex");
                //int finalBuffersTagID = Shader.PropertyToID("_FinalBuffers");
                //int finalDepthBuffersTagID = Shader.PropertyToID("_FinalDepthBuffers");
                //buffer.GetTemporaryRT(colorBufferID,camera.pixelWidth,camera.pixelHeight,0,FilterMode.Point,RenderTextureFormat.Default);
                //buffer.GetTemporaryRT(detphBufferID,camera.pixelWidth,camera.pixelHeight,0,FilterMode.Point,RenderTextureFormat.RFloat);
                //
                //buffer.GetTemporaryRT(depthRenderBufferTagID ,camera.pixelWidth,camera.pixelHeight,32,FilterMode.Point,RenderTextureFormat.RFloat);
                //
                //buffer.GetTemporaryRTArray(finalBuffersTagID,camera.pixelWidth,camera.pixelHeight,renderSettings.peelingDepth,0,FilterMode.Point,RenderTextureFormat.Default);
                //buffer.GetTemporaryRTArray(finalDepthBuffersTagID,camera.pixelWidth,camera.pixelHeight,renderSettings.peelingDepth,32,FilterMode.Point,RenderTextureFormat.RFloat);
                //
                //for(int i = 0 ;i<renderSettings.peelingDepth;i++)
                //{
                //    buffer.SetGlobalInt(depthRenderedIndexTagID,i);
                //    buffer.SetGlobalTexture(depthRenderBufferTagID,depthRenderBufferTagID);
                //    buffer.SetRenderTarget(mrt,detphBufferID);
                //    buffer.ClearRenderTarget(true,true,Color.black);
                //    context.ExecuteCommandBuffer(buffer);
                //    buffer.Clear();
                //    context.DrawRenderers(cullingResults,ref drawingSettings,ref filteringSettings,ref stateBlock);
                //    
                //    //buffer.Clear();
                //    buffer.Blit(mrt[1],depthRenderBufferTagID);
                //    buffer.CopyTexture(mrt[0],0,0,finalBuffersTagID,i,0);
                //    buffer.CopyTexture(mrt[1],0,0,finalDepthBuffersTagID,i,0);
                //    context.ExecuteCommandBuffer(buffer);
                //    buffer.Clear();
                //}
                //buffer.SetGlobalTexture(finalBuffersTagID,finalBuffersTagID);
                ////buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget);
                //buffer.SetGlobalInt(Shader.PropertyToID("_MaxDepth"),renderSettings.peelingDepth);
                ////buffer.SetGlobalTexture(Shader.PropertyToID("_MainTex"),BuiltinRenderTextureType.CameraTarget);
                //var mat = new Material(Shader.Find("Unlit/FRPTransparent"));
                ////buffer.Blit(null,BuiltinRenderTextureType.CameraTarget,mat,3);
                //for(int i = renderSettings.peelingDepth-1;i>=0;i--)
                //{
                //    buffer.SetGlobalInt(Shader.PropertyToID("_Test"),i);
                //    buffer.Blit(null,BuiltinRenderTextureType.CameraTarget,mat,3);
                //    buffer.ReleaseTemporaryRT(colorBufferID);
                //    buffer.ReleaseTemporaryRT(detphBufferID);
                //}
                ////buffer.Blit(null, BuiltinRenderTextureType.CameraTarget,mat,3);
                ////buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget);
                //context.ExecuteCommandBuffer(buffer);
                //buffer.Clear();
                peelingCmd.EndSample("Depth Peeling");
                context.ExecuteCommandBuffer(peelingCmd);
                peelingCmd.Clear();
                Profiler.EndSample();
            }
            CommandBufferPool.Release(peelingCmd);
        }

    }
}
