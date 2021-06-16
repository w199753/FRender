
#ifndef __UNITY__HLSL__
#define __UNITY__HLSL__

#define UNITY_PI			3.14159265359f
#define UNITY_TWO_PI		6.28318530718f
#define UNITY_FOUR_PI		12.56637061436f
#define UNITY_INV_PI		0.31830988618f
#define UNITY_INV_TWO_PI	0.15915494309f
#define UNITY_INV_FOUR_PI	0.07957747155f
#define UNITY_HALF_PI		1.57079632679f
#define UNITY_INV_HALF_PI	0.636619772367f

#ifdef UNITY_COLORSPACE_GAMMA
#define unity_ColorSpaceGrey fixed4(0.5, 0.5, 0.5, 0.5)
#define unity_ColorSpaceDouble fixed4(2.0, 2.0, 2.0, 2.0)
#define unity_ColorSpaceDielectricSpec half4(0.220916301, 0.220916301, 0.220916301, 1.0 - 0.220916301)
#define unity_ColorSpaceLuminance half4(0.22, 0.707, 0.071, 0.0) // Legacy: alpha is set to 0.0 to specify gamma mode
#else // Linear values
#define unity_ColorSpaceGrey fixed4(0.214041144, 0.214041144, 0.214041144, 0.5)
#define unity_ColorSpaceDouble fixed4(4.59479380, 4.59479380, 4.59479380, 2.0)
#define unity_ColorSpaceDielectricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)
#define unity_ColorSpaceLuminance half4(0.0396819152, 0.458021790, 0.00609653955, 1.0) // Legacy: alpha is set to 1.0 to specify linear mode
#endif



#endif
/*


#ifndef __FRP__DEFAULT__
#define __FRP__DEFAULT__


#include "../Shader/InputMacro.hlsl"
#include "../Shader/UnityBuiltIn.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

#include "../Shader/FRP_SH.hlsl"
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
#define sampler_MainTex SamplerState_Trilinear_Repeat
//#define sampler_MainTex SamplerState_Linear_Mirror

//#define sampler_Normal SamplerState_Trilinear_Repeat
SAMPLER(sampler_MainTex);
TEXTURE2D(_MainTex);

SAMPLER(sampler_Normal);
TEXTURE2D(_Normal);
TEXTURE2D(_RoughnessTex);
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

    float4 vertex : SV_POSITION;
    float4 worldPos : POSITION1;
    float3 shColor : TEXCOORD3;

    float3 tangent :TEXCOORD4;
    float3 bitangent :TEXCOORD5;
    float3 normal :TEXCOORD6;
    //float3x3 tbn : TEXCOORD4;
};

v2f vert (appdata v)
{
    v2f o;
    o.vertex = TransformObjectToHClip(v.vertex.xyz);
    //o.normal = TransformObjectToWorldNormal(v.normal);
    //o.tangent = normalize(mul(unity_ObjectToWorld,float4(v.tangent.xyz,0.0)).xyz);
    //o.bitangent = normalize(cross(o.normal,o.tangent)*v.tangent.w);
    float3 w_normal = TransformObjectToWorldNormal(v.normal);
    float3 w_tangent = normalize(mul(unity_ObjectToWorld,float4(v.tangent.xyz,0)).xyz);
    float3 w_bitangent = cross(w_normal , w_tangent) * v.tangent.w;
    //o.tbn = float3x3(w_tangent,w_bitangent,w_normal);
    o.tangent = w_tangent;
    o.bitangent = w_bitangent;
    o.normal = w_normal;
    o.uv = TRANSFORM_TEX(v.uv, _MainTex); 
    o.worldPos = mul(unity_ObjectToWorld,v.vertex);
    o.shColor = CalVertexSH(w_normal);
    return o;
}

float3 SSSS(float3 N)
{
    return  DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0,samplerunity_SpecCube0 , N, 0), unity_SpecCube0_HDR);
}


half4 frag (v2f i) : SV_Target
{

    float4 abledo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    float4 resColor = 0;
    //return float4(i.shColor,1);
    float3 F0 ;
    CalMaterialF0(abledo,_Metallic,F0);

    float3 nn = normalize(i.normal);
    float3 TT = normalize(i.tangent - dot(i.tangent,nn)*nn);
    float3 BB = normalize(cross(nn, TT));
    //return float4(T,1);
    //tangentTransform = float3x3(T,B,nn);

float3x3 tangentTransform = float3x3(TT, BB, nn);

    //return float4(normalize(i.bitangent),1);
    half3 normal_Tex = UnpackNormal(SAMPLE_TEXTURE2D(_Normal, sampler_MainTex, i.uv));
    //return float4(normal_Tex,1);
    float Roughness = _Roughness;
#if _NormalTexOn
    //float3 N = normalize(normal_Tex);//ormalize(mul(normal_Tex,tangentTransform));
    float3 N = normalize(mul(normal_Tex,tangentTransform));
    //N = normalize(normal_Tex);
#else
    //float3 N = normalize(i.tbn[2].xyz);
    float3 N = normalize(i.normal);
     //N = normalize(mul(normal_Tex,i.tbn));
#endif
//return float4(N,1);

#if _RoughnessTexOn
    Roughness = SAMPLE_TEXTURE2D(_RoughnessTex, sampler_MainTex, i.uv).r;
#else
    Roughness = _Roughness;
#endif


    //return float4(N,1);
    // float3 T = normalize(i.tbn[0].xyz);
    // float3 B = normalize(i.tbn[1].xyz);
    float3 T = normalize(i.tangent);
    float3 n = normalize(i.normal);
    T = normalize(i.tangent - dot(i.tangent,n)*n);
    float3 B = normalize(i.bitangent);
    B = normalize(cross(N, T));
    //return float4(T,1);
    tangentTransform = float3x3(T,B,n);
    tangentTransform = transpose(tangentTransform);
    float3 worldPos = mul(i.worldPos,tangentTransform);
    float3 V = normalize(mul(_WorldSpaceCameraPos.xyz,tangentTransform) - worldPos);

    float3 lightDir ;
    half3 contrib = 0;
    BRDFParam brdfParam;
    AnisoBRDFParam anisoBrdfParam;
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
        
        float3 L = lightDir;
        L = normalize(mul(L,tangentTransform));
        float3 H = normalize(V+L);
        InitBRDFParam(brdfParam,N,V,L,H);
        InitAnisoBRDFParam(anisoBrdfParam,T,B,H,L,V);
        //resColor += float4(contrib*BRDF_CookTorrance(abledo.rgb,F0,_Metallic,Roughness,_Anisotropy,brdfParam,anisoBrdfParam),0);
        resColor += float4(contrib*Disney_BRDF(abledo.rgb,F0,Roughness,_Anisotropy,brdfParam,anisoBrdfParam),0);
    }
    //return float4(T,1);
    //return anisoBrdfParam.BoH;
    //return  float4(DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0,samplerunity_SpecCube0 , N, 0), unity_SpecCube0_HDR),1);
     
    //return unity_SpecCube0.SampleLevel(samplerunity_SpecCube0,N,1);
    
    //return UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, i.normal);
    return resColor;
    return resColor+float4(i.shColor*abledo.rgb,1);
}

#endif

*/