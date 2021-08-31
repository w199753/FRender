Shader "Unlit/SSPR"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
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
            sampler2D _CameraDepthTex;
sampler2D _Test;
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
                //if (_ProjectionParams.x < 0)
                //    i.uv.y = 1-i.uv.y;
                fixed4 col = tex2D(_Test, i.uv);
                fixed4 aa = tex2D(_MainTex,i.uv);
                float depth = tex2D(_CameraDepthTex,i.uv).r;
                //depth = Linear01Depth(depth);
                if(depth == 0)
                return aa;
                else
                //aa = 0;
                return col*0.3 + aa;
            }
            ENDCG
        }
    }
}
