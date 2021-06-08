
#ifndef __FRP__BRDF__
#define __FRP__BRDF__

#include "../Shader/UnityHLSL.hlsl"

inline half Pow5(half x){return x * x * x * x * x;}

inline half2 Pow5(half2 x){return x * x * x * x * x;}

inline half3 Pow5(half3 x){return x * x * x * x * x;}

inline half4 Pow5(half4 x){return x * x * x * x * x;}

inline float3 F_Schlick(float3 f0, float f90, float VoH) 
{
    // Schlick 1994, "An Inexpensive BRDF Model for Physically-Based Rendering"
    return f0 + (f90 - f0) * Pow5(1.0 - VoH);
}
inline float3 F_Schlick(float3 f0, float VoH) 
{
    float f = pow(1.0 - VoH, 5.0);
    return f + f0 * (1.0 - f);
}
inline float F_Schlick(float f0, float f90, float VoH) {
    return f0 + (f90 - f0) * Pow5(1.0 - VoH);
}

//-----------------------DisneyBRDF
/*
    f_r = diffuse + D*F*G/(4*ndotl*ndotv)

    f_diffuse = p/pi * (1+(fd90 - 1)(1-ndotl))*(1+(fd90 - 1)(1-ndotv))
    fd90 = 0.5 + 2*roughness*hdotv*hdotv

    D(GTR): D_gtr = c/pow( ( Sq(a)*Sq(ndoth) + Sq(sin(h)) , y)

*/
//--------------------

float D_GTR_1(float roughness,float NdotH)
{
    float sq_roughness =Sq(roughness);
    float sq_NdotH = Sq(NdotH);
    float denom = (1.0+(sq_roughness - 1.0)*sq_NdotH);
    return (sq_roughness - 1.0)/(UNITY_PI * log(sq_roughness)*denom);
}

float D_GTR_2(float roughness,float NdotH)
{
    float sq_roughness =Sq(roughness);
    float sq_NdotH = Sq(NdotH);
    float denom = (1.0+(sq_roughness - 1.0)*sq_NdotH);
    return (sq_roughness )/(UNITY_PI*denom*denom);
}

inline float SmithJointGGXVisibilityTerm(float NdotL, float NdotV, float roughness)
{
#if 0
	// Original formulation:
	//  lambda_v    = (-1 + sqrt(a2 * (1 - NdotL2) / NdotL2 + 1)) * 0.5f;
	//  lambda_l    = (-1 + sqrt(a2 * (1 - NdotV2) / NdotV2 + 1)) * 0.5f;
	//  G           = 1 / (1 + lambda_v + lambda_l);

	// Reorder code to be more optimal
	half a = roughness;
	half a2 = a * a;

	half lambdaV = NdotL * sqrt((-NdotV * a2 + NdotV) * NdotV + a2);
	half lambdaL = NdotV * sqrt((-NdotL * a2 + NdotL) * NdotL + a2);

	// Simplify visibility term: (2.0f * NdotL * NdotV) /  ((4.0f * NdotL * NdotV) * (lambda_v + lambda_l + 1e-5f));
	return 0.5f / (lambdaV + lambdaL + 1e-5f);  // This function is not intended to be running on Mobile,
												// therefore epsilon is smaller than can be represented by half
#else
	// Approximation of the above formulation (simplify the sqrt, not mathematically correct but close enough)
	float a = roughness;
	float lambdaV = NdotL * (NdotV * (1 - a) + a);
	float lambdaL = NdotV * (NdotL * (1 - a) + a);

#if defined(SHADER_API_SWITCH)
	return 0.5f / (lambdaV + lambdaL + 1e-4f); // work-around against hlslcc rounding error
#else
	return 0.5f / (lambdaV + lambdaL + 1e-5f);
#endif

#endif
}

float smithG_GGX(float NdotV, float alphaG)
{
    float a = alphaG * alphaG;
    float b = NdotV * NdotV;
    return 1 / (NdotV + sqrt(a + b - a * b));
}

float DisneyDiffuse(float NdotV,float NdotL,float VdotH,float roughness)
{
    float fd90 = 0.5 + 2*Sq(VdotH) * roughness;
    float f_view = F_Schlick(1,fd90,NdotV); // 1+(fd90 - 1) *Pow5(1-NdotV);
    float f_light = F_Schlick(1,fd90,NdotL); // 1+(fd90 - 1) *Pow5(1-NdotL);
    return UNITY_INV_PI * f_view * f_light;
}

inline float3 CalMaterialF0(float3 albedo,float metallic,out float3 F0)
{
    F0 = lerp(unity_ColorSpaceDielectricSpec.rgb,albedo,metallic);
    return albedo;
}

float3 Disney_BRDF(float3 baseColor,float3 F0,float NdotV,float NdotL,float VdotH,float LdotH ,float NdotH,float roughness)
{
    
    roughness = clamp(roughness*roughness,0.000001,0.99999);
    float G_Roughness = Sq(0.5+roughness*0.5);
    float kd = DisneyDiffuse(NdotV,NdotL,VdotH,roughness);
    float D = D_GTR_2(roughness,NdotH);
    float G = smithG_GGX(NdotV,G_Roughness)*smithG_GGX(NdotL,G_Roughness);
    float3 F = F_Schlick(F0,LdotH);
    float ks = G*D*F*UNITY_PI;
    //return ks*0.25*NdotL*NdotV;
    return kd*baseColor+ks*0.25*NdotL*NdotV;

}

float3 BRDF_CookTorrance(float3 albedo, float3 N, float3 V, float3 L, float smoothness, float light_contri)
{
    return float3(1,1,1);
}

#endif