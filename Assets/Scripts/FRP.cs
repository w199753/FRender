using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class FRP : RenderPipeline
{
    private readonly FRPAsset _asset;

    
    
    public FRP(FRPAsset asset)
    {
        _asset = asset;
    }

    public override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
    {
        base.Render(renderContext, cameras);
        renderContext.DrawSkybox(cameras[0]);
        renderContext.Submit();
        //是基类RenderPipeline中的静态成员，在使用camera进行渲染时开始执行相关回调，回调可以自己加，不操作了
        BeginFrameRendering(cameras);

        SortCamera(ref cameras);

        foreach (var camera in cameras)
        {
            BeginCameraRendering(camera);
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
#endif   
        }


        
    }




    private void SortCamera(ref Camera[] cameras)
    {
        Array.Sort(cameras, (c1, c2) => { return c1.depth.CompareTo(c2.depth); });
    }
}
