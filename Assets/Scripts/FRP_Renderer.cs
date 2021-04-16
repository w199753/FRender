using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public abstract class F_Render
{
    protected FRPRenderResource _renderResources;
    virtual public void Execute(ref ScriptableRenderContext renderContext,CullResults cullResults,Camera camera)
    {}
    public abstract void Dispose();
    public void AllocateResources(FRPRenderResource resoucres)
    {
        _renderResources = resoucres;
    }
}

public class CommonRenderer : F_Render
{
    public override void Execute(ref ScriptableRenderContext renderContext, CullResults cullResults, Camera camera)
    {
        base.Execute(ref renderContext, cullResults, camera);
        renderContext.SetupCameraProperties(camera);
        
        CommandBuffer cg = CommandBufferPool.Get("CommonRender_setBuffer");
        RenderTargetIdentifier[] mrt = new RenderTargetIdentifier[10];
    }
    public override void Dispose()
    {
        throw new System.NotImplementedException();
    }
}
