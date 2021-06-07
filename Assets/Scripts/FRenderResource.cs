using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
        private struct LightData 
        {
            public Vector4 pos_type;
            public Vector4 geometry;
            public Vector4 color;
        }
        private class ShaderPropertyID
        {
            public int lightData;
            public int lightCount;
            public ShaderPropertyID()
            {
                lightData = Shader.PropertyToID("_LightData");
                lightCount = Shader.PropertyToID("_LightCount");
            }
        }

        LightData data= new LightData();
        List<LightData> lightDataList = new List<LightData>(4);
        private static readonly ShaderPropertyID shaderPropertyID = new ShaderPropertyID();
        public const int MAX_DIR_LIGHT_COUNT = 2;
        public const int MAX_POINT_LIGHT_COUNT = 2;

        public void UpdateLightData(NativeArray<VisibleLight> visibleLights,CommandBuffer buffer)
        {
            lightDataList.Clear();

            int lightCount = visibleLights.Length;
            if(lightCount == 0)return ;
            ComputeBuffer dataBuffer = new ComputeBuffer(lightCount,Marshal.SizeOf(typeof(LightData)));
            
            foreach (var light in visibleLights)
            {
                Matrix4x4 local2world = light.localToWorldMatrix;
                
                if(light.lightType == LightType.Directional)
                {
                    data.geometry = -local2world.GetColumn(2).normalized;//new Vector3(local2world.m02, local2world.m12, local2world.m22).normalized;
                    data.geometry.w = float.MaxValue;
                    data.pos_type = -data.geometry;
                    data.pos_type.w = 1;
                    data.color = light.finalColor;
                }
                else if(light.lightType == LightType.Point)
                {
                    data.geometry = -local2world.GetColumn(2).normalized;
                    data.geometry.w = light.range;
                    data.pos_type = new Vector4(local2world.m03,local2world.m13,local2world.m23,2);
                    data.color = light.finalColor;
                }

                lightDataList.Add(data);
            }
            
            dataBuffer.SetData<LightData>(lightDataList);   
            buffer.SetGlobalInt(shaderPropertyID.lightCount,lightCount);
            buffer.SetGlobalBuffer(shaderPropertyID.lightData,dataBuffer);

        }

    }

    public class ShadowResource
    {

    }
}
