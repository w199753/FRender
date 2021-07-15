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
        public abstract void ExecuteRender(ref ScriptableRenderContext renderContext,CullingResults cullingResults, Camera camera);
        public abstract void DisposeRender(bool disposing);

        public void AllocateResources(FRenderResource resource)
        {
            m_renderResource = resource;
        }
    }
    public class CommonRender : FRenderBase
    {
        private const string BUFFER_NAME = "CommonRender";
        CommandBuffer _cmd = new CommandBuffer(){name = BUFFER_NAME};
        public override void DisposeRender(bool disposing)
        {
            if(disposing)
            {

            }
        }

        public override void ExecuteRender(ref ScriptableRenderContext renderContext, CullingResults cullingResults, Camera camera)
        {
            _cmd.BeginSample(BUFFER_NAME);
            _cmd.ClearRenderTarget(true,true,Color.clear);
            renderContext.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();

            //draw skybox
            renderContext.DrawSkybox(camera);
            //draw gizmos
            if(Handles.ShouldRenderGizmos())
            {
                renderContext.DrawGizmos(camera,GizmoSubset.PreImageEffects);
                renderContext.DrawGizmos(camera,GizmoSubset.PostImageEffects);
            }

            _cmd.EndSample(BUFFER_NAME);
            renderContext.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }
    }

    public class ObjectRender : FRenderBase
    {
        private const string BUFFER_NAME = "ObjectRender";
        private const string FRP_BASE = "FRP_BASE";

        ShaderTagId baseShaderTagID = new ShaderTagId(FRP_BASE);
        CommandBuffer _cmd = new CommandBuffer(){name = BUFFER_NAME};
        public override void DisposeRender(bool disposing)
        {
            if(disposing)
            {

            }
        }

        public override void ExecuteRender(ref ScriptableRenderContext renderContext, CullingResults cullingResults, Camera camera)
        {
            _cmd.BeginSample(BUFFER_NAME);
            renderContext.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();

            SortingSettings sortingSettings = new SortingSettings(camera);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
            DrawingSettings drawingSettings = new DrawingSettings(baseShaderTagID,sortingSettings);

            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.opaque;
            drawingSettings.perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.ReflectionProbes;
            renderContext.DrawRenderers(cullingResults,ref drawingSettings,ref filteringSettings);

            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;
            renderContext.DrawRenderers(cullingResults,ref drawingSettings,ref filteringSettings);

            _cmd.EndSample(BUFFER_NAME);
            renderContext.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }
    }

    //draw shadowmap and make light params
    public class LightRender : FRenderBase
    {
        //NativeArray 主要来存放值类型数据，在jobsystem中使得可共享主线程中的资源。可让cpu更容易命中缓存

        private const string BUFFER_NAME = "LightRender";
        CommandBuffer _cmd = new CommandBuffer(){name = BUFFER_NAME};

        public LightRender()
        {

        }
        public override void DisposeRender(bool disposing)
        {
            if(disposing)
            {

            }
        }

        //public void PrepareShadow(ref ScriptableRenderContext renderContext,Culling)

        public override void ExecuteRender(ref ScriptableRenderContext renderContext, CullingResults cullingResults, Camera camera)
        {

            m_renderResource.lightResource.UpdateLightData(cullingResults.visibleLights,_cmd);
            renderContext.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }
    }

    public class SphericalHarmonicsRender : FRenderBase
    {
        private const string BUFFER_NAME = "SHRender";
        CommandBuffer _cmd = new CommandBuffer(){name = BUFFER_NAME};

        public SphericalHarmonicsRender()
        {

        }
        public override void DisposeRender(bool disposing)
        {
            if(disposing)
            {
                m_renderResource.SHResource.Dispose();
            }
        }

        public override void ExecuteRender(ref ScriptableRenderContext renderContext, CullingResults cullingResults, Camera camera)
        {
            m_renderResource.SHResource.UpdateSHData(camera,_cmd);
            renderContext.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }
    }

    public class ShadowRender : FRenderBase
    {
        private const string BUFFER_NAME = "ShadowRender";
        CommandBuffer _cmd = new CommandBuffer(){name = BUFFER_NAME};
        FShadowSetting shadowSetting;
        public ShadowRender()
        {

        }

        public void SetupShadowSettings(FShadowSetting setting)
        {
            shadowSetting = setting;
        }
        public override void DisposeRender(bool disposing)
        {
            //m_renderResource.shadowResource.Dispose();
            if(disposing)
            {
                 
                
            }
        }
        Dictionary<Light,int> dirLight = new Dictionary<Light,int>(2);
        Dictionary<Light,int> pointLight = new Dictionary<Light,int>(2);
        
        
        public override void ExecuteRender(ref ScriptableRenderContext renderContext, CullingResults cullingResults, Camera camera)
        {
            //m_renderResource.shadowResource.UpdateShadowSettingParams(shadowSetting);
            dirLight.Clear();
            pointLight.Clear();
            int lightIdx = 0;
            foreach(var visLight in cullingResults.visibleLights)
            {
                if(visLight.lightType == LightType.Directional)
                {
                    dirLight.Add(visLight.light,lightIdx++);
                }
                else if(visLight.lightType == LightType.Point)
                {
                    pointLight.Add(visLight.light,lightIdx++);
                }
            }
            //m_renderResource.shadowResource.UpdateDirShadowMap(dirLight,renderContext,camera,cullingResults,_cmd);
            //m_renderResource.shadowResource.UpdatePointShadowMap(pointLight,renderContext,camera,cullingResults,_cmd);

            renderContext.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }
    }
    
}

