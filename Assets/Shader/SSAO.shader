﻿Shader "Unlit/CSMCaster"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        //ColorMask R
        Pass
        {
            Name "FRP_TEST"
            Tags{"LightMode" = "FRP_TEST"}
            HLSLPROGRAM
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
                float2 depth : TEXCOORD1;
            };

            sampler2D _MainTex;

            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.depth = o.vertex.zw;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float depth = i.depth.x/i.depth.y;
#if defined (UNITY_REVERSED_Z)
			depth = 1 - depth;       //(1, 0)-->(0, 1)
#else
			depth = depth*0.5 + 0.5; //(-1, 1)-->(0, 1)
#endif
                return (depth);
                //return i.vertex.z/i.vertex.w;
                return 0.2;
            }
            ENDHLSL
        }
    }
}
