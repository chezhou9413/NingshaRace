Shader "NingshaRace/Zhenni/Bladebearer/BloomBlur"
{
    Properties
    {
        _MainTex ("当前层级", 2D) = "black" {}
        _LowMip ("更低分辨率层级", 2D) = "black" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        sampler2D _LowMip;
        float4 _MainTex_TexelSize;
        float2 _Direction;
        float _Scatter;

        //相机捕获纹理按自身方向标记归一化，后续模糊层保持同一 UV 方向。
        float4 CopySource(v2f_img input) : SV_Target
        {
            float2 uv = input.uv;
            #if UNITY_UV_STARTS_AT_TOP
            if (_MainTex_TexelSize.y < 0.0) uv.y = 1.0 - uv.y;
            #endif
            return float4(tex2D(_MainTex, uv).rgb, 1.0);
        }

        //利用双线性插值合并相邻采样，在五次采样内完成九点高斯模糊。
        float4 Gaussian(v2f_img input) : SV_Target
        {
            float2 offset = _MainTex_TexelSize.xy * _Direction;
            float3 color = tex2D(_MainTex, input.uv).rgb * 0.227027;
            color += tex2D(_MainTex, input.uv + offset * 1.384615).rgb * 0.316216;
            color += tex2D(_MainTex, input.uv - offset * 1.384615).rgb * 0.316216;
            color += tex2D(_MainTex, input.uv + offset * 3.230769).rgb * 0.070270;
            color += tex2D(_MainTex, input.uv - offset * 3.230769).rgb * 0.070270;
            return float4(color, 1.0);
        }

        //平均四个相邻区域，将亮部图缩小至下一级，减轻细小亮点的跳变。
        float4 Downsample(v2f_img input) : SV_Target
        {
            float2 offset = _MainTex_TexelSize.xy * 0.5;
            float3 color = tex2D(_MainTex, input.uv + float2(-offset.x, -offset.y)).rgb;
            color += tex2D(_MainTex, input.uv + float2(offset.x, -offset.y)).rgb;
            color += tex2D(_MainTex, input.uv + float2(-offset.x, offset.y)).rgb;
            color += tex2D(_MainTex, input.uv + float2(offset.x, offset.y)).rgb;
            return float4(color * 0.25, 1.0);
        }

        //按扩散权重合并宽窄两种光晕，避免增加层级时无约束累加亮度。
        float4 Upsample(v2f_img input) : SV_Target
        {
            float3 narrow = tex2D(_MainTex, input.uv).rgb;
            float3 wide = tex2D(_LowMip, input.uv).rgb;
            return float4(lerp(narrow, wide, _Scatter), 1.0);
        }
        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Gaussian
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Downsample
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Upsample
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment CopySource
            ENDCG
        }
    }
    Fallback Off
}
