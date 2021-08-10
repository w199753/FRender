using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace frp
{
    public abstract class FRenderPassAsset : ScriptableObject
    {
        private Dictionary<Camera, FRenderPass> perCameraPass = new Dictionary<Camera, FRenderPass>();
        public abstract FRenderPass CreateRenderPass();
        public FRenderPass GetRenderPass(Camera camera)
        {
            if (!perCameraPass.ContainsKey(camera))
                perCameraPass[camera] = CreateRenderPass();
            return perCameraPass[camera];
        }
    }
}

