Shader "NingshaRace/Zhenni/Bladebearer/Particles"
{
    Properties
    {
        _Color ("颜色与透明度", Color) = (0.7,0.4,1,1)
        _Additive ("光束叠加比例", Range(0,1)) = 0
        _BloomIntensity ("辉光强度", Range(0,2)) = 0.8
        _BloomThreshold ("亮部阈值", Range(0,1)) = 0.5
        [HideInInspector] _BloomCapture ("辉光捕获", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest LEqual
        Blend One OneMinusSrcAlpha
        CGINCLUDE
        #include "UnityCG.cginc"
        half4 _Color;
        float _Additive, _BloomIntensity, _BloomThreshold, _BloomCapture;
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

        //单 Pass 分别提交颜色与辉光捕获，避免地图自动执行额外 Pass 把火星重复加亮。
        half4 frag(v2f i) : SV_Target
        {
            half4 c = spark(i);
            if (_BloomCapture < 0.5) return half4(c.rgb * c.a, c.a * (1 - _Additive));
            float brightness = max(c.r, max(c.g, c.b));
            float knee = max(_BloomThreshold * 0.5, 0.0001);
            float soft = clamp(brightness - _BloomThreshold + knee, 0, 2 * knee);
            soft = soft * soft / (4 * knee);
            float contribution = max(brightness - _BloomThreshold, soft) / max(brightness, 0.0001);
            return half4(c.rgb * c.a * contribution * _BloomIntensity, c.a * (1 - _Additive));
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
