using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace frp
{
    public class FRP : RenderPipeline
    {
        private FShadowSetting shadowSetting;
        private FRenderResource m_renderresouces;
        private CullingResults cullingResults;
        
        CommonRender commonRender;
        ObjectRender objectRender;
        LightRender lightRender;
        SphericalHarmonicsRender shRender;
        ShadowRender shadowRender;
        public FRP(FShadowSetting smsetting)
        {
            FRenderResourcePool.Disposexx();
            m_renderresouces = new FRenderResource();

            commonRender = new CommonRender();
            objectRender = new ObjectRender();
            lightRender = new LightRender();
            shRender = new SphericalHarmonicsRender();
            shadowRender = new ShadowRender();

            shadowSetting = smsetting;
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
            //shadowRender.DisposeRender(disposed);
            FRenderResourcePool.TestFRenderResourcePool();
            FRenderResourcePool.Disposexx();
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
            
            shadowRender.AllocateResources(m_renderresouces);
            shadowRender.ExecuteRender(ref context,cullingResults,camera);

            shRender.AllocateResources(m_renderresouces);
            shRender.ExecuteRender(ref context,cullingResults,camera);

            commonRender.ExecuteRender(ref context,cullingResults,camera);

            objectRender.ExecuteRender(ref context,cullingResults,camera);


        }

        void Setup(ref ScriptableRenderContext context,Camera camera)
        {
            shadowRender.SetupShadowSettings(shadowSetting);
            context.SetupCameraProperties(camera);
        }
    }
}

