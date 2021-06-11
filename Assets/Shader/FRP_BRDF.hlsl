
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
    return f0+(1.0-f0)*exp2((-5.55473*VoH-6.98316)*VoH);

    //float f = pow(1.0 - VoH, 5.0);
    //return f0 + f * (1.0 - f0);
}
inline float F_Schlick(float f0, float f90, float VoH) {
    return f0 + (f90 - f0) * Pow5(1.0 - VoH);
}

struct BRDFParam
{
    float NdotV;
    float NdotL;
    float NdotH;
    float VdotH;
    float VdotL;
    float LdotH;
};
inline void InitBRDFParam(out BRDFParam param,float3 N,float3 V,float3 L,float3 H)
{
    param.NdotV = max(1e-6f,(dot(N,V)));
    param.NdotL = max(1e-6f,(dot(N,L)));
    param.NdotH = max(1e-6f,(dot(N,H)));
    param.VdotH = max(1e-6f,(dot(V,H)));
    param.VdotL = max(1e-6f,(dot(V,L)));
    param.LdotH = max(1e-6f,(dot(L,H)));

}

struct AnisoBRDFParam
{
    float ToH;
    float ToL; 
    float ToV; 
    float BoH;
    float BoL;
    float BoV; 
};
inline void InitAnisoBRDFParam(out AnisoBRDFParam param,float3 T,float3 B,float3 H,float3 L,float3 V)
{
    //T == X,B == Y
    param.ToH = dot(T, H);
    param.ToL = dot(T, L); 
    param.ToV = dot(T, V); 

    param.BoH = dot(B, H);
    param.BoL = dot(B, L);
	param.BoV = dot(B, V);
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

float D_GTR_2_Aniso(float NdotH, float HdotX, float HdotY, float ax, float ay)
{
    return 1 / (UNITY_PI * ax*ay * Sq( Sq(HdotX/ax) + Sq(HdotY/ay) + NdotH*NdotH ));
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

float smithG_GGX_aniso(float NdotV, float VdotX, float VdotY, float ax, float ay)
{
    return 1 / (NdotV + sqrt( Sq(VdotX*ax) + Sq(VdotY*ay) + Sq(NdotV) ));
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

float3 Disney_BRDF
    (float3 baseColor,float3 F0,float _roughness,float anisotropy,BRDFParam brdfParam,AnisoBRDFParam anisoBrdfParam)
{

    float roughness = clamp(_roughness,2e-4f,0.9999999);
    float sq_roughness = clamp(_roughness*_roughness,4e-7f,0.9999999);
    float aspect = sqrt(1 - anisotropy * 0.9);
    float ax = max(0.001,sq_roughness/aspect);
    float ay = max(0.001,sq_roughness*aspect);
    
    
    float kd = DisneyDiffuse(brdfParam.NdotV,brdfParam.NdotL,brdfParam.VdotH,roughness);
    //float D = D_GTR_2(roughness,NdotH);
    float D = D_GTR_2_Aniso(brdfParam.NdotH,anisoBrdfParam.ToH,anisoBrdfParam.BoH,ax,ay);
    //float G_Roughness = Sq(0.5+roughness*0.5);
    //float G = smithG_GGX(NdotV,G_Roughness)*smithG_GGX(NdotL,G_Roughness);
    float G = smithG_GGX_aniso(brdfParam.NdotV,anisoBrdfParam.ToV,anisoBrdfParam.BoV,ax,ay)*smithG_GGX_aniso(brdfParam.NdotV,anisoBrdfParam.ToL,anisoBrdfParam.BoL,ax,ay);
    float3 F = F_Schlick(F0,brdfParam.VdotH);
    float3 ks = G*D*F;
    //return ks*0.25*NdotL*NdotV;
    //return (F*0.25)/(brdfParam.NdotL*brdfParam.NdotV)*brdfParam.NdotL;
    return (kd*baseColor+ks/(4.0*brdfParam.NdotL*brdfParam.NdotV)) * brdfParam.NdotL;

}



//------CookTorranceBRDF
/*
    ks = 1 - kd
    f_diffuse = c/pi * kd
    f_specular = DGF/(4*NdotL*NdotV) 
*/     
//-------------------------------

float NormalDistributionFunciont_GGX(float roughness,float NdotH)
{
    return 0 ;
	//float roughnessSqr = roughness * roughness;
	//float roughnessFourSqr = roughnessSqr * roughnessSqr;
	//float NdotHSqr = NdotH * NdotH;
	//return (roughnessFourSqr / ((NdotHSqr * (roughnessFourSqr - 1) + 1) * (NdotHSqr * (roughnessFourSqr - 1) + 1)))* one_div_pi;
}

float3 BRDF_CookTorrance(float3 baseColor,float3 F0,float _roughness,float anisotropy,BRDFParam brdfParam,AnisoBRDFParam anisoBrdfParam)
{
    
}

#endif