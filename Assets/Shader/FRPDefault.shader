Shader "FRP/Default"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Normal("_Normal",2D) = "white"{}
        _RoughnessTex("_RoughnessTex",2D) = "white"{}
        _MetallicTex("_MetallicTex",2D) = "white"{}
        _EmissionTex("_EmissionTex",2D) = "white"{}
        _LUT ("LUTTex",2D) = "white"{}
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
            #pragma shader_feature _RoughnessTexOn
            #pragma shader_feature _EmissionTexOn
            #pragma vertex vert
            #pragma fragment frag


            ENDHLSL
        }
    }
    CustomEditor "FRPShaderGUI"
}
