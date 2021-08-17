using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace frp
{
    [CreateAssetMenu(fileName = "PostEffectRenderAsset", menuName = "FRP/RenderPass/postEffectPass")]
    public class PostEffectRenderAsset : FRenderPassAsset
    {
        public override FRenderPass CreateRenderPass()
        {
            return new PostEffectRenderPass(this);
        }
    }

    public class PostEffectRenderPass : FRenderPassRender<PostEffectRenderAsset>
    {
        public PostEffectRenderPass(PostEffectRenderAsset asset):base(asset)
        {

        }
        private static readonly string BUFFER_NAME = "PostEffectPass";
        public FRPRenderSettings settings;
        CommandBuffer buffer = new CommandBuffer() { name = BUFFER_NAME };

        public override void Render()
        {
            buffer.BeginSample(BUFFER_NAME);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();

            settings = renderingData.settings;

            buffer.EndSample(BUFFER_NAME);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear(); 
        }


        public override void Cleanup()
        {
        }
    }

}
