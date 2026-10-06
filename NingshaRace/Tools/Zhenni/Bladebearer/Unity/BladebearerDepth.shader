Shader "NingshaRace/Zhenni/Bladebearer/Depth"
{
    Properties
    {
        _MainTex ("实体轮廓", 2D) = "white" {}
        _DisplacementTex ("循环置换图 RG", 2D) = "gray" {}
        _Color ("透明度", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest+49" "RenderType"="TransparentCutout" }
        Cull Off
        ZWrite On
        ZTest LEqual
        ColorMask 0
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment fragDepth
            #include "BladebearerFlameCommon.cginc"
            ENDCG
        }
    }
    Fallback Off
}
