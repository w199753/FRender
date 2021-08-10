using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        public TransparentRenderPass (TransparentRenderAsset asset):base(asset)
        {

        }
        public override void Render()
        {
            base.Render();
        }

    }
}
