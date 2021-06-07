
#ifndef __FRP__BRDF__
#define __FRP__BRDF__

#include "../Shader/UnityHLSL.hlsl"

inline half Pow5(half x){return x * x * x * x * x;}

inline half2 Pow5(half2 x){return x * x * x * x * x;}

inline half3 Pow5(half3 x){return x * x * x * x * x;}

inline half4 Pow5(half4 x){return x * x * x * x * x;}


float DisneyDiffuse(float NdotV,float NdotL,float VdotH,float perceptualRoughness)
{
    float fd90 = 0.5 + 2*Sq(VdotH) * perceptualRoughness;
    float f_view = 1+(fd90 - 1) *Pow5(1-NdotV);
    float f_light = 1+(fd90 - 1) *Pow5(1-NdotL);
    return UNITY_INV_PI * f_view * f_light;
}

inline float3 CalMaterialF0(float3 albedo,float metallic,out float3 F0)
{
    F0 = lerp(unity_ColorSpaceDielectricSpec.rgb,albedo,metallic);
    return albedo;
}

float3 BRDF_CookTorrance(float3 albedo, float3 N, float3 V, float3 L, float smoothness, float light_contri)
{
    return float3(1,1,1);
}

#endif