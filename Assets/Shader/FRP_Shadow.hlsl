
#ifndef __FRP_SHADOW__
#define __FRP_SHADOW__
#include "../Shader/InputMacro.hlsl"
#include "../Shader/UnityBuiltIn.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "../Shader/UnityHLSL.hlsl"
#include "../Shader/Noise_Library.hlsl"


CBUFFER_START (FRP_Shadow)
    TEXTURE2D_ARRAY(_SMShadowMap);
    float4x4 _LightVPArray[4];
    float4 _LightSplitNear;
    float4 _LightSplitFar;
    int _ShadowType;
CBUFFER_END

#define sampler_SMShadowMap SamplerState_Point_Clamp
SAMPLER(sampler_SMShadowMap);

#define sampler_VSMShadowMap SamplerState_Linear_Clamp
SAMPLER(sampler_VSMShadowMap);

struct v2f_shadow
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
    float2 depth : TEXCOORD1;
};

struct appdata_shadow
{
    float4 vertex : POSITION;

};

float transferDepth(float z)
{
    float res = z;
#if defined (UNITY_REVERSED_Z)
	res = 1 - res;       //(1, 0)-->(0, 1)
#else 
	res = res*0.5 + 0.5; //(-1, 1)-->(0, 1)
#endif
    return res;
}



v2f_shadow vert_shadow(appdata_shadow v)
{
    v2f_shadow o;
    o.vertex = TransformObjectToHClip(v.vertex.xyz);
    o.depth = o.vertex.zw;
    return o;
}

float4 frag_sm (v2f_shadow i) : SV_TARGET
{
    float depth = i.depth.x/i.depth.y;
    depth = transferDepth(depth);
    return float4(depth,0,0,0);
}

float4 frag_vsm (v2f_shadow i) : SV_TARGET
{
    float depth = i.depth.x/i.depth.y;
    depth = transferDepth(depth);
    return float4(depth,depth*depth,0,0);
}

float4 frag_esm (v2f_shadow i) :SV_TARGET
{
    float depth = i.depth.x/i.depth.y;
    depth = transferDepth(depth);
    return 0;
    //return exp(_)
}

//-------------------------------
float4 getCascadeWeights(float z)
{
	float4 zNear = float4(z >= _LightSplitNear);
	float4 zFar = float4(z < _LightSplitFar);
	float4 weights = zNear * zFar;
	return weights;
}
float sampleShadowPCF(float4 weights,float4 ndc0,float4 ndc1,float4 ndc2,float4 ndc3,float4 bias,float2 offset)
{
    float depth0 = transferDepth(ndc0.z);
    float depth1 = transferDepth(ndc1.z);
    float depth2 = transferDepth(ndc2.z);
    float depth3 = transferDepth(ndc3.z);

    float2 uv0 = ndc0.xy*0.5+0.5;
    float2 uv1 = ndc1.xy*0.5+0.5;
    float2 uv2 = ndc2.xy*0.5+0.5;
    float2 uv3 = ndc3.xy*0.5+0.5;
    float shadow0 = 0;
    float shadow1 = 0;
    float shadow2 = 0;
    float shadow3 = 0;
    uniformDiskSamples(uv0);

    //---test
    
    for(int idx = 0;idx<NUM_SAMPLES;idx++)
    {
        float d0 = SAMPLE_TEXTURE2D_ARRAY(_SMShadowMap,sampler_SMShadowMap,uv0+poissonDisk[idx]*(1.0/1024.0),0).r;
        shadow0 += min(max(0.0,step(depth0-bias[0],d0)),1);
    }
    for(idx = 0;idx<NUM_SAMPLES;idx++)
    {
        float d1 = SAMPLE_TEXTURE2D_ARRAY(_SMShadowMap,sampler_SMShadowMap,uv1+poissonDisk[idx]*(1.0/1024.0),1).r;
        shadow1 += min(max(0.0,step(depth1-bias[1],d1)),1);
    }
    for(idx = 0;idx<NUM_SAMPLES;idx++)
    {
        float d2 = SAMPLE_TEXTURE2D_ARRAY(_SMShadowMap,sampler_SMShadowMap,uv2+poissonDisk[idx]*(1.0/1024.0),2).r;
        shadow2 += min(max(0.0,step(depth2-bias[2],d2)),1);
    }
    for(idx = 0;idx<NUM_SAMPLES;idx++)
    {
        float d3 = SAMPLE_TEXTURE2D_ARRAY(_SMShadowMap,sampler_SMShadowMap,uv3+poissonDisk[idx]*(1.0/1024.0),3).r;
        shadow3 += min(max(0.0,step(depth3-bias[3],d3)),1);
    }
    shadow0/=NUM_SAMPLES;
    shadow1/=NUM_SAMPLES;
    shadow2/=NUM_SAMPLES;
    shadow3/=NUM_SAMPLES;

    float res = shadow0*weights[0] +shadow1*weights[1] +shadow2*weights[2] +shadow3*weights[3];
    return res;
}

