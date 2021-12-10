
Shader "Unlit/TestCubemap" {
Properties {
    _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
    [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
    _Rotation ("Rotation", Range(0, 360)) = 0
    [NoScaleOffset] _OctTex ("Texture   (HDR)", 2D) = "white" {}
}

SubShader {
    Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
    Cull Off ZWrite Off

    Pass {

        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0

        #include "UnityCG.cginc"
        #include "octTools.cginc"

        sampler2D _OctTex;
        half4 _OctTex_HDR;
        half4 _Tint;
        half _Exposure;
        float _Rotation;

        float3 RotateAroundYInDegrees (float3 vertex, float degrees)
        {
            float alpha = degrees * UNITY_PI / 180.0;
            float sina, cosa;
            sincos(alpha, sina, cosa);
            float2x2 m = float2x2(cosa, -sina, sina, cosa);
            return float3(mul(m, vertex.xz), vertex.y).xzy;
        }

        struct appdata_t {
            float4 vertex : POSITION;
        };

        struct v2f {
            float4 vertex : SV_POSITION;
            float3 texcoord : TEXCOORD0;
        };

        v2f vert (appdata_t v)
        {
            v2f o;
            float3 rotated = RotateAroundYInDegrees(v.vertex, _Rotation);
            o.vertex = UnityObjectToClipPos(rotated);
            o.texcoord = v.vertex.xyz;
            return o;
        }

        fixed4 frag (v2f i) : SV_Target
        {
            half2 uv = octEncode(i.texcoord);

            //return uv.xyyy;
            // half4 = texCUBE (_OctTex, i.texcoord);
            //return tex2D(_OctTex,uv);
            half4 col = tex2D(_OctTex,(uv + 1.0)/2.0);
            col.rgb = col.rgb * _Tint.rgb * unity_ColorSpaceDouble.rgb;
            col *= _Exposure;
            return half4(col.rgb,1);
            // half3 c = DecodeHDR (tex, _Tex_HDR);
            // c = c * _Tint.rgb * unity_ColorSpaceDouble.rgb;
            // c *= _Exposure;
            // return half4(c, 1);
        }
        ENDCG
    }
}


Fallback Off

}
