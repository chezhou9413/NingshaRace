Shader "NingshaRace/Zhenni/Bladebearer/BloomSource"
{
    Properties
    {
        _MainTex ("原图", 2D) = "white" {}
        _DisplacementTex ("循环置换图 RG", 2D) = "gray" {}
        _Color ("颜色与透明度", Color) = (1,1,1,1)
        _BloomIntensity ("辉光强度", Range(0,2)) = 0.8
        _BloomThreshold ("亮部提取阈值", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest LEqual
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment bloomSource
            #include "BladebearerFlameCommon.cginc"
            float _BloomIntensity, _BloomThreshold;

            //与原图共用位移和时钟；非火焰部件只遮挡来源，不产生辉光。
            half4 bloomSource(v2f i) : SV_Target
            {
                half4 c = sampleFlame(i.uv);
                float brightness = max(c.r, max(c.g, c.b));
                float knee = max(_BloomThreshold * 0.5, 0.0001);
                float soft = clamp(brightness - _BloomThreshold + knee, 0, 2 * knee);
                soft = soft * soft / (4 * knee);
                float contribution = max(brightness - _BloomThreshold, soft) / max(brightness, 0.0001);
                return half4(c.rgb * c.a * contribution * _BloomIntensity, c.a * (1 - _Additive));
            }
            ENDCG
        }
    }
    Fallback Off
}
