using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace frp
{
    public abstract class FRenderBase
    {
        protected FRenderResource m_renderResource;
        public abstract void ExecuteRender(ref ScriptableRenderContext renderContext, CullingResults cullingResults, Camera camera);
        public abstract void DisposeRender(bool disposing);

        public void AllocateResources(FRenderResource resource)
        {
            m_renderResource = resource;
        }
    }
    public class CommonRender : FRenderBase
    {
        private const string BUFFER_NAME = "CommonRender";
        CommandBuffer _cmd = new CommandBuffer() { name = BUFFER_NAME };
        public override void DisposeRender(bool disposing)
        {
            if (disposing)
            {

            }
        }

        public override void ExecuteRender(ref ScriptableRenderContext renderContext, CullingResults cullingResults, Camera camera)
        {
            _cmd.BeginSample(BUFFER_NAME);
            _cmd.ClearRenderTarget(true, true, Color.clear);
            renderContext.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();

            //draw skybox
            renderContext.DrawSkybox(camera);


            _cmd.EndSample(BUFFER_NAME);
            renderContext.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }
    }

    public class ObjectRender : FRenderBase
    {
        private const string BUFFER_NAME = "ObjectRender";
        private const string FRP_BASE = "FRP_BASE";
        private FRPRenderSettings renderSettings;
        ShaderTagId baseShaderTagID = new ShaderTagId(FRP_BASE);

        ShaderTagId transParentShaderPass1TagID = new ShaderTagId("FRP_TRANS_NORMAL");
        ShaderTagId transParentShaderPass2TagID = new ShaderTagId("FRP_TRANS_NORMAL1");
        
        ShaderTagId depthPeelingPass1TagID = new ShaderTagId("FRP_TRANS_DEPTH_PEELING");
        ShaderTagId depthPeelingPass2TagID = new ShaderTagId("FRP_TRANS_DEPTH_PEELING1");
        CommandBuffer _cmd = new CommandBuffer() { name = BUFFER_NAME };
        public void SetupRenderSettings(FRPRenderSettings settings)
        {
            renderSettings = settings;
        }
        public override void DisposeRender(bool disposing)
        {
            if (disposing)
            {

            }
        }

        public override void ExecuteRender(ref ScriptableRenderContext renderContext, CullingResults cullingResults, Camera camera)
        {
            _cmd.BeginSample(BUFFER_NAME);
            renderContext.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();

            DrawOpaqueRenders(ref renderContext, cullingResults, camera);
            DrawTransparentRenders(ref renderContext, cullingResults, camera);

            _cmd.EndSample(BUFFER_NAME);
            renderContext.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }

        private void DrawOpaqueRenders(ref ScriptableRenderContext renderContext, CullingResults cullingResults, Camera camera)
        {
            SortingSettings sortingSettings = new SortingSettings(camera);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
            DrawingSettings drawingSettings = new DrawingSettings(baseShaderTagID, sortingSettings);

            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.opaque;
            drawingSettings.perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.ReflectionProbes;
            renderContext.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        }

        private void DrawTransparentRenders(ref ScriptableRenderContext renderContext, CullingResults cullingResults, Camera camera)
        {
            if (renderSettings.useDepthPeeling == false)
            {
                RenderTransparentByNormal(ref renderContext,cullingResults,camera);
            }
            else
            {
                RenderTransparentByDepthPeeling(ref renderContext,cullingResults,camera);
            }
        }

        private void RenderTransparentByNormal(ref ScriptableRenderContext renderContext, CullingResults cullingResults, Camera camera)
        {
            SortingSettings sortingSettings = new SortingSettings(camera);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
            DrawingSettings drawingSettings = new DrawingSettings(transParentShaderPass1TagID, sortingSettings);

            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            drawingSettings.SetShaderPassName(0,transParentShaderPass1TagID);
            drawingSettings.SetShaderPassName(1,transParentShaderPass2TagID);
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;
            RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            //stateBlock.depthState = new DepthState(true,CompareFunction.Greater);
            renderContext.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings,ref stateBlock);
        }

        private void RenderTransparentByDepthPeeling(ref ScriptableRenderContext renderContext, CullingResults cullingResults, Camera camera)
        {
            SortingSettings sortingSettings = new SortingSettings(camera);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
            DrawingSettings drawingSettings = new DrawingSettings(depthPeelingPass1TagID, sortingSettings);

            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            drawingSettings.SetShaderPassName(0,depthPeelingPass1TagID);
            //drawingSettings.SetShaderPassName(1,transParentShaderPass2TagID);
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;
            RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            using (new ProfilingScope(_cmd,new ProfilingSampler("Depth Peeling")))
            {
                renderContext.ExecuteCommandBuffer(_cmd);
                _cmd.Clear();
                
                int colorBufferID = Shader.PropertyToID("_ColorBuffer");
                int detphBufferID = Shader.PropertyToID("_DepthBuffer");
                RenderTargetIdentifier[] mrt = new RenderTargetIdentifier[2]{
                    new RenderTargetIdentifier(colorBufferID),
                    new RenderTargetIdentifier(detphBufferID),
                };
                int depthRenderBufferTagID = Shader.PropertyToID("_DepthRenderBuffer");
                int depthRenderedIndexTagID = Shader.PropertyToID("_DepthRenderedIndex");
                int finalBuffersTagID = Shader.PropertyToID("_FinalBuffers");
                _cmd.GetTemporaryRT(colorBufferID,camera.pixelWidth,camera.pixelHeight,0,FilterMode.Point,RenderTextureFormat.Default);
                _cmd.GetTemporaryRT(detphBufferID,camera.pixelWidth,camera.pixelHeight,0,FilterMode.Point,RenderTextureFormat.RFloat);

                _cmd.GetTemporaryRT(depthRenderBufferTagID ,camera.pixelWidth,camera.pixelHeight,32,FilterMode.Point,RenderTextureFormat.RFloat);
                
                _cmd.GetTemporaryRTArray(finalBuffersTagID,camera.pixelWidth,camera.pixelHeight,renderSettings.peelingDepth,0,FilterMode.Point,RenderTextureFormat.Default);
                
                for(int i = 0 ;i<renderSettings.peelingDepth;i++)
                {
                    _cmd.SetGlobalInt(depthRenderedIndexTagID,i);
                    _cmd.SetGlobalTexture(depthRenderBufferTagID,depthRenderBufferTagID);
                    _cmd.SetRenderTarget(mrt,detphBufferID);
                    _cmd.ClearRenderTarget(true,true,Color.black);
                    renderContext.ExecuteCommandBuffer(_cmd);
                    _cmd.Clear();
                    renderContext.DrawRenderers(cullingResults,ref drawingSettings,ref filteringSettings,ref stateBlock);
                    
                    //_cmd.Clear();
                    _cmd.Blit(mrt[1],depthRenderBufferTagID);
                    renderContext.ExecuteCommandBuffer(_cmd);
                    _cmd.Clear();
                }
                //_cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget);
                _cmd.Blit(mrt[1], BuiltinRenderTextureType.CameraTarget);
                renderContext.ExecuteCommandBuffer(_cmd);
                _cmd.Clear();
            }
        }

    }

    //draw shadowmap and make light params
    public class LightRender : FRenderBase
    {
        //NativeArray 主要来存放值类型数据，在jobsystem中使得可共享主线程中的资源。可让cpu更容易命中缓存

        private const string BUFFER_NAME = "LightRender";
        CommandBuffer _cmd = new CommandBuffer() { name = BUFFER_NAME };

        public LightRender()
        {

        }
        public override void DisposeRender(bool disposing)
        {
            if (disposing)
            {

            }
        }

        //public void PrepareShadow(ref ScriptableRenderContext renderContext,Culling)

        public override void ExecuteRender(ref ScriptableRenderContext renderContext, CullingResults cullingResults, Camera camera)
        {

            m_renderResource.lightResource.UpdateLightData(cullingResults.visibleLights, _cmd);
            renderContext.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }
    }

    public class SphericalHarmonicsRender : FRenderBase
    {
        private const string BUFFER_NAME = "SHRender";
        CommandBuffer _cmd = new CommandBuffer() { name = BUFFER_NAME };

        public SphericalHarmonicsRender()
        {

        }
        public override void DisposeRender(bool disposing)
        {
            if (disposing)
            {
                m_renderResource.SHResource.Dispose();
            }
        }

        public override void ExecuteRender(ref ScriptableRenderContext renderContext, CullingResults cullingResults, Camera camera)
        {
            m_renderResource.SHResource.UpdateSHData(camera, _cmd);
            renderContext.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }
    }

    public class ShadowRender : FRenderBase
    {
        private const string BUFFER_NAME = "ShadowRender";
        CommandBuffer _cmd = new CommandBuffer() { name = BUFFER_NAME };
        FRPRenderSettings renderSettings;
        public ShadowRender()
        {

        }

        public void SetupRenderSettings(FRPRenderSettings setting)
        {
            renderSettings = setting;
        }
        public override void DisposeRender(bool disposing)
        {
            //m_renderResource.shadowResource.Dispose();
            if (disposing)
            {


            }
        }
        Dictionary<Light, int> dirLight = new Dictionary<Light, int>(2);
        Dictionary<Light, int> pointLight = new Dictionary<Light, int>(2);


        public override void ExecuteRender(ref ScriptableRenderContext renderContext, CullingResults cullingResults, Camera camera)
        {
            //m_renderResource.shadowResource.UpdateShadowSettingParams(shadowSetting);
            dirLight.Clear();
            pointLight.Clear();
            int lightIdx = 0;
            foreach (var visLight in cullingResults.visibleLights)
            {
                if (visLight.lightType == LightType.Directional)
                {
                    dirLight.Add(visLight.light, lightIdx++);
                }
                else if (visLight.lightType == LightType.Point)
                {
                    pointLight.Add(visLight.light, lightIdx++);
                }
            }
            //m_renderResource.shadowResource.UpdateDirShadowMap(dirLight,renderContext,camera,cullingResults,_cmd);
            //m_renderResource.shadowResource.UpdatePointShadowMap(pointLight,renderContext,camera,cullingResults,_cmd);

            renderContext.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }
    }

}

