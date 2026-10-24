Shader "NingshaRace/Pawn/WindSandify"
{
    Properties
    {
        [PerRendererData] _MainTex ("沙傀主贴图", 2D) = "white" {}
        _Color ("整体染色", Color) = (1, 1, 1, 1)
        _SandDark ("暗部沙色", Color) = (0.358, 0.340, 0.323, 1)
        _SandMid ("中部沙色", Color) = (0.679, 0.617, 0.548, 1)
        _SandLight ("亮部沙色", Color) = (0.660, 0.632, 0.564, 1)
        _ShadowInfluence ("保留原图明暗", Range(0, 1)) = 1
        _DetailPreserve ("原图细节保留", Range(0, 1)) = 1
        _ReliefStrength ("沙雕浮雕强度", Range(0, 1)) = 1
        _OriginalTint ("原图颜色残留", Range(0, 0.35)) = 0.35
        _Brightness ("整体亮度", Range(0, 3)) = 0.82
        _ReliefLightDirection ("浮雕光照方向", Vector) = (-0.35, 0.65, 0, 0)
        _SandProgress ("聚拢与消散进度", Range(0, 1)) = 0.5
        _GatherRadius ("聚拢距离", Range(0, 1.5)) = 0.991
        _ScatterRadius ("飘散距离", Range(0, 1.5)) = 1
        _ParticleScale ("碎砂数量", Range(32, 512)) = 140
        _ParticleSize ("碎砂大小", Range(0.05, 0.95)) = 0.62
        _ParticleJitter ("碎砂位置错落", Range(0, 1)) = 0.6
        _ParticleDensity ("碎砂密度", Range(0, 1)) = 0.72
        _SolidHold ("完整轮廓保持宽度", Range(0.001, 0.25)) = 0.035
        _CanvasPadding ("飘散画布缓冲", Range(0, 2)) = 0.95
        _GrainScale ("表面砂纹密度", Range(8, 256)) = 256
        _GrainContrast ("表面砂纹对比", Range(0.2, 6)) = 0.2
        _GrainAmount ("表面砂纹强度", Range(0, 1)) = 1
        _SparkleAmount ("亮砂闪点", Range(0, 1)) = 0.104
        _AlphaCutoff ("透明裁剪", Range(0, 1)) = 0.092
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color, _SandDark, _SandMid, _SandLight;
            float _ShadowInfluence, _DetailPreserve, _ReliefStrength, _OriginalTint, _Brightness;
            float4 _ReliefLightDirection;
            float _SandProgress, _GatherRadius, _ScatterRadius, _SolidHold, _CanvasPadding;
            float _ParticleScale, _ParticleSize, _ParticleJitter, _ParticleDensity;
            float _GrainScale, _GrainContrast, _GrainAmount, _SparkleAmount, _AlphaCutoff;

            //接收原有沙傀截图面片。
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            //保留扩展前的贴图坐标，使完整身体的尺寸不随画布扩大。
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 rawUv : TEXCOORD0;
                float2 meshUv : TEXCOORD1;
                fixed4 color : COLOR;
            };

            //扩展地图面片，给向左上飘散的碎砂留下空间。
            v2f vert(appdata v)
            {
                v2f o;
                float expansion = 1.0 + 2.0 * max(max(_GatherRadius, _ScatterRadius), _CanvasPadding);
                v.vertex.xz *= expansion;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.rawUv = (v.uv - 0.5) * expansion + 0.5;
                o.meshUv = v.uv;
                o.color = v.color;
                return o;
            }

            //固定噪声种子让同一颗碎砂沿连续轨迹运动。
            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);
                return frac(value.x * value.y);
            }

            //生成连续噪声，供砂纹和剥离边缘使用。
            float ValueNoise(float2 uv)
            {
                float2 cell = floor(uv);
                float2 local = frac(uv);
                float2 blend = local * local * (3.0 - 2.0 * local);
                return lerp(lerp(Hash21(cell), Hash21(cell + float2(1, 0)), blend.x),
                    lerp(Hash21(cell + float2(0, 1)), Hash21(cell + 1), blend.x), blend.y);
            }

            //提取原图的明暗。
            float GetLuminance(fixed3 color)
            {
                return dot(color, fixed3(0.299, 0.587, 0.114));
            }

            //只采样真实贴图范围，防止透明画布外复制边缘像素。
            fixed4 SamplePawn(float2 uv)
            {
                float bounds = step(0.0, uv.x) * step(uv.x, 1.0)
                    * step(0.0, uv.y) * step(uv.y, 1.0);
                fixed4 sample = tex2D(_MainTex, saturate(uv));
                sample.a *= bounds;
                return sample;
            }

            //保留衣褶和五官的局部刻痕。
            float GetSourceDetail(float2 uv, float luminance)
            {
                float2 texel = max(abs(_MainTex_TexelSize.xy), 0.00001);
                float left = GetLuminance(tex2D(_MainTex, uv - float2(texel.x, 0)).rgb);
                float right = GetLuminance(tex2D(_MainTex, uv + float2(texel.x, 0)).rgb);
                float down = GetLuminance(tex2D(_MainTex, uv - float2(0, texel.y)).rgb);
                float up = GetLuminance(tex2D(_MainTex, uv + float2(0, texel.y)).rgb);
                return (luminance - (left + right + down + up) * 0.25) * 2.0;
            }

            //沿用完整沙傀的浮雕光照。
            float GetReliefShade(float2 uv)
            {
                float2 texel = max(abs(_MainTex_TexelSize.xy), 0.00001);
                float left = GetLuminance(tex2D(_MainTex, uv - float2(texel.x, 0)).rgb);
                float right = GetLuminance(tex2D(_MainTex, uv + float2(texel.x, 0)).rgb);
                float down = GetLuminance(tex2D(_MainTex, uv - float2(0, texel.y)).rgb);
                float up = GetLuminance(tex2D(_MainTex, uv + float2(0, texel.y)).rgb);
                float3 normal = normalize(float3(-float2(right - left, up - down) * 3.0, 1));
                float relief = dot(normal, normalize(float3(_ReliefLightDirection.xy, 1))) * 0.5 + 0.5;
                return lerp(1.0, lerp(0.82, 1.12, relief), saturate(_ReliefStrength));
            }

            //用原图明暗映射已有的三段砂色。
            fixed3 GetSandColor(float luminance, float grain)
            {
                float shaded = lerp(0.42, luminance, saturate(_ShadowInfluence));
                float tone = saturate(shaded * 0.82 + grain * 0.18);
                fixed3 color = lerp(_SandDark.rgb, _SandMid.rgb, smoothstep(0.05, 0.68, tone));
                return lerp(color, _SandLight.rgb, smoothstep(0.58, 1.0, tone));
            }

            //保持稳定阶段原有的细砂表面。
            float GetGrain(float2 uv)
            {
                float grain = ValueNoise(floor(uv * _GrainScale) + float2(7.1, 19.3));
                return pow(saturate(grain), max(_GrainContrast, 0.001));
            }

            #include "SandGolemWind.cginc"

            //完整部分保持原位，只从移动的剥离边界放出碎砂。
            fixed4 frag(v2f i) : SV_Target
            {
                float progress = saturate(_SandProgress);
                float dissolve = saturate((abs(progress - 0.5) - _SolidHold) / (0.5 - _SolidHold));
                fixed4 source = SamplePawn(i.rawUv) * _Color * i.color;
                float bodyAlpha = source.a * GetRemainingBody(i.rawUv, dissolve);
                float grain = GetGrain(i.rawUv);
                float sourceInfluence = saturate(source.a * 2.0);
                float luminance = lerp(0.42, GetLuminance(source.rgb), sourceInfluence);
                fixed3 bodyColor = GetSandColor(luminance, grain);
                float detail = GetSourceDetail(saturate(i.rawUv), luminance) * sourceInfluence;
                bodyColor *= lerp(1.0, GetReliefShade(saturate(i.rawUv)), sourceInfluence);
                bodyColor *= lerp(1.0, lerp(0.78, 1.10, grain), _GrainAmount);
                bodyColor *= clamp(1.0 + detail * _DetailPreserve, 0.72, 1.08);
                bodyColor = lerp(bodyColor, bodyColor * source.rgb, saturate(_OriginalTint) * sourceInfluence);
                bodyColor = lerp(bodyColor, _SandLight.rgb,
                    saturate(smoothstep(0.9, 1.0, grain) * _SparkleAmount * 0.45));
                bodyColor *= _Brightness;
                bodyAlpha *= lerp(1.0, saturate(0.72 + grain * 0.45), _GrainAmount * 0.45);

                float4 grains = 0;
                if (dissolve > 0.0 && dissolve < 1.0)
                {
                    float radius = progress < 0.5 ? _GatherRadius : _ScatterRadius;
                    grains = GetWindGrains(i.rawUv, dissolve, radius, i.color);
                }
                float alpha = bodyAlpha + grains.a * (1.0 - bodyAlpha);
                float3 premultiplied = bodyColor * bodyAlpha + grains.rgb * (1.0 - bodyAlpha);
                float2 edge = min(i.meshUv, 1.0 - i.meshUv);
                float feather = smoothstep(0.0, 0.035, min(edge.x, edge.y));
                alpha *= feather * (1.0 - smoothstep(0.94, 1.0, dissolve));
                clip(alpha - _AlphaCutoff * 0.35);
                return fixed4(premultiplied / max(bodyAlpha + grains.a * (1.0 - bodyAlpha), 0.0001), alpha);
            }
            ENDCG
        }
    }
}
