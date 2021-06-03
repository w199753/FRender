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
        NativeArray<VisibleLight> visibleDirLight ;
        NativeArray<VisibleLight> visiblePointLight ;

        private const string BUFFER_NAME = "LightRender";
        CommandBuffer _cmd = new CommandBuffer(){name = BUFFER_NAME};

        public LightRender()
        {
            visibleDirLight = new NativeArray<VisibleLight>(LightResource.MAX_DIR_LIGHT_COUNT,Allocator.Persistent);
            visiblePointLight = new NativeArray<VisibleLight>(LightResource.MAX_POINT_LIGHT_COUNT,Allocator.Persistent);
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
            int d_idx = 0;
            int p_idx = 0;
            foreach (var visLight in cullingResults.visibleLights)
            {
                if(visLight.lightType == LightType.Directional && d_idx < LightResource.MAX_DIR_LIGHT_COUNT)
                {
                    visibleDirLight[d_idx++] = visLight;
                }
                if(visLight.lightType == LightType.Point && p_idx < LightResource.MAX_POINT_LIGHT_COUNT)
                {
                    visiblePointLight[p_idx++] = visLight;
                }
            }
            m_renderResource.lightResource.UpdateDirLightData(visibleDirLight,_cmd);
            m_renderResource.lightResource.UpdatePointLightData(visiblePointLight,_cmd);

            renderContext.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }
    }
}

