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
#include "../Shader/Montcalo_Library.hlsl"

TextureCube<float3> CubeMap; 
SamplerState samplerCubeMap;

TextureCube<float3> TestPrefilter; 
SamplerState samplerTestPrefilter;

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
TEXTURE2D(_LUT);

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


float3 PrefilterEnvMap( TextureCube<float3> _AmbientCubemap, float Roughness, float3 Position) {
    float3 N = Position; float3 R = N; float3 V = R;

    const uint NumSamples = 32; float TotalWeight = 0.0; float3 PrefiterColor = 0.0;
    float Resolution = 256.0;
    float saTexel  = 4.0 * PI / (6.0 * Resolution * Resolution);
    for(uint i = 0u; i < NumSamples; ++i) {
        float2 Xi = Hammersley(i, NumSamples ,HaltonSequence(i));
        float3 H = TangentToWorld( ImportanceSampleGGX(Xi, Roughness), half4(N, 1.0) ).xyz;
        float3 L  = reflect(-V,H);  //这里的dwi是根据采样出的法线并根据观察方向反推回去的
        float NdotL = max(dot(N, L), 0.0);
        if(NdotL > 0.0) {
            float NoH = max(dot(N, H), 0.0);
            float HoV = max(dot(H, V), 0.0);
            float D   = D_GGX(Roughness, NoH);
            D = smithD_GGX(NoH,Roughness);
            float PDF = D * NoH / (4.0 * HoV) + 0.0001; 

            float saSample = 1.0 / (float(NumSamples) * PDF);

            float MipLevel = Roughness <= 1e-2f ? Roughness : 0.5 * log2(saSample / saTexel); 
            TotalWeight += NdotL;
            PrefiterColor += CubeMap.SampleLevel(samplerCubeMap, L, MipLevel).rgb * NdotL;
        }
    }

    return PrefiterColor / TotalWeight;
}


half4 frag (v2f i) : SV_Target
{

    float4 abledo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    float4 resColor = 0;
    //return float4(i.shColor,1);
    float3 F0 ;
    CalMaterialF0(abledo,_Metallic,F0);

    //return float4(T,1);
    //tangentTransform = float3x3(T,B,nn);

float3x3 tangentTransform = float3x3(i.tangent, i.bitangent, normalize(i.normal));

    //return float4(normalize(i.bitangent),1);
    half3 normal_Tex = UnpackNormal(SAMPLE_TEXTURE2D(_Normal, sampler_MainTex, i.uv));
    //return float4(normal_Tex,1);
    float Roughness = _Roughness;
#if _NormalTexOn
    //float3 N = normalize(normal_Tex);//ormalize(mul(normal_Tex,tangentTransform));
    float3 N = normalize(mul(normal_Tex,tangentTransform));
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

    Roughness = clamp(Roughness,1e-6f,0.9999999);

    float3 T = normalize(i.tangent);
    T = normalize(i.tangent - dot(i.tangent,N)*N);
    float3 B = normalize(i.bitangent);
    B = normalize(cross(N, T));

    float3 worldPos = i.worldPos;
    float3 V = normalize(_WorldSpaceCameraPos.xyz - worldPos);

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
        float3 H = normalize(V+L);
        InitBRDFParam(brdfParam,N,V,L,H);
        InitAnisoBRDFParam(anisoBrdfParam,T,B,H,L,V);
        resColor += float4(contrib*BRDF_CookTorrance(abledo.rgb,F0,_Metallic,Roughness,_Anisotropy,brdfParam,anisoBrdfParam),0);
        //resColor += float4(contrib*Disney_BRDF(abledo.rgb,F0,Roughness,_Anisotropy,brdfParam,anisoBrdfParam),0);
    }
    
    //return float4(resColor);
    float3 anisoN = GetAnisotropicModifiedNormal(B, N, V, clamp(_Anisotropy, -1, 1));
    //V:从顶点到相机向量，要传-V
    float3 R = normalize(reflect(-V,anisoN));
    
    float3 ks = F_SchlickRoughness(brdfParam.VdotH,F0,Roughness);
    float3 kd = (1.0-ks)*(1.0-_Metallic);


    //-------------------------------使用实时PrefilterMap
    float3 prefilterColor =0;
    //------------------------------------------------------------------------------------------

    //-------------------------------使用预处理PrefilterMap
    //如果使用了预处理的Prefiltermap，可以使用下面这行，由于使用生成的cubemap不是tex2D，不支持三线性插值，所以下面自己简单写了下三线性插值，效果会更好一些
    //如果要使用预处理的Prefiltermap，请找到"FRenderResource"中的cb2并加载使用的cubemap
    float level = (Roughness * 9.0 );   //--简单三线性插值，是因为使用了512分辨率，工9个mipmap，为了方便才这么写的
    float uu = ceil(level);
    float dd = floor(level);
    float3 uPre = TestPrefilter.SampleLevel(samplerTestPrefilter,R,uu);
    float3 dPre = TestPrefilter.SampleLevel(samplerTestPrefilter,R,dd);
    prefilterColor =  (lerp(dPre,uPre,(level-dd)/(uu-dd)));
    //------------------------------------------------------------------------------------------


    //return float4(prefilterColor,1);
    float2 envBrdf = SAMPLE_TEXTURE2D(_LUT, sampler_MainTex, float2(brdfParam.NdotV,Roughness)).xy;
    float3 sp = prefilterColor*(ks*envBrdf.x+envBrdf.y);
    float3 shColor = i.shColor*kd * abledo;
    float4 indirColor =  float4(sp+shColor,0);
    return resColor+indirColor;
}

#endif
