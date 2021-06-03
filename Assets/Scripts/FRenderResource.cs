using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace frp
{
    public class FRenderResource
    {
        public LightResource lightResource;

        public ShadowResource shadowResource;

        public FRenderResource()
        {
            lightResource = new LightResource();
            shadowResource = new ShadowResource();
        }

    }

    public class LightResource
    {
        private class ShaderPropertyID
        {
            public int dirLightColor;
            public int dirLightCount;
            public int pointLightCount;
            public ShaderPropertyID()
            {
                dirLightColor = Shader.PropertyToID("_DirLightColor");
                dirLightCount = Shader.PropertyToID("_DirLightCount");
                pointLightCount = Shader.PropertyToID("_PointLightCount");
            }
        }
        private static readonly ShaderPropertyID shaderPropertyID = new ShaderPropertyID();
        public const int MAX_DIR_LIGHT_COUNT = 2;
        public const int MAX_POINT_LIGHT_COUNT = 2;

        public void UpdateDirLightData(NativeArray<VisibleLight> dirLights,CommandBuffer buffer)
        {
            buffer.SetGlobalVector(shaderPropertyID.dirLightColor,new Vector4(1,0,0,1));
        }
        public void UpdatePointLightData(NativeArray<VisibleLight> pointLights,CommandBuffer buffer)
        {

        }
    }

    public class ShadowResource
    {

    }
}
