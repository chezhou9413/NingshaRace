Shader "NingshaRace/Combat/MeleeFlameTrail"
{
    Properties
    {
        _Color ("颜色与透明度", Color) = (0.7,0.4,1,1)
        _Additive ("光束叠加比例", Range(0,1)) = 0.35
        _DisplacementTex ("刀光 RG 置换图", 2D) = "gray" {}
        _SlashTime ("刀光时间", Float) = 0
        _SlashDistortion ("刀光扰动", Range(0,2)) = 0.65
        _Glow ("柔光强度", Range(0,2)) = 0.7
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest LEqual
        Blend One OneMinusSrcAlpha
        CGINCLUDE
        #include "UnityCG.cginc"
        half4 _Color;
        float _Additive, _Glow;
        sampler2D _DisplacementTex;
        float _SlashTime, _SlashDistortion;
        struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
        struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

        //直接使用每颗粒子的实际顶点，不绘制任何静态粒子底图。
        v2f vert(appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv; o.color = v.color;
            return o;
        }

        //细碎火屑有亮芯和弯曲尖尾，不再使用圆形光球轮廓。
        half4 spark(v2f i)
        {
            float along = saturate(i.uv.y);
            float bend = sin(along * 4.5) * (1 - along) * 0.18;
            float across = (i.uv.x - 0.5) * 2 - bend;
            float width = max(pow(saturate(sin(along * 3.14159265)), 0.7) * lerp(0.25, 1, along), 0.001);
            float edge = abs(across) / width;
            float aa = max(fwidth(edge), 0.12);
            float silhouette = 1 - smoothstep(1 - aa, 1 + aa, edge);
            float ends = smoothstep(0, 0.12, along) * (1 - smoothstep(0.8, 1, along));
            half alpha = silhouette * ends * i.color.a * _Color.a;
            float core = exp2(-across * across * 18 - (along - 0.65) * (along - 0.65) * 28);
            half3 color = lerp(_Color.rgb, half3(1,0.9,1), core * 0.65) * i.color.rgb;
            return half4(color, alpha);
        }

        //沿刃段轨迹的 RG 置换同时弯曲颜色与轮廓，不用噪声擦除贴图。
        half4 slash(v2f i)
        {
            float2 uv = float2(i.uv.x - 2, i.uv.y);
            float2 flow = float2(uv.x * 1.8, uv.y * 3.2 - _SlashTime * 2.8);
            float2 noise = tex2D(_DisplacementTex, flow).rg * 2 - 1;
            float2 detail = tex2D(_DisplacementTex, flow * 2.7 + float2(_SlashTime, 0)).rg * 2 - 1;
            uv += (noise * 0.10 + detail * 0.035) * _SlashDistortion;
            float inner = smoothstep(0.03, 0.36, uv.x);
            float outer = 1 - smoothstep(0.85, 1, uv.x);
            float core = exp2(-pow((uv.x - 0.78) * 10, 2));
            half3 color = lerp(_Color.rgb * 0.65, half3(1,0.86,1), core * 0.8) * i.color.rgb;
            return half4(color, inner * outer * _Color.a * i.color.a);
        }

        //刀光保留深度测试，用柔软轮廓和适量加色产生局部辉光。
        half4 frag(v2f i) : SV_Target
        {
            half4 c = i.uv.x > 1.5 ? slash(i) : spark(i);
            return half4(c.rgb * c.a * (1 + _Glow * 0.35), c.a * (1 - _Additive));
        }
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }
    Fallback Off
}
