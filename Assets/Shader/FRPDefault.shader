Shader "FRP/Default"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Name "FRP_BASE"
            Tags{"LightMode" = "FRP_BASE"}

            ZTest on
            ZWrite off

            HLSLPROGRAM
            #include "../Shader/FRP_Default.hlsl"
            #pragma vertex vert
            #pragma fragment frag


            ENDHLSL
        }
    }
}
