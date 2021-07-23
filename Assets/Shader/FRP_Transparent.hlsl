
#ifndef __FRP__TRANSPARENT__
#define __FRP__TRANSPARENT__

#include "../Shader/InputMacro.hlsl"
#include "../Shader/UnityBuiltIn.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

#include "../Shader/FRP_SH.hlsl"
#include "../Shader/FRP_Light.hlsl"
#include "../Shader/FRP_BRDF.hlsl"
#include "../Shader/Montcalo_Library.hlsl"



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
    float3 worldPos : POSITION1;
    float3 shColor : TEXCOORD1;
    float4 screenPos : TEXCOORD2;
};

#define sampler_MainTex SamplerState_Trilinear_Repeat
SAMPLER(sampler_MainTex);

//#define sampler_MainTex SamplerState_Trilinear_Repeat
//SAMPLER(sampler_FinalBuffers);

CBUFFER_START(UnityPerMaterial)
    TEXTURE2D(_MainTex);
    float4 _MainTex_ST;
    float4 _Color;
    float _AlphaClip;
    int _DepthRenderedIndex;
    TEXTURE2D(_DepthRenderBuffer);
    TEXTURE2D(_BackColor);
    TEXTURE2D_ARRAY(_FinalBuffers);
    int _MaxDepth;
    int _Test;
CBUFFER_END


float4 compute(float4 pos)
{
    float4 o = pos * 0.5f;
    o.xy = float2(o.x, o.y*_ProjectionParams.x) + o.w;
    o.zw = pos.zw;
    return o;
}

v2f vert (appdata v)
{
    v2f o;
    o.vertex = TransformObjectToHClip(v.vertex.xyz);
    o.normal = TransformObjectToWorldNormal(v.normal);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    o.worldPos = mul(unity_ObjectToWorld,v.vertex);
    o.shColor = CalVertexSH(o.normal);
    o.screenPos = compute(o.vertex);
    return o;
}
half4 frag_trans_default_1 (v2f i) : SV_Target
{
    half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color;
    clip(col.a - _AlphaClip);
    return 0;
}

half4 frag_trans_default_2 (v2f i) :SV_TARGET
{

    half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color;
    half3 L = 0;
    half3 N = normalize(i.normal);
    half contrib = 0;
    float3 worldPos = i.worldPos;
    for(int idx=0;idx< _LightCount;idx++)
    {
        Light light = _LightData[idx];
        half3 lightDir = 0;
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
        L = lightDir;
    }
    //clip(col.a - 0.2);
    col.xyz = col.xyz * max(0,dot(N,L)) * contrib + i.shColor*col.xyz;
    col.a = col.a;
    return col;
}

struct fout 
{
    float4 colorBuffer : SV_Target0;
    float4 depthBuffer : SV_Target1;
};


fout frag_trans_peeling_1 (v2f i)
{
    fout o;
    float depth = i.vertex.z;


    half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color;
    half3 L = 0;
    half3 N = normalize(i.normal);
    half contrib = 0;
    float3 worldPos = i.worldPos;
    for(int idx=0;idx< _LightCount;idx++)
    {
        Light light = _LightData[idx];
        half3 lightDir = 0;
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
        L = lightDir;
    }

    //col.rgb = col.rgb * max(0,dot(N,L)) * contrib + i.shColor*col.rgb;
    col.rgb += i.shColor*col.rgb;
    //col.a = col.a;
    clip(col.a - 0.05);
    float renderdDepth=SAMPLE_TEXTURE2D(_DepthRenderBuffer, sampler_MainTex, i.screenPos.xy/i.screenPos.w).r;
    if(_DepthRenderedIndex>0&&depth>=renderdDepth-0.000001) discard;
    o.depthBuffer = depth;
    o.colorBuffer = col;
    return o;
}

float4 frag_trans_peeling_2 (v2f i) : SV_TARGET
{
    float4 col = 0;
//     for(int idx = 0 ; idx<_MaxDepth+1;idx++)
//     {
// float4 front = SAMPLE_TEXTURE2D_ARRAY(_FinalBuffers,sampler_MainTex,i.uv,_MaxDepth - idx);
// 	      col.rgb=col.rgb*(1-front.a)+front.rgb*front.a;
// 	       col.a=1-(1-col.a)*(1-front.a);
//     }
//            col.a=saturate(col.a);
    col.rgb += SAMPLE_TEXTURE2D_ARRAY(_FinalBuffers,sampler_MainTex,i.uv,_Test);
        //col.rgb += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb;
        return col;
}


#endif