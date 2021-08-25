#ifndef __FRP__DEPTH_NORMAL__PASS__
#define __FRP__DEPTH_NORMAL__PASS__

#include "../Shader/InputMacro.hlsl"
#include "../Shader/UnityBuiltIn.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

#include "../Shader/FRP_SH.hlsl"
#include "../Shader/FRP_Light.hlsl"
#include "../Shader/FRP_BRDF.hlsl"
#include "../Shader/Montcalo_Library.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 vertex : SV_POSITION;
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

// Z buffer to linear 0..1 depth
inline float linear01( float z )
{
    return 1.0 / (_ZBufferParams.x * z + _ZBufferParams.y);
}
// Z buffer to linear depth
inline float linearEye( float z )
{
    return 1.0 / (_ZBufferParams.z * z + _ZBufferParams.w);
}
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                float3 w_normal = TransformObjectToWorldNormal(v.normal);
                o.normal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float depth = i.vertex.z/i.vertex.w;
                depth = linear01(depth);
                float3 normal = normalize(i.normal);
                return float4(normal,depth);
            }

#endif