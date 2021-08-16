Shader "Unlit/NewBoxFilter"
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
            #pragma target 3.0
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

            
            sampler2D u_SourceTexture;
            sampler2D u_CoarserTexture;
            sampler2D _TempDepth;
            float4 g_focus;
            int g_level;

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

float MipGaussianBlendWeight(float2 tex)
{
    float g_sigma = 1.5;
	float sigma = g_sigma;
	if (uint(g_focus.x) != 0xffffffff)
	{
		float2 r = (2.0 * tex - 1.0) - g_focus;
		//sigma *= dot(r, r);
	}
	float sigma2 = sigma * sigma;
    //return sigma2;
	float c = 2.0 * 3.1415926 * sigma2;
	float numerator = (1 << (g_level << 2)) * log(4.0);
	float denorminator = c * ((1 << (g_level << 1)) + c);
	return clamp(numerator / denorminator,0,1);

}

float GaussianExp(float sigma2, uint level)
{
	return -(1 << (level << 1)) / (2.0 * 3.1415926 * sigma2);
};

//--------------------------------------------------------------------------------------
// Calculate Gaussian basis
//--------------------------------------------------------------------------------------
float GaussianBasis(float sigma2, uint level)
{
	return level < 0.0 ? 0.0 : exp(GaussianExp(sigma2, level));
};

float MipGaussianWeight(float sigma2, uint level)
{
	float g = GaussianBasis(sigma2, level);

	return (1 << (level << 2)) * g;
}

float W(float2 tex)
{    float g_sigma = 2;
    	float sigma = g_sigma;
	if (uint(g_focus.x) != 0xffffffff)
	{
		float2 r = (2.0 * tex - 1.0) - g_focus;
		//sigma *= dot(r, r);
	}
	float sigma2 = sigma * sigma;
    float wsum = 0.0, weight = 0.0;
	for (uint i = g_level; i < 12; ++i)
	{
		float w = MipGaussianWeight(sigma2, i);
		weight = i == g_level ? w : weight;
		wsum += w;
	}
	return wsum > 0.0 ? weight / wsum : 1.0;
}

            fixed4 frag (v2f i) : SV_Target
            {
                float2 v2f_TexCoords = i.uv;
                
                //前2-4级贡献太小，取个差不就好了
                // if(g_level >=10)
                // {
                //     //col = tex2Dlod(u_CoarserTexture, float4(v2f_TexCoords,0,11)).rgba;
                //     return 0.5;
                //     //return col;
                // }
                float3 Color = tex2Dlod(u_SourceTexture, float4(v2f_TexCoords,0,g_level + 1)).rgb;
                float weight = MipGaussianBlendWeight(v2f_TexCoords);
                //weight = W(v2f_TexCoords);
                float3 src = tex2Dlod(u_CoarserTexture, float4(v2f_TexCoords,0,g_level)).rgb;
                float4 col = float4((1 - weight) * Color + weight * src, 1.0f);

                //9级前取0.5
                float4 colLess10 = saturate(step(g_level,9)*col + step(9,g_level)*0.5);
                return colLess10;
            }
            ENDHLSL
        }
    }
}
