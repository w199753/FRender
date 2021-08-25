﻿Shader "Unlit/DitherPass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Alpha ("Alpha",Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        //Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags{"LightMode" = "FRP_BASE"}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

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
            float _Alpha;
sampler3D _DitherMaskLOD;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                float ditherArray[16] = {
                    17.0/17.0, 9.0/17.0, 3.0/17.0, 11.0/17.0,
                    13.0/17.0, 5.0/17.0, 15.0/17.0, 7.0/17.0,
                    4.0/17.0, 12.0/17.0, 2.0/17.0, 10.0/17.0,
                    16.0/17.0, 8.0/17.0, 14.0/17.0, 6.0/17.0,
                };
                uint index = (uint(i.uv.x)%4)*4 +uint(i.uv.y)%4;
                half alphaRef = tex3D( _DitherMaskLOD, float3( i.vertex.xy * 0.25, _Alpha * 0.9375 ) ).a;
                //col = alphaRef;
                clip( alphaRef - 0.01 );
                //col.a = 1;
                return col;
            }
            ENDCG
        }
    }
}
