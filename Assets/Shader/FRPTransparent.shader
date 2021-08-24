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
            Name "Pass1"
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
            Name "Pass2"
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
            Name "Pass3"
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
            Name "Pass4"
            Cull Off ZWrite On ZTest On
            Blend SrcAlpha OneMinusSrcAlpha
            Tags { "LightMode" = "FRP_TRANS_DEPTH_PEELING" }
            HLSLPROGRAM
            #include "/FRP_Transparent.hlsl"
            #pragma vertex vert
            #pragma fragment frag_trans_peeling_2

            ENDHLSL
        }



//--shadow 
        Pass
        {
            Name "FRP_ShadowCaster_SM"
            Tags{"LightMode" = "FRP_ShadowCaster_SM"}
            ColorMask R
            // Cull Front
            HLSLPROGRAM
            #include "FRP_Shadow.hlsl"
            #pragma vertex vert_shadow
            #pragma fragment frag_sm

            ENDHLSL
        }

        Pass
        {
            Name "FRP_ShadowCaster_VSM"
            Tags{"LightMode" = "FRP_ShadowCaster_VSM"}
            ColorMask RG
            // Cull Front
            HLSLPROGRAM
            #include "FRP_Shadow.hlsl"
            #pragma vertex vert_shadow
            #pragma fragment frag_vsm

            ENDHLSL
        }

        Pass
        {
            Name "FRP_ShadowCaster_ESM"
            Tags{"LightMode" = "FRP_ShadowCaster_ESM"}
            ColorMask R
            // Cull Front
            HLSLPROGRAM
            #include "FRP_Shadow.hlsl"
            #pragma vertex vert_shadow
            #pragma fragment frag_esm

            ENDHLSL
        }

        Pass
        {
            Name "FRP_DepthNormalPass"
            Tags{"LightMode" = "FRP_DepthNormalPass"}
            HLSLPROGRAM
            #include "FRPDepthNormalPass.hlsl"
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}
