
#ifndef __FRP__LIGHT__
#define __FRP__LIGHT__

struct Light
{
    float4 pos_type;  //xyz(pos) w(type)
    float4 geometry;  //xyz(dir) w(radius)
    float4 color;
};

int _LightCount;
StructuredBuffer<Light> _LightData;

float3 CalDirLightContribution(Light light )
{
    float3 light_contri = light.color;
    return light_contri;
}

float3 CalPointLightContribution(Light light, float3 worldPos)
{
    float3 delta = light.pos_type.xyz - worldPos.xyz;
    float distance = length(delta);
    if(distance > light.geometry.w) return 0 ;
    float3 l = delta / distance;
    float satu = saturate(1 - distance / light.geometry.w);
    float3 light_contri = satu * light.color.rgb ;
    return light_contri;
    //return light_contri * max(0,dot(N,normalize(delta)));
}

#endif