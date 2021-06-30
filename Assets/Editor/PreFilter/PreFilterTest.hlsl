
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
    //_FaceID = 5;
    if(_FaceID == 0)//0:(right)
    {
        nx = 0.5f;
        ny = -0.5f+uv.y;
        nz = 0.5f-uv.x;
    }
    else if(_FaceID == 1)//0:(left)
    {

        nx = -0.5f;
        ny = -0.5f+uv.y;
        nz = -0.5f+uv.x;
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

float4 quat_zto(float3 to)
{
    float cosHalfTheta = sqrt(max(0, (to.z + 1) * 0.5));
	//vec3 axisSinTheta = cross(from, to);
	//    0    0    1
	// to.x to.y to.z
	//vec3 axisSinTheta = vec3(-to.y, to.x, 0);
	float twoCosHalfTheta = 2 * cosHalfTheta;
	return float4(-to.y / twoCosHalfTheta, to.x / twoCosHalfTheta, 0, cosHalfTheta);
}
float4 quat_inverse(float4 q)
{
    return float4(-q.xyz,q.w);
}
float3 quat_rotate(float4 q,float3 p)
{
    float4 qp = float4(q.w * p + cross(q.xyz, p), - dot(q.xyz, p));
	float4 invQ = quat_inverse(q);
	float3 qpInvQ = qp.w * invQ.xyz + invQ.w * qp.xyz + cross(qp.xyz, invQ.xyz);
	return qpInvQ;
}

float2 UniformOnDisk(float Xi) {
	float theta = UNITY_PI*2 * Xi;
	return float2(cos(theta), sin(theta));
}

float2 UniformInDisk(float2 Xi) {
	float r = sqrt(Xi.x);
	return r * UniformOnDisk(Xi.y);
}

float3 CosOnHalfSphere(float2 Xi) {
	float r = sqrt(Xi.x);
	float2 pInDisk = r * UniformOnDisk(Xi.y);
	float z = sqrt(1 - Xi.x);
	return float3(pInDisk, z);
}
float4 frag (v2f i) : SV_Target
{
    // sample the texture
    fixed4 col = tex2D(_MainTex, i.uv);
    float nx,ny,nz;
    GetNormal(i.uv,nx,ny,nz);
    float3 N = normalize(float3(nx,ny,nz)); //used with z
    float3 up = abs(N.y)<0.999f ? float3(0,1,0) : float3(0,0,1);
    float3 left = normalize(cross(up,N));
    up = cross(N,left);
    const uint SAMPLE_COUNT = 4096;
    float3 res = 0;
    float PDF = 0;
    for(uint idx=0;idx<SAMPLE_COUNT;idx++)
    {
        float2 Xi = Hammersley(idx, SAMPLE_COUNT,HaltonSequence(idx));
        float4 sm = CosineSampleHemisphere(Xi);
        PDF = sm.w;
        float3 H = TangentToWorld(sm.xyz,float4(N,1));

        //H = TangentToWorld(CosOnHalfSphere(Xi),float4(N,1));
        float4 rot = quat_zto(N);
        //res += _EnvMap.SampleLevel(sampler_EnvMap, quat_rotate(rot,H), 0).rgb;
        res += _EnvMap.SampleLevel(sampler_EnvMap, H, 0).rgb ;
    }
 
    return float4(  res /(float)SAMPLE_COUNT,1);
    N = _EnvMap.SampleLevel(sampler_EnvMap, N, 0).rgb;
    return float4(N,1);
    return float4(1,0,1,0);
}

#endif