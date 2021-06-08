Shader "FRP/Default"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Normal("Normal",2D) = "white"{}
        _Metallic ("Metallic",Range(0,1)) = 0
        _Roughness ("Roughness",Range(0,1)) = 0
        _Anisotropy ("Anisotropy",Range(-1,1)) = 0
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
			ZWrite on

            HLSLPROGRAM
            #include "../Shader/FRP_Default.hlsl"
            #pragma shader_feature _NormalTexOn
            #pragma shader_feature _MetallicTexOn
            #pragma vertex vert
            #pragma fragment frag


            ENDHLSL
        }
    }
    CustomEditor "FRPShaderGUI"
}
