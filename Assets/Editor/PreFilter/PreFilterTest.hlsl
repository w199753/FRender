
#ifndef __PRE_FILTER__TEST__
#define __PRE_FILTER__TEST__

#include "../../../Assets/Shader/Montcalo_Library.hlsl"


uniform TextureCube<float3> _EnvMap; 
SamplerState sampler_EnvMap;
uniform int _FaceID;
uniform float _Roughness;

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};
struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
};
sampler2D _MainTex;
float4 _MainTex_ST;

v2f vert (appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    return o;
}


//*based right hand coordinate system*  0:(right) 1:(left) 2:(up) 3:(down) 4:(front) 5:(back)
void GetNormal(float2 uv,out float nx,out float ny,out float nz)
{
    //_FaceID = 5;
    if(_FaceID == 0)//0:(right)
    {
                nx = -0.5f;
        ny = -0.5f+uv.y;
        nz = -0.5f+uv.x;

    }
    else if(_FaceID == 1)//0:(left)
    {
        nx = 0.5f;
        ny = -0.5f+uv.y;
        nz = 0.5f-uv.x;

    }
    else if(_FaceID == 2)//0:(up)
    {
        nx = -0.5f + uv.x;
        ny = 0.5f;
        nz = 0.5f - uv.y;
    }
    else if(_FaceID == 3)//0:(down)
    {
        nx = -0.5f + uv.x;
        ny = -0.5f;
        nz = -0.5f+uv.y;
    }
    else if(_FaceID == 4)//0:(front)
    {
        nx = -0.5f + uv.x;
        ny = -0.5f + uv.y;
        nz = 0.5;
    }
    else if(_FaceID == 5)//0:(back)
    {
        nx = 0.5f - uv.x;
        ny = -0.5f + uv.y;
        nz = -0.5;
    }
}


float4 frag_irradiance (v2f i) : SV_Target
{
    // sample the texture
    fixed4 col = tex2D(_MainTex, i.uv);
    if(_FaceID == 0)//0:(right)
    {
        return (0,0,0,0);

    }
    else if(_FaceID == 1)//0:(left)
    {
return float4(1,0,0,0);

    }
    else if(_FaceID == 2)//0:(up)
    {
return float4(0,0,1,0);
    }
    else if(_FaceID == 3)//0:(down)
    {
return float4(1,1,0,0);
    }
    else if(_FaceID == 4)//0:(front)
    {
return float4(1,0,1,0);
    }
    else if(_FaceID == 5)//0:(back)
    {
return float4(0,1,0,0);
    }

    float nx,ny,nz;
    GetNormal(i.uv,nx,ny,nz);
    float3 N = normalize(float3(nx,ny,nz)); //used with z
    float3 up = abs(N.y)<0.999f ? float3(0,1,0) : float3(0,0,1);
    float3 left = normalize(cross(up,N));
    up = cross(N,left);
    const uint SAMPLE_COUNT = 4096u;
    float3 res = 0;
    float PDF = 0;
    for(uint idx=0;idx<SAMPLE_COUNT;idx++)
    {
        float2 Xi = Hammersley(idx, SAMPLE_COUNT,HaltonSequence(idx));
        float4 sm = CosineSampleHemisphere(Xi);
        PDF = sm.w;
        float3 H = TangentToWorld(sm.xyz,float4(N,1));
        res += _EnvMap.SampleLevel(sampler_EnvMap, H, 0).rgb ;
    }
 
    return float4(res /(float)SAMPLE_COUNT,1);
}

float Sq(float v)
{
    return v*v;
}

float D_GGX(float roughness,float NdotH)
{
	float roughnessFourSqr = Sq(roughness);
	float NdotHSqr = Sq(NdotH);
	return (roughnessFourSqr / ((NdotHSqr * (roughnessFourSqr - 1) + 1) * (NdotHSqr * (roughnessFourSqr - 1) + 1)* UNITY_INV_PI));
}