float sampleShadowSM(float4 weights,float4 ndc0,float4 ndc1,float4 ndc2,float4 ndc3,float4 bias)
{
    float depth0 = transferDepth(ndc0.z);
    float depth1 = transferDepth(ndc1.z);
    float depth2 = transferDepth(ndc2.z);
    float depth3 = transferDepth(ndc3.z);

    float2 uv0 = ndc0.xy*0.5+0.5;
    float2 uv1 = ndc1.xy*0.5+0.5;
    float2 uv2 = ndc2.xy*0.5+0.5;
    float2 uv3 = ndc3.xy*0.5+0.5;
//return SAMPLE_TEXTURE2D_ARRAY(_SMShadowMap,sampler_SMShadowMap,uv0,0).r;
    float d0 = SAMPLE_TEXTURE2D_ARRAY(_SMShadowMap,sampler_SMShadowMap,uv0,0).r;
    float shadow0 = min(max(0.0,step(depth0-bias[0],d0)),1);

    float d1 = SAMPLE_TEXTURE2D_ARRAY(_SMShadowMap,sampler_SMShadowMap,uv1,1).r;
    float shadow1 = min(max(0.0,step(depth1-bias[1],d1)),1);

    float d2 = SAMPLE_TEXTURE2D_ARRAY(_SMShadowMap,sampler_SMShadowMap,uv2,2).r;
    float shadow2 = min(max(0.0,step(depth2-bias[2],d2)),1);

    float d3 = SAMPLE_TEXTURE2D_ARRAY(_SMShadowMap,sampler_SMShadowMap,uv3,3).r;
    float shadow3 = min(max(0.0,step(depth3-bias[3],d3)),1);

    float res = shadow0*weights[0] +shadow1*weights[1] +shadow2*weights[2] +shadow3*weights[3];
    return res;
}


////v <= min return 0
////v >= max return 1
float linstep(float minValue, float maxValue, float v)
{
	return clamp((v - minValue)/(maxValue - minValue), 0, 1);
}

//p_max：不在阴影的概率
//Amount：只要p_max < Amount，p_max = 0
//映射关系：
//f(p_max) = saturate((p_max - Amount)/(1 - Amount))
//简单的来说就把[amount,1]线性映射到[0,1]
float ReduceLightBleeding(float p_max, float Amount)
{  
	return linstep(Amount, 1, p_max);
}

float chebyshev(float mx,float my,float depth,float v_min)
{
    float v = my - mx*mx;  //方差
    //return ex_2;
    v = max(v,v_min);
    float md = depth-mx;
    float md2 = md*md;
    float p_max = v/(v+md2);
    p_max = ReduceLightBleeding(p_max,0.01);
    float h = step(depth,mx);
    return max(p_max,h);
}

float sampleShadowVSM(float4 weights,float4 ndc0,float4 ndc1,float4 ndc2,float4 ndc3)
{
    float depth0 = transferDepth(ndc0.z);
    float depth1 = transferDepth(ndc1.z);
    float depth2 = transferDepth(ndc2.z);
    float depth3 = transferDepth(ndc3.z);

    float2 uv0 = ndc0.xy*0.5+0.5;
    float2 uv1 = ndc1.xy*0.5+0.5;
    float2 uv2 = ndc2.xy*0.5+0.5;
    float2 uv3 = ndc3.xy*0.5+0.5;
    
//return SAMPLE_TEXTURE2D_ARRAY(_SMShadowMap,sampler_SMShadowMap,uv0,0).rggg;
    float2 d0 = SAMPLE_TEXTURE2D_ARRAY(_SMShadowMap,sampler_VSMShadowMap,uv0,0).rg;
    float2 d1 = SAMPLE_TEXTURE2D_ARRAY(_SMShadowMap,sampler_VSMShadowMap,uv1,1).rg;
    float2 d2 = SAMPLE_TEXTURE2D_ARRAY(_SMShadowMap,sampler_VSMShadowMap,uv2,2).rg;
    float2 d3 = SAMPLE_TEXTURE2D_ARRAY(_SMShadowMap,sampler_VSMShadowMap,uv3,3).rg;
    float shadow0 = chebyshev(d0.x,d0.y,depth0,0.001f);
    float shadow1 = chebyshev(d1.x,d1.y,depth1,0.001f);
    float shadow2 = chebyshev(d2.x,d2.y,depth2,0.001f);
    float shadow3 = chebyshev(d3.x,d3.y,depth3,0.001f);

    float res = shadow0*weights[0] +shadow1*weights[1] +shadow2*weights[2] +shadow3*weights[3];
    return res;
}

float getShadow(float4x4 sm_coord ,float4 weights,float4 bias)
{
    float4 ndc0 = (sm_coord[0]/sm_coord[0].w);
    float4 ndc1 = (sm_coord[1]/sm_coord[1].w);
    float4 ndc2 = (sm_coord[2]/sm_coord[2].w);
    float4 ndc3 = (sm_coord[3]/sm_coord[3].w);
    float res = 0;
    if(_ShadowType == 0)
    {
        res = sampleShadowSM(weights,ndc0,ndc1,ndc2,ndc3,bias); 
        
    }
    else if(_ShadowType == 1)
    {
        res = sampleShadowPCF(weights,ndc0,ndc1,ndc2,ndc3,bias,0); 
    }
    else if(_ShadowType == 2)
    {
        return 1;
    }
    else if(_ShadowType == 3)
    {
        res = sampleShadowVSM(weights,ndc0,ndc1,ndc2,ndc3);
    }
    else if(_ShadowType == 4)
    {
        return 1;
    }
    else
    {
        return 1;
    }

    return res;
}


#endif