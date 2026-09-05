//职责：在离屏底纹中生成细沙、风蚀纹和沉积层，为 IMGUI 提供可重复铺设的古砂岩表面。
Shader "Ningsha/UI/WeatheredSandstone"
{
    Properties
    {
        _MainTex ("输入贴图", 2D) = "white" {}
        _StoneColor ("深砂岩", Color) = (0.13, 0.105, 0.075, 1)
        _VeinColor ("沉积纹", Color) = (0.23, 0.18, 0.115, 1)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _StoneColor;
            float4 _VeinColor;

            //函数职责：以固定格坐标生成不依赖外部图片的砂砾分布。
            float grain(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            //函数职责：组合周期沉积纹和砂砾明暗，保持文字背景低对比度。
            fixed4 frag(v2f_img input) : SV_Target
            {
                float2 uv = input.uv;
                float sediment = sin(uv.y * 50.26548 + sin(uv.x * 12.56637) * 1.7);
                float veins = pow(saturate(sediment), 12.0) * 0.32;
                float sand = grain(floor(uv * 256.0));
                float pores = step(0.987, sand) * 0.18;
                float3 color = lerp(_StoneColor.rgb, _VeinColor.rgb, veins + sand * 0.18);
                color *= 0.94 + sand * 0.12 - pores;
                return float4(color, 1.0);
            }
            ENDCG
        }
    }
}
