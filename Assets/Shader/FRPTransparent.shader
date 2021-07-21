Shader "Unlit/FRPTransparent"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color",Color) = (1,1,1,1)
    }
    SubShader
    {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Pass
        {
            Cull Off Lighting Off ZWrite Off //ZTest on
            Blend SrcAlpha OneMinusSrcAlpha
            Tags { "LightMode" = "FRP_BASE" }
            HLSLPROGRAM
            #include "/FRP_Transparent.hlsl"
            #pragma vertex vert
            #pragma fragment frag_trans_default_1

            ENDHLSL
        }
    }
}
