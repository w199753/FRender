Shader "Unlit/TestShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color",Color) = (1,1,1,1)
        _Anisotropy ("Anisotropy",Range(-1,1)) = 0
    }
    SubShader
    {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        UsePass "Unlit/FRPTransparent/Pass1"
        UsePass "Unlit/FRPTransparent/Pass2"
        Pass 
        {
            Cull Off ZTest On ZWrite On
            Tags { "LightMode" = "FRP_TRANS_DEPTH_PEELING" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_trans_peeling_1

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
                float4 tangent : TANGENT;
            };
            struct v2f
            {
                float2 uv : TEXCOORD0;
                //float3 normal : NORMAL;
                float4 vertex : SV_POSITION;
                float3 worldPos : POSITION1;
                float3 shColor : TEXCOORD1;
                float4 screenPos : TEXCOORD2;

                float3 tangent : TEXCOORD3;
                float3 bitangent : TEXCOORD4;
                float3 normal : TEXCOORD5;                
            };

            #define sampler_MainTex SamplerState_Trilinear_Repeat
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                TEXTURE2D(_MainTex);
                float4 _MainTex_ST;
                float4 _Color;
                int _DepthRenderedIndex;
                TEXTURE2D(_DepthRenderBuffer);
                TEXTURE2D(_DepthTex);
                float _Anisotropy;
                int _MaxDepth;
            CBUFFER_END


            float4 compute(float4 pos)
            {
                float4 o = pos * 0.5f;
                o.xy = float2(o.x, o.y*_ProjectionParams.x) + o.w;
                o.zw = pos.zw;
                return o;
            }
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.normal = TransformObjectToWorldNormal(v.normal);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld,v.vertex);
                o.shColor = CalVertexSH(o.normal);
                o.screenPos = compute(o.vertex);

                float3 w_normal = TransformObjectToWorldNormal(v.normal);
                float3 w_tangent = normalize(mul(unity_ObjectToWorld,float4(v.tangent.xyz,0)).xyz);
                float3 w_bitangent = normalize(cross(w_normal,w_tangent))*v.tangent.w;
                o.normal = w_normal;
                o.tangent = w_tangent;
                o.bitangent = w_bitangent;
                return o;
            }
            struct fout 
            {
                float4 colorBuffer : SV_Target0;
                float4 depthBuffer : SV_Target1;
            };
            fout frag_trans_peeling_1 (v2f i)
            {
                fout o;
                float depth = i.vertex.z;

                float renderdDepth=SAMPLE_TEXTURE2D(_DepthRenderBuffer, sampler_MainTex, i.screenPos.xy/i.screenPos.w).r;
                if(_DepthRenderedIndex>0&&depth>=renderdDepth) discard;

                float3x3 tangentTransform = float3x3(i.tangent, i.bitangent, normalize(i.normal));

                half3 contrib = 0;
                float3 worldPos = i.worldPos;
                half3 N = normalize(i.normal);
                float3 T = normalize(i.tangent);
                T = normalize(i.tangent - dot(i.tangent,N)*N);
                float3 B = normalize(i.bitangent);
                B = normalize(cross(N, T));

                float3 V = normalize(_WorldSpaceCameraPos.xyz - worldPos);
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color;
                float3 resColor = 0;
                half3 L = 0;
                
                BRDFParam brdfParam;
                AnisoBRDFParam anisoBrdfParam;

                for(int idx=0;idx< _LightCount;idx++)
                {
                    Light light = _LightData[idx];
                    half3 lightDir = 0;
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
                    L = lightDir;
                    float3 H = normalize(L+V);
                    InitBRDFParam(brdfParam,N,V,L,H);
                    InitAnisoBRDFParam(anisoBrdfParam,T,B,H,L,V);
                    //col.rgb = col.rgb * max(0,dot(N,L)) * contrib / UNITY_PI + i.shColor*col.rgb;
                    //col.rgb = N;
                    float3 F0 = (col.rgb);
                    resColor += contrib*Disney_BRDF(col.rgb,F0,0.09,_Anisotropy,brdfParam,anisoBrdfParam);
                }


                o.colorBuffer = float4(resColor + i.shColor*col.rgb,col.a);
                //o.colorBuffer = col;
                o.depthBuffer = depth;

                return o;
            }
            ENDHLSL
        }
        UsePass "Unlit/FRPTransparent/Pass4"

    }
}
