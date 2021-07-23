Shader "Unlit/FRPTransparent"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color",Color) = (1,1,1,1)
        _AlphaClip ("AlphaClip",Range(0,1)) = 0.2
    }
    SubShader
    {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Pass //pass 0
        {
            Cull Off Lighting Off ZWrite On ZTest On
            ColorMask 0
            Tags { "LightMode" = "FRP_TRANS_NORMAL" }
            HLSLPROGRAM
            #include "/FRP_Transparent.hlsl"
            #pragma vertex vert
            #pragma fragment frag_trans_default_1

            ENDHLSL
        }
        Pass //pass 1
        {
            Cull Off Lighting Off ZWrite Off ZTest On
            Blend SrcAlpha OneMinusSrcAlpha
            Tags { "LightMode" = "FRP_TRANS_NORMAL1" }
            HLSLPROGRAM
            #include "/FRP_Transparent.hlsl"
            #pragma vertex vert
            #pragma fragment frag_trans_default_2

            ENDHLSL
        }
        //"FRP_TRANS_DEPTH_PEELING"
        Pass //pass 2
        {
            Cull Off ZTest On ZWrite On
            
            //Blend SrcAlpha OneMinusSrcAlpha
            Tags { "LightMode" = "FRP_TRANS_DEPTH_PEELING" }
            HLSLPROGRAM
            #include "/FRP_Transparent.hlsl"
            #pragma vertex vert
            #pragma fragment frag_trans_peeling_1

            ENDHLSL
        }
        Pass //pass 3
        {
            Cull back Lighting Off ZWrite On ZTest On
            Blend One OneMinusSrcAlpha
            Tags { "LightMode" = "FRP_TRANS_DEPTH_PEELING" }
            HLSLPROGRAM
            #include "/FRP_Transparent.hlsl"
            #pragma vertex vert
            #pragma fragment frag_trans_peeling_2

            ENDHLSL
        }
    }
}
