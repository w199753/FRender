
#ifndef __FRP__DEFAULT__
#define __FRP__DEFAULT__


#include "../Shader/InputMacro.hlsl"
#include "../Shader/UnityBuiltIn.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "../Shader/FRP_Light.hlsl"

CBUFFER_START(UnityPerMaterial)
    //sampler2D _MainTex;
    float4 _MainTex_ST;

CBUFFER_END

//采样器状态参考文档 https://docs.unity3d.com/Manual/SL-SamplerStates.html 
// #define sampler_MainTex SamplerState_Point_Repeat
//#define sampler_MainTex SamplerState_Point_Clamp
#define sampler_MainTex SamplerState_Linear_Clamp
//#define sampler_MainTex SamplerState_Linear_Mirror
SAMPLER(sampler_MainTex);
TEXTURE2D(_MainTex);

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
};
struct v2f
{
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
    float4 vertex : SV_POSITION;
};

v2f vert (appdata v)
{
    v2f o;
    o.vertex = TransformObjectToHClip(v.vertex.xyz);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex); 
    return o;
}
float4 frag (v2f i) : SV_Target
{
    float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    float4 res = 0;
    float3 N = normalize(i.normal);
    for(int idx=0;idx< _LightCount;idx++)
    {
        Light light = _LightData[idx];
        if(light.pos_type.w == 1)
        {
            res += normalize(light.geometry);
            //res += max(0,dot(N,normalize(light.pos_type.xyz))) * col;
        }
        else if(light.pos_type.w == 2)
        {

        }
    }
    //return _LightData[idx].color;
    return res;
}

#endif