Shader "Unlit/PreFilterTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass    //irradiance pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_irradiance

            #include "UnityCG.cginc"
            #include "PreFilterTest.hlsl"
            ENDHLSL
        }

        Pass    //prefitler pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_prefitler

            #include "UnityCG.cginc"
            #include "PreFilterTest.hlsl"
            ENDHLSL
        }

        Pass    //integrateBRDF pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_integrate

            #include "UnityCG.cginc"
            #include "PreFilterTest.hlsl"
            ENDHLSL
        }
    }
}
