Shader "Unlit/CubemapProjectionOct"
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
            #include "octTools.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;

            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            samplerCUBE _Cubemap;


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul((float3x3)unity_ObjectToWorld,v.vertex).xyz;
                return o;
            }



            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = octDecode(i.uv*2 -1);
                //float2 uv = octEncode(dir)
                //return i.worldPos.xyzz;
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                half4 tex = texCUBE (_Cubemap, dir);
                return tex;
                return col;
            }
            ENDCG
        }
    }
}
