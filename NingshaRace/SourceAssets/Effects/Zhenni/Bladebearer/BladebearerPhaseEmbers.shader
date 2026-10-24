Shader "NingshaRace/Zhenni/Bladebearer/PhaseEmbers"
{
    Properties
    {
        _MainTex ("角色轮廓", 2D) = "white" {}
        _DisplacementTex ("紫焰置换 RG", 2D) = "gray" {}
        _Color ("原画颜色", Color) = (1,1,1,1)
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
            #pragma vertex emberVert
            #pragma fragment emberFrag
            #include "BladebearerFlameCommon.cginc"
            float _PhaseAge, _PhaseDuration, _PhaseLifetime, _PhaseAssemble, _PhaseInitial;
            struct emberInput { float4 vertex : POSITION; float2 uv : TEXCOORD0; float2 corner : TEXCOORD1; };
            struct emberOutput { float4 vertex : SV_POSITION; float2 source : TEXCOORD0; float2 uv : TEXCOORD1; float2 life : TEXCOORD2; };

            //细小火屑沿噪声路径旋动，聚合时反向回到原图的确切位置。
            emberOutput emberVert(emberInput v)
            {
                emberOutput o;
                float field = phaseField(v.uv);
                float2 seed = tex2Dlod(_DisplacementTex, float4(v.uv * 11.9 + 0.73, 0, 0)).rg;
                float age = _PhaseAssemble > 0.5 ? (1 - field) * _PhaseDuration - _PhaseAge
                    : _PhaseAge - saturate((field - _PhaseInitial) / max(1 - _PhaseInitial, 0.001)) * _PhaseDuration;
                float lifetime = _PhaseAssemble > 0.5 ? _PhaseDuration : _PhaseLifetime * lerp(0.65, 1, seed.x);
                float travel = max(age, 0);
                float2 noise = tex2Dlod(_DisplacementTex, float4(v.uv * 3.1 + float2(travel * 0.29, -travel * 0.43), 0, 0)).rg;
                float2 initialNoise = tex2Dlod(_DisplacementTex, float4(v.uv * 3.1, 0, 0)).rg;
                float2 direction = normalize(float2(seed.x - 0.5, 0.18 + seed.y));
                float2 drift = direction * travel * 0.3 + (noise - initialNoise) * 0.35;
                if (_PhaseAssemble > 0.5) drift += (v.uv - 0.5) * travel * 1.1;
                float2 side = float2(-direction.y, direction.x);
                float size = lerp(0.0028, 0.0068, seed.x * seed.x);
                float shrink = _PhaseAssemble > 0.5 ? 1 : lerp(1, 0.3, saturate(age / lifetime));
                float2 corner = (v.corner - 0.5) * 2;
                float2 fragmentPosition = v.vertex.xz + drift + (side * corner.x + direction * corner.y * lerp(2.1, 4, seed.y)) * size * shrink;
                o.vertex = UnityObjectToClipPos(float4(fragmentPosition.x, v.vertex.y, fragmentPosition.y, 1));
                o.source = v.uv; o.uv = v.corner;
                float fade = smoothstep(0, 0.055, age) * (1 - smoothstep(lifetime * 0.4, lifetime, age));
                if (_PhaseAssemble > 0.5) fade = smoothstep(0, 0.04, _PhaseAge) * smoothstep(0, 0.05, age);
                if (_PhaseAssemble < 0.5) fade *= step(_PhaseInitial, field);
                o.life = float2(fade, seed.y);
                return o;
            }

            //透明空白处不产生粒子，细紫边与少量亮芯延续现有火星质感。
            half4 emberFrag(emberOutput i) : SV_Target
            {
                float2 sourceUV = displacedUV(i.source);
                clip(min(min(sourceUV.x, sourceUV.y), min(1 - sourceUV.x, 1 - sourceUV.y)));
                half4 source = tex2D(_MainTex, sourceUV) * _Color;
                clip(source.a - 0.12);
                clip(i.life.x - 0.001);
                float y = i.uv.y;
                float x = (i.uv.x - 0.5) * 2 - sin(y * 4.5) * (1 - y) * 0.15;
                float width = max(pow(saturate(sin(y * 3.14159265)), 0.7) * lerp(0.3, 1, y), 0.001);
                float edge = abs(x) / width;
                float shape = 1 - smoothstep(0.65, 1.15, edge);
                float core = exp2(-x * x * 22 - (y - 0.65) * (y - 0.65) * 25);
                float halo = exp2(-edge * edge * 2.7) * 0.15;
                float ends = smoothstep(0, 0.12, y) * (1 - smoothstep(0.85, 1, y));
                half alpha = source.a * i.life.x * ends * saturate(shape + halo);
                half3 color = lerp(half3(0.56,0.09,1), half3(1,0.82,1), core * 0.8);
                color *= lerp(0.8, 1.65, step(0.72, i.life.y));
                return half4(color * alpha, alpha * 0.72);
            }
            ENDCG
        }
    }
    Fallback Off
}
