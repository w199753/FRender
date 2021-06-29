
#ifndef __PRE_FILTER__TEST__
#define __PRE_FILTER__TEST__

#include "../../../Assets/Shader/Montcalo_Library.hlsl"

uniform TextureCube<float3> _EnvMap; 
SamplerState sampler_EnvMap;
uniform int _FaceID;
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


//*based left hand coordinate system*  0:(right) 1:(left) 2:(up) 3:(down) 4:(front) 5:(back)
void GetNormal(float2 uv,out float nx,out float ny,out float nz)
{
    if(_FaceID == 0)//0:(right)
    {
        nx = 0.5f;
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
        nx = 0.5f;
        ny = -0.5f+uv.y;
        nz = 0.5f-uv.x;
    }
    else if(_FaceID == 3)//0:(down)
    {
        nx = 0.5f;
        ny = -0.5f+uv.y;
        nz = 0.5f-uv.x;
    }
    else if(_FaceID == 4)//0:(right)
    {
        
    }
    else if(_FaceID == 5)//0:(right)
    {
        
    }
}
fixed4 frag (v2f i) : SV_Target
{
    // sample the texture
    fixed4 col = tex2D(_MainTex, i.uv);
    float nx,ny,nz;
    GetNormal(i.uv,nx,ny,nz);
    float3 N = normalize(float3(nx,ny,nz)); //used with z
    float3 up = abs(N.y)<0.999f ? float3(0,1,0) : float3(0,0,1);
    float3 left = normalize(cross(up,N));
    up = cross(N,left);
    const int SAMPLE_COUNT = 1024;
    float3 res = 0;
    for(int i=0;i<SAMPLE_COUNT;i++)
    {
        float2 Xi = Hammersley(i, SAMPLE_COUNT);
        float3 H = TangentToWorld(UniformSampleHemisphere(Xi).xyz,float4(up,1));
        res += _EnvMap.SampleLevel(sampler_EnvMap, H, 0).rgb;
        //return float4(res,1);
    }
    return float4(res/SAMPLE_COUNT ,1);
    N = _EnvMap.SampleLevel(sampler_EnvMap, N, 0).rgb;
    return float4(N,1);
    return float4(1,0,1,0);
}

#endif