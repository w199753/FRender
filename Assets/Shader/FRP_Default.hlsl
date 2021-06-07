
#ifndef __FRP__DEFAULT__
#define __FRP__DEFAULT__


#include "../Shader/InputMacro.hlsl"
#include "../Shader/UnityBuiltIn.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "../Shader/FRP_Light.hlsl"
#include "../Shader/FRP_BRDF.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    float _Metallic;
    float _Roughness;
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
    float4 worldPos : POSITION1;
};

v2f vert (appdata v)
{
    v2f o;
    o.vertex = TransformObjectToHClip(v.vertex.xyz);
    o.normal = v.normal;
    o.uv = TRANSFORM_TEX(v.uv, _MainTex); 
    o.worldPos = mul(unity_ObjectToWorld,v.vertex);
    return o;
}
float4 frag (v2f i) : SV_Target
{
    float4 abledo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    float4 resColor = 0;
    float3 contrib = 0;

    float3 F0 ;
    CalMaterialF0(abledo,_Metallic,F0);

    float3 N = normalize(i.normal);
    float3 worldPos = i.worldPos;
    float3 V = normalize(_WorldSpaceCameraPos - worldPos);


    float3 lightDir ;
    for(int idx=0;idx< _LightCount;idx++)
    {
        Light light = _LightData[idx];
        
        if(light.pos_type.w == 1)
        {
            contrib = CalDirLightContribution(light);
            lightDir = normalize(light.geometry.xyz);
        }
        else if(light.pos_type.w == 2)
        {
            contrib = CalPointLightContribution(light,worldPos);
            lightDir = normalize(light.pos_type.xyz - worldPos.xyz);
        }
        resColor += float4(contrib,0);
    }
    float L = lightDir;
    float3 H = normalize(L+V);
    float NdotV = max(0.00001,saturate(dot(N,V)));
    float NdotL = max(0.00001,saturate(dot(N,L)));
    float VdotH = max(0.00001,saturate(dot(V,H)));
    return  DisneyDiffuse(NdotV,NdotL,VdotH,_Roughness);
    //return _LightData[idx].color;
    return resColor*abledo;
}

#endif