
#ifndef __OCT__TOOLS__
#define __OCT__TOOLS__

float signNotZero(float f){
    return(f >= 0.0) ? 1.0 : -1.0;
}

float2 signNotZero(float2 v) {
    return float2(signNotZero(v.x), signNotZero(v.y));
}

float2 octEncode(float3 vDirection)
{
    float NormalTotal = abs(vDirection.x)+abs(vDirection.y)+abs(vDirection.z);
    float2 Result = vDirection.xy*(1.0/NormalTotal);
    if(vDirection.z<0.0)
        Result =(1.0-abs(Result.yx))*signNotZero(Result.xy);
    return Result;
}

float3 octDecode(float2 vTextCoord)
{
    float3 Direction = float3(vTextCoord.x, vTextCoord.y, 1.0 - abs(vTextCoord.x) - abs(vTextCoord.y));
    if (Direction.z < 0.0)
        Direction.xy = (1.0 - abs(Direction.yx)) * signNotZero(Direction.xy);
    return normalize(Direction);
}

#endif