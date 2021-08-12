using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace frp
{
public class FRP : RenderPipeline
{
    FRPRenderSettings renderSettings;
    private CullingResults m_cullingResults;
    private List<FRenderPass> m_renderPassQueue = new List<FRenderPass>();
    public FRP(FRPRenderSettings settings)
    {
        renderSettings = settings;
    }
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(context, cameras);
        foreach (var camera in cameras)
        {
            BeginCameraRendering(context, camera);

            if(!Cull(context,camera)) break;
            CameraRender(context,camera);

            context.Submit();
            EndCameraRendering(context, camera);
        }
        EndFrameRendering(context, cameras);
    }

    private bool Cull(ScriptableRenderContext context, Camera camera)
    {
        if(camera.TryGetCullingParameters(out ScriptableCullingParameters cullParam))
        {
            m_cullingResults = context.Cull(ref cullParam);
            return true;
        }
        return false;
    }

    private void CameraRender(ScriptableRenderContext context,Camera camera)
    {
        if(camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }

        RenderingData renderingData = new RenderingData();
        renderingData.settings = renderSettings;
        renderingData.cullingResults = m_cullingResults;
        renderingData.lightingData = new Dictionary<int, LightingData>();
        renderingData.shadowData = new Dictionary<int, ShadowData>();
        renderingData.sourceViewMatrix = camera.worldToCameraMatrix;
        renderingData.sourceProjectionMatrix = camera.projectionMatrix;


        Setup(context,camera);

        InitRenderPassQueue(camera);
        
        context.DrawSkybox(camera);

        foreach(var pass in m_renderPassQueue)
        {
            pass.Setup(context,camera,renderingData);
            pass.Render();
        }
        
        if(UnityEditor.Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera,GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera,GizmoSubset.PostImageEffects);
        }
        
        var resetBuffer = CommandBufferPool.Get("ResetBuffer");
        resetBuffer.SetViewProjectionMatrices(renderingData.sourceViewMatrix,renderingData.sourceProjectionMatrix);
        context.ExecuteCommandBuffer(resetBuffer);
        resetBuffer.Clear();
        CommandBufferPool.Release(resetBuffer);
        context.Submit();
    }

    private void Setup(ScriptableRenderContext context,Camera camera)
    {
        context.SetupCameraProperties(camera);
        //setup之后需要清一次
        var buffer = CommandBufferPool.Get();
        buffer.ClearRenderTarget(true,true,Color.clear);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private void InitRenderPassQueue(Camera camera)
    {
        m_renderPassQueue.Clear();
        foreach(var renderPassAsset in renderSettings.RenderPassAssets)
        {
            if(renderPassAsset != null)
            {
                var pass = renderPassAsset.GetRenderPass(camera);
                m_renderPassQueue.Add(pass);
            }
        }
    }

    private void Cleanup()
    {
        
    }
    
}
}

