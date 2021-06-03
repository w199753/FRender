
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

#endif