float4 frag_prefitler(v2f i) :SV_Target
{
    float nx,ny,nz;
    GetNormal(i.uv,nx,ny,nz);
    //没办法，只能用去创建cubemap了，虽然官方推荐使用texture的cube shape，但不知道怎么去写入texture的mip并存到asset，只好用cubemap去存了
    //同时也不知道为什么用cubemap去填充的结果是反的，这里为了解决只好把法线方向y取反，好歹结果是正确的。
    float3 N = normalize(float3(nx,-ny,nz)); 
    const uint SAMPLE_COUNT = 2048u;
    float3 res = 0;
    float3 R = N; float3 V = R;
    float TotalWeight = 0.0;
    for(uint idx=0;idx<SAMPLE_COUNT;idx++)
    {
        float2 Xi = Hammersley(idx, SAMPLE_COUNT,HaltonSequence(idx));
        float4 sm = ImportanceSampleGGX(Xi,_Roughness);
        
        float3 H = TangentToWorld(sm.xyz,float4(N,1));
        float3 L  = reflect(-V,H);  //这里的dwi是根据采样出的法线并根据观察方向反推回去的
        float NdotL = max(dot(N, L), 0.0);
        if(NdotL>0.0)
        {
            float NoH = max(dot(N, H), 0.0);
            float HoV = max(dot(H, V), 0.0);
            float D   = D_GGX(_Roughness, NoH);
            float PDF = D * NoH / (4.0 * HoV) + 0.0001; 
            TotalWeight+= NdotL;
            res += _EnvMap.SampleLevel(sampler_EnvMap, L, 0).rgb * NdotL;
        }

        //H = TangentToWorld(CosOnHalfSphere(Xi),float4(N,1));
        //float4 rot = quat_zto(N);
        //res += _EnvMap.SampleLevel(sampler_EnvMap, quat_rotate(rot,H), 0).rgb;
        //res += _EnvMap.SampleLevel(sampler_EnvMap, H, 0).rgb ;
    }
    return float4(res/TotalWeight,1);
    N = _EnvMap.SampleLevel(sampler_EnvMap, N, 0).rgb;
    return float4(N,1);
}


float G_SmithGGX(float NdotL, float NdotV, float Roughness)
{
	float a = Sq(Roughness);
	float LambdaL = NdotV * (NdotL * (1 - a) + a);
	float LambdaV = NdotL * (NdotV * (1 - a) + a);
	return (0.5 / (LambdaV + LambdaL + 1e-6f)) ;
}

float4 frag_integrate(v2f i):SV_TARGET
{
    const uint SAMPLE_COUNT = 1024u;
    float3 N = float3(0.0, 0.0, 1.0);
    float NdotV = i.uv.x;
    float roughness = i.uv.y;
    float3 V = float3(sqrt(1.0-Sq(NdotV)),0,NdotV);
    float scale = 0.0;
    float bias = 0.0;
    for(uint idx = 0;idx<SAMPLE_COUNT;idx++)
    {
        float2 Xi = Hammersley(idx, SAMPLE_COUNT,HaltonSequence(idx));
        float4 sm = ImportanceSampleGGX(Xi,roughness);
        float3 H = TangentToWorld(sm.xyz,float4(N,1));
        float3 L = reflect(-V,H);

        float NdotL = max(L.z, 0.0);
        float NdotH = max(H.z, 0.0);
        float VdotH = max(dot(V, H), 0.0);

        if (NdotL > 0)
        {
            //1 / NumSample * \int[L * fr * (N.L) / pdf]  with pdf = D(H) * (N.H) / (4 * (V.H)) and fr = F(H) * G(V, L) * D(H) / (4 * (N.L) * (N.V))
            float Vis = G_SmithGGX(NdotL,NdotV,roughness) * 4 * NdotL * VdotH / NdotH;
            float Fc = pow(1.0 - VdotH, 5);

            scale += (1.0 - Fc) * Vis;
            bias += Fc * Vis;
        }
    }
    return float4(scale/float(SAMPLE_COUNT),bias/float(SAMPLE_COUNT),0,1);
}

#endif