
#ifndef __FRP__DEFAULT__
#define __FRP__DEFAULT__


#include "../Shader/InputMacro.hlsl"
#include "../Shader/UnityBuiltIn.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

#include "../Shader/FRP_Light.hlsl"
#include "../Shader/FRP_BRDF.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    float _Metallic;
    float _Roughness;
    float _Anisotropy;
CBUFFER_END

//采样器状态参考文档 https://docs.unity3d.com/Manual/SL-SamplerStates.html 
// #define sampler_MainTex SamplerState_Point_Repeat
//#define sampler_MainTex SamplerState_Point_Clamp
#define sampler_MainTex SamplerState_Linear_Clamp
//#define sampler_MainTex SamplerState_Linear_Mirror
SAMPLER(sampler_MainTex);
TEXTURE2D(_MainTex);
TEXTURE2D(_Normal);

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
    float4 tangent : TANGENT;
};
struct v2f
{
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
    float3 tangent : TEXCOORD1;
    float3 bitangent : TEXCOORD2;
    float4 vertex : SV_POSITION;
    float4 worldPos : POSITION1;
};

v2f vert (appdata v)
{
    v2f o;
    o.vertex = TransformObjectToHClip(v.vertex.xyz);
    o.normal = TransformObjectToWorldNormal(v.normal);
    o.tangent = normalize(mul(unity_ObjectToWorld,float4(v.tangent.xyz,0)).xyz);
    o.bitangent = normalize(cross(o.normal,o.tangent)*v.tangent.w);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex); 
    o.worldPos = mul(unity_ObjectToWorld,v.vertex);
    return o;
}
float4 frag (v2f i) : SV_Target
{
    float3x3 tangentTransform = float3x3(i.tangent, i.bitangent, normalize(i.normal));

    float4 abledo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    float4 resColor = 0;
    float3 contrib = 0;

    float3 F0 ;
    CalMaterialF0(abledo,_Metallic,F0);

    float3 normal_Tex = UnpackNormalMaxComponent(SAMPLE_TEXTURE2D(_Normal, sampler_MainTex, i.uv).xyz);
#if _NormalTexOn
    float3 N = normalize(mul(normal_Tex,tangentTransform));
#else
    float3 N = normalize(i.normal);
#endif
    float3 T = normalize(i.tangent);
    float3 B = normalize(i.bitangent);
    float3 worldPos = i.worldPos;
    float3 V = normalize(_WorldSpaceCameraPos - worldPos);


    float3 lightDir ;
    for(int idx=0;idx< _LightCount;idx++)
    {
        Light light = _LightData[idx];
        
        if(light.pos_type.w == 1)
        {
            contrib = CalDirLightContribution(light);
            lightDir = normalize(light.pos_type.xyz);
        }
        else if(light.pos_type.w == 2)
        {
            contrib = CalPointLightContribution(light,worldPos);
            lightDir = normalize(light.pos_type.xyz - worldPos.xyz);
        }
        resColor += float4(contrib,0);
    }
    float3 L = lightDir;
    float3 H = normalize(L+V);
    float NdotV = max(0.000001,(dot(N,V)));
    float NdotL = max(0.000001,(dot(N,L)));
    float VdotH = max(0.000001,(dot(V,H)));
    float LdotH = max(0.000001,(dot(L,H)));
    float NdotH = max(0.000001,(dot(N,H)));
    float3 X = T;
    float3 Y = B;
    float VdotX = max(0.000001,(dot(V,X)));
    float VdotY = max(0.000001,(dot(V,Y)));
    float LdotX = max(0.000001,(dot(L,X)));
    float LdotY = max(0.000001,(dot(L,Y)));
    float HdotX = max(0.000001,(dot(H,X)));
    float HdotY = max(0.000001,(dot(H,Y)));
    return float4(contrib*Disney_BRDF(abledo.rgb,F0,NdotV,NdotL,LdotH,LdotH,NdotH,
    _Roughness,_Anisotropy,VdotX,VdotY,LdotX,LdotY,HdotX,HdotY,X,Y),1);
    //return  DisneyDiffuse(NdotV,NdotL,LdotH,_Roughness);
    //return _LightData[idx].color;
    return resColor*abledo;
}

#endif