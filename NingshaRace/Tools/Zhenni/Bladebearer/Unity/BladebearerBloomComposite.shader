Shader "NingshaRace/Zhenni/Bladebearer/BloomComposite"
{
    Properties
    {
        _MainTex ("场景画面", 2D) = "black" {}
        _BloomTex ("辉光画面", 2D) = "black" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Composite
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _BloomTex;
            float4 _MainTex_TexelSize;

            //将光晕叠加到场景，保留原有亮部并抑制已经接近白色的区域继续过曝。
            float4 Composite(v2f_img input) : SV_Target
            {
                float2 sourceUV = input.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0.0) sourceUV.y = 1.0 - sourceUV.y;
                #endif
                float4 source = tex2D(_MainTex, sourceUV);
                float3 bloom = max(tex2D(_BloomTex, input.uv).rgb, 0.0);
                float3 color = source.rgb + bloom * (1.0 - saturate(source.rgb));
                return float4(color, source.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
