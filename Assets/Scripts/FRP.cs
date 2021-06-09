using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace frp
{
    public class FRP : RenderPipeline
    {
        private FRenderResource m_renderresouces;
        private CullingResults cullingResults;
        CommonRender commonRender;
        ObjectRender objectRender;
        LightRender lightRender;
        SphericalHarmonicsRender shRender;
        public FRP()
        {
            m_renderresouces = new FRenderResource();

            commonRender = new CommonRender();
            objectRender = new ObjectRender();
            lightRender = new LightRender();
            shRender = new SphericalHarmonicsRender();
        }
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            BeginFrameRendering(context, cameras);
            foreach (var camera in cameras)
            {
                BeginCameraRendering(context, camera);

                if(!Cull(context,camera)) break;
                
                CustomRender(context,camera);

                context.Submit();
                EndCameraRendering(context, camera);
            }
            EndFrameRendering(context, cameras);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            commonRender.DisposeRender(disposing);
            lightRender.DisposeRender(disposed);
            shRender.DisposeRender(disposed);
        }


        bool Cull(ScriptableRenderContext context, Camera camera)
        {
            if(camera.TryGetCullingParameters(out ScriptableCullingParameters cullParam))
            {
                cullingResults = context.Cull(ref cullParam);
                return true;
            }
            return false;
        }

        void CustomRender(ScriptableRenderContext context, Camera camera)
        {
            Setup(ref context,camera);
            lightRender.AllocateResources(m_renderresouces);
            lightRender.ExecuteRender(ref context,cullingResults,camera);
            shRender.AllocateResources(m_renderresouces);
            shRender.ExecuteRender(ref context,cullingResults,camera);
            commonRender.ExecuteRender(ref context,cullingResults,camera);
            objectRender.ExecuteRender(ref context,cullingResults,camera);
        }

        void Setup(ref ScriptableRenderContext context,Camera camera)
        {
            context.SetupCameraProperties(camera);
        }
    }
}

