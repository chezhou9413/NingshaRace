//职责：在静态砂岩上叠加稀薄的流动沙尘，保留底纹、边框与前景内容的主体质感。
Shader "Ningsha/UI/DriftingSand"
{
    Properties
    {
        _MainTex ("输入贴图", 2D) = "white" {}
        _NoiseTex ("四尺度风沙噪声", 2D) = "gray" {}
        _GrainTex ("自由散布细沙", 2D) = "black" {}
        _FlowTime ("流动时间", Float) = 0
        _DeepSand ("砂岩底色", Color) = (0.13, 0.105, 0.075, 1)
        _CloudSand ("沉积沙色", Color) = (0.23, 0.18, 0.115, 1)
        _GrainSand ("受光细沙", Color) = (0.83, 0.69, 0.46, 1)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Blend Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "SandstormField.cginc"

            float4 _DeepSand;
            float4 _CloudSand;
            float4 _GrainSand;

            //职责：合成与静态底纹同色系的薄沙、碎裂噪声和两层颗粒，不使用边缘活动遮罩。
            fixed4 frag(v2f_img input) : SV_Target
            {
                float2 uv = input.uv;
                float2 wind = float2(-0.058, 0.012) * _FlowTime;
                float2 warp = StormWarp(uv);
                float billow;
                float shreds;
                float density = StormDensity(uv, wind, warp, billow, shreds);

                //微小细沙占主导，少量大粒采用错向坐标取样，涡流进一步打散移动轨迹。
                float3 grain = GrainField(uv, wind, warp);
                float grainLight = saturate(grain.r * 0.80 + grain.g * 0.28) * lerp(0.65, 1.0, density);
                float powder = tex2D(_NoiseTex, uv * 4.0 + wind * 2.1 + warp).a;
                float veil = saturate(0.30 + density * 0.58 + (powder - 0.5) * 0.09);
                float3 color = lerp(_DeepSand.rgb, _CloudSand.rgb, veil);
                color *= 1.0 + (grain.b - 0.5) * 0.14;
                color += _CloudSand.rgb * shreds * 0.025;

                //受光沙粒只作细微点缀，不叠加整板压黑或高亮色幕。
                color = lerp(color, _GrainSand.rgb, grainLight * 0.62);
                //稀疏处近乎透明，最浓处也保留至少七成静态砂岩与积沙纹理。
                float opacity = 0.02 + density * 0.16 + grainLight * 0.10;
                return fixed4(color, opacity);
            }
            ENDCG
        }
    }
}
