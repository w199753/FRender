using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace frp
{
    public abstract class FRenderPassRender<T> : FRenderPass where T : FRenderPassAsset
    {
        protected T asset { get; private set; }
        public FRenderPassRender(T asset)
        {
            this.asset = asset;
        }
    }
    public abstract class FRenderPass
    {
        [NonSerialized]
        protected ScriptableRenderContext context;
        [NonSerialized]
        protected Camera camera;
        [NonSerialized]
        protected RenderingData renderingData;
        public virtual void Setup(ScriptableRenderContext context, Camera camera, RenderingData renderingData)
        {
            this.context = context;
            this.camera = camera;
            this.renderingData = renderingData;
        }

        public virtual void Render()
        {

        }

        public virtual void Cleanup()
        {

        }
    }


}

