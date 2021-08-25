Shader "Unlit/Blur"
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
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

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
                    
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
sampler2D _DepthNormal;
            float4 frag(v2f i) : SV_Target
            {
                float4 o = 0;
                //if (_ProjectionParams.x < 0)
                //i.uv.y = 1-i.uv.y;
                //return tex2D(_DepthNormal,i.uv).w;
                const float gussianKernel[25] = {
                    0.002969, 0.013306, 0.021938, 0.013306, 0.002969,
                    0.013306, 0.059634, 0.098320, 0.059634, 0.013306,
                    0.021938, 0.098320, 0.162103, 0.098320, 0.021938,
                    0.013306, 0.059634, 0.098320, 0.059634, 0.013306,
                    0.002969, 0.013306, 0.021938, 0.013306, 0.002969,
                };

                float2 blurOffset = 1 + 2;

                for (int x = -2; x <= 2; ++x) {
                    for (int y = -2; y <= 2; ++y) {
                        float weight = gussianKernel[x * 5 + y + 11];
                        o += weight * tex2D(_MainTex, i.uv + float2(x, y) * _MainTex_TexelSize.xy * blurOffset);
                    }
                }

                return o;
            }

            ENDHLSL
        }

    }
}
