#ifndef __FRP__DEFAULT__
#define __FRP__DEFAULT__


#include "../Shader/InputMacro.hlsl"
#include "../Shader/UnityBuiltIn.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

#include "../Shader/FRP_SH.hlsl"
#include "../Shader/FRP_Light.hlsl"
#include "../Shader/FRP_Shadow.hlsl"
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
TEXTURE2D(_MetallicTex);
TEXTURE2D(_EmissionTex);
TEXTURE2D(_LUT);
//TEXTURE2D(_SMShadowMap);

float NewTest;

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
    float3 shColor : TEXCOORD2;

    float3 tangent :TEXCOORD3;
    float3 bitangent :TEXCOORD4;
    float3 normal :TEXCOORD5;

    float4 sm_coord0 : TEXCOORD6;
    float4 sm_coord1 : TEXCOORD7;
    float4 sm_coord2 : TEXCOORD8;
    float4 sm_coord3 : TEXCOORD9;
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
    
    o.sm_coord0 = mul(_LightVPArray[0],mul(unity_ObjectToWorld,v.vertex));
    o.sm_coord1 = mul(_LightVPArray[1],mul(unity_ObjectToWorld,v.vertex));
    o.sm_coord2 = mul(_LightVPArray[2],mul(unity_ObjectToWorld,v.vertex));
    o.sm_coord3 = mul(_LightVPArray[3],mul(unity_ObjectToWorld,v.vertex));
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


inline half3 LinearToGammaSpace (half3 linRGB)
{
    linRGB = max(linRGB, half3(0.h, 0.h, 0.h));
    // An almost-perfect approximation from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
    return max(1.055h * pow(linRGB, 0.416666667h) - 0.055h, 0.h);

    // Exact version, useful for debugging.
    //return half3(LinearToGammaSpaceExact(linRGB.r), LinearToGammaSpaceExact(linRGB.g), LinearToGammaSpaceExact(linRGB.b));
}

//-------------------------------

half4 frag (v2f i) : SV_Target
{
    //float3 sh = LinearToGammaSpace(i.shColor);
    //sh = pow(i.shColor,2.2);
    //return i.shColor.xyzz;
    return NewTest;
    float4 abledo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    float4 resColor = 0;
//  float4 front = SAMPLE_TEXTURE2D(_SMShadowMap,sampler_MainTex,i.uv);
//  return front;
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

#if _MetallicTexOn
    float Metallic = SAMPLE_TEXTURE2D(_MetallicTex,sampler_MainTex,i.uv).r;
#else
    float Metallic = _Metallic;
#endif

#if _RoughnessTexOn
    Roughness = SAMPLE_TEXTURE2D(_RoughnessTex, sampler_MainTex, i.uv).r;
#else
    Roughness = _Roughness;
#endif

#if _EmissionTexOn
    float4 emission = SAMPLE_TEXTURE2D(_EmissionTex,sampler_MainTex,i.uv);
#else
    float4 emission = 0;
#endif

    float3 F0 ;
    CalMaterialF0(abledo,Metallic,F0);

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
    float shadow = 0;
    float4x4 sm_coords = float4x4(i.sm_coord0,i.sm_coord1,i.sm_coord2,i.sm_coord3);
    //return getCascadeWeights(i.vertex.w);
    for(int idx=0;idx< _LightCount;idx++)
    {
        Light light = _LightData[idx];
        if(idx == 0)
        {
            float4 weights = getCascadeWeights(i.vertex.w);
            float b1 = min(0.002 * clamp((1.0 - dot(N, light.pos_type.xyz)),0.2,1), 0.002);
            float b2 = min(0.005 * clamp((1.0 - dot(N, light.pos_type.xyz)),0.22,1), 0.005);
            float b3 = min(0.009 * clamp((1.0 - dot(N, light.pos_type.xyz)),0.23,1), 0.009);
            float b4 = min(0.013 * clamp((1.0 - dot(N, light.pos_type.xyz)),0.25,1), 0.013);
            float4 bias = float4(b1,b2,b3,b4);
            //return weights;
            shadow = getShadow(sm_coords,weights,bias);
        }   
        else
        {
            shadow = 1;
        }
        if(light.pos_type.w == 1)
        {
            contrib = CalDirLightContribution(light);
            //if(idx==0)
            //else
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
        resColor += float4(contrib*BRDF_CookTorrance(abledo.rgb,F0,Metallic,Roughness,_Anisotropy,brdfParam,anisoBrdfParam),0);
        //resColor += float4(contrib*Disney_BRDF(abledo.rgb,F0,Roughness,_Anisotropy,brdfParam,anisoBrdfParam),0);
    }
    //return shadow;
    //return float4(resColor);
    float3 anisoN = GetAnisotropicModifiedNormal(B, N, V, clamp(_Anisotropy, -1, 1));
    //V:从顶点到相机向量，要传-V
    float3 R = normalize(reflect(-V,anisoN));
    
    float3 ks = F_SchlickRoughness(brdfParam.VdotH,F0,Roughness);
    float3 kd = (1.0-ks)*(1.0-Metallic);


    //-------------------------------使用实时PrefilterMap
    float3 prefilterColor =0;
    //------------------------------------------------------------------------------------------
    
    //-------------------------------使用预处理PrefilterMap
    //如果使用了预处理的Prefiltermap，可以使用下面这行，由于使用生成的cubemap不是tex2D，不支持三线性插值，所以下面自己简单写了下三线性插值，效果会更好一些
    //如果要使用预处理的Prefiltermap，请找到"FRenderResource"中的cb2并加载使用的cubemap
    //粗糙度和mipmap不是线性关系，要转化一下 https://blog.csdn.net/qq_38275140/article/details/86145803 
    float r = Roughness * 1.7 - 0.7*Roughness*Roughness;
    float level = clamp(r*8.0,0.0001,7.999) ;//--简单三线性插值，是因为使用了512分辨率，工9个mipmap，为了方便才这么写的
    //return TestPrefilter.SampleLevel(samplerTestPrefilter,R,level).xyzz;
    float uu = ceil(level);
    float dd = floor(level);
    float3 uPre = TestPrefilter.SampleLevel(samplerTestPrefilter,R,uu);
    float3 dPre = TestPrefilter.SampleLevel(samplerTestPrefilter,R,dd);
    //return float4(dPre,1);
    prefilterColor =  (lerp(dPre,uPre,(level-dd)/(uu-dd)));
    //return prefilterColor.xyzz;
    //没有hdr的图，没有做tonemapping

    //------------------------------------------------------------------------------------------

    float2 envBrdf = SAMPLE_TEXTURE2D(_LUT, sampler_MainTex, float2(lerp(0.001,0.999,brdfParam.NdotV),lerp(0.01,0.99,Roughness))).xy;
    float3 sp = prefilterColor*(ks*envBrdf.x+envBrdf.y);
    float3 shColor = pow(i.shColor,1)*kd * abledo;
    return pow(i.shColor,1).xyzz;
    float4 indirColor =  float4(sp+shColor,0);
    //return indirColor;
    //indirColor = 0;
    //return shColor.xyzz ;
    return (resColor*2 + indirColor + emission);
}


#endif
