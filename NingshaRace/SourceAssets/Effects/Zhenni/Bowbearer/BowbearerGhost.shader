Shader "NingshaRace/Zhenni/Bowbearer/Ghost"
{
    Properties
    {
        _Color ("紫焰颜色与亮度", Color) = (0.7,0.38,1,1)
        _Intensity ("发光强度", Float) = 1
        _DisplacementTex ("流动噪声", 2D) = "gray" {}
        _EffectTime ("游戏时间", Float) = 0
        _Distortion ("扰动", Float) = 0.65
        _Glow ("辉光", Float) = 1.2
        _Mode ("螺旋或爆焰", Float) = 0
        _Progress ("爆焰进度", Float) = 0
        _Seed ("相位", Float) = 0
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
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            sampler2D _DisplacementTex;
            float _Intensity, _EffectTime, _Distortion, _Glow, _Mode, _Progress, _Seed;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            //沿用地图和预览提交的实际网格与颜色。
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; o.color = v.color;
                return o;
            }

            //软光包围连续亮芯，流动噪声只扰动形态，不把整股螺旋切断。
            float4 trail(v2f i)
            {
                float across = i.uv.x * 2 - 1;
                float2 flow = float2(i.uv.y * 1.35 - _EffectTime * 1.7, across * 0.7 + _Seed);
                float2 noise = tex2D(_DisplacementTex, flow).rg;
                float detail = tex2D(_DisplacementTex, flow * 2.3 + float2(0, _EffectTime * .7)).r;
                float center = (noise.x - .5) * .13 * _Distortion;
                float x = across - center;
                float core = exp2(-x * x * 105);
                float veil = exp2(-x * x * (9 + detail * 8));
                float halo = exp2(-across * across * 4.5);
                float edge = 1 - smoothstep(.65, 1, abs(across));
                float flowLight = .7 + .3 * noise.y;
                float fade = i.color.a * _Color.a * edge;
                float3 hot = lerp(_Color.rgb, float3(1, .86, 1), .7);
                float3 energy = hot * core * (2.8 + _Glow) * flowLight
                    + _Color.rgb * (veil * .65 * flowLight + halo * _Glow * .24);
                return float4(energy * _Intensity * i.color.rgb * fade, (veil * .18 + core * .12) * fade);
            }

            //圆润的亮芯、翻卷焰团和散开的薄雾共用连续软边，避免箭头拼片轮廓。
            float4 burst(v2f i)
            {
                float2 p = i.uv * 2 - 1;
                float r = length(p);
                float t = saturate(_Progress);
                float2 flow = p * 1.6 + float2(_Seed, -_EffectTime * .45);
                float2 noise = tex2D(_DisplacementTex, flow).rg;
                float detail = tex2D(_DisplacementTex, flow * 2.7 + float2(_EffectTime * .3, 0)).r;
                float warped = max(0, r + ((noise.x - .5) * .18 + (detail - .5) * .06) * _Distortion);
                float sphere = sqrt(saturate(1 - r * r));
                float shellRadius = .18 + .53 * sqrt(t);
                float shellDistance = (warped - shellRadius) / (.13 + t * .08);
                float shell = exp2(-shellDistance * shellDistance) * (.35 + .65 * noise.y);
                float body = exp2(-warped * warped / (.025 + t * .25)) * (.45 + .55 * detail) * sphere;
                float flash = exp2(-t * 24 - r * r * 32);
                float halo = exp2(-r * r * 4) * (1 - t);
                float edge = 1 - smoothstep(.72, 1, r);
                float fade = (1 - smoothstep(.22, 1, t)) * edge * _Color.a * i.color.a;
                float3 hot = lerp(_Color.rgb, float3(1, .88, 1), .75);
                float3 energy = hot * (flash * 4.5 + body * (1.3 + _Glow))
                    + _Color.rgb * (shell * (1 + _Glow * .6) + halo * _Glow * .3);
                return float4(energy * _Intensity * fade * i.color.rgb, (body * .22 + shell * .12) * fade);
            }

            //连续密度团同时累积法线方向，让爆焰各瓣具有一致的明暗体积。
            void lobe(float3 point, float3 center, float radius, inout float density, inout float3 normal)
            {
                float3 delta = (point - center) / radius;
                float weight = exp2(-dot(delta, delta) * 3);
                density += weight;
                normal += weight * delta / radius;
            }

            //沿视线积分五瓣翻卷焰团，前层吸收后层；薄冲击波与短亮芯分开衰减。
            float4 empoweredBurst(v2f i)
            {
                float2 p = i.uv * 2 - 1;
                float t = saturate(_Progress);
                float grow = .15 + .73 * (1 - exp2(-t * 7));
                float fade = (1 - smoothstep(.35, 1, t)) * _Color.a * i.color.a;
                float3 sum = 0;
                float transmission = 1;
                [unroll] for (int step = 0; step < 14; step++)
                {
                    float3 point = float3(p.x, p.y - t * .12, .95 - step * .145) / grow;
                    float2 flow = point.xy * 1.3 + point.z * float2(.61, -.43) + float2(_Seed, -_EffectTime * .48);
                    float noise = tex2D(_DisplacementTex, flow).r;
                    float detail = tex2D(_DisplacementTex, flow * 2.8 + _EffectTime * .12).g;
                    point += (float3(noise, detail, noise - detail) - float3(.5, .5, 0)) * .18 * _Distortion;
                    float density = 0; float3 normal = 0;
                    lobe(point, float3(0, -.12, 0), .58, density, normal);
                    lobe(point, float3(-.35, .10, .08), .43, density, normal);
                    lobe(point, float3(.36, .10, -.10), .44, density, normal);
                    lobe(point, float3(-.06, .44, -.02), .4, density, normal);
                    lobe(point, float3(.19, .32, .25), .32, density, normal);
                    density = max(0, density - .1) * (.45 + .8 * noise);
                    float alpha = (1 - exp2(-density * .85)) * fade;
                    float light = .22 + .78 * saturate(dot(normalize(normal + .00001), normalize(float3(-.5, .65, .8))));
                    float rim = pow(saturate(1 - density), 2) * (.35 + .65 * detail);
                    float veins = pow(light, 4) * sqrt(saturate(density)) * (.7 + .3 * noise);
                    float3 shade = _Color.rgb * (.12 + .55 * light)
                        + float3(.25, .55, .85) * rim * light * (.75 + _Glow)
                        + lerp(_Color.rgb, float3(.45, .8, 1), .55) * veins * .45;
                    shade *= lerp(.5, 1, saturate(point.y * .6 + .65));
                    sum += transmission * shade * alpha;
                    transmission *= 1 - alpha;
                }
                float2 ground = float2(p.x, (p.y + t * .18) * 1.65);
                float radius = .12 + .78 * sqrt(t);
                float ring = exp2(-pow((length(ground) - radius) / .035, 2)) * pow(1 - t, 2);
                float flash = exp2(-t * 38 - dot(p, p) * 150);
                float edge = 1 - smoothstep(.88, 1, length(p));
                float ringDepth = p.y > -t * .18 ? transmission : 1;
                sum += (_Color.rgb * ring * .3 * ringDepth + float3(.7, .85, 1) * flash * 1.5) * fade;
                return float4(sum * _Intensity * edge * i.color.rgb, (1 - transmission) * edge);
            }

            //普通箭沿用原效果，强化箭单独进入体积积分分支。
            float4 frag(v2f i) : SV_Target
            { return _Mode < .5 ? trail(i) : _Mode < 1.5 ? burst(i) : empoweredBurst(i); }
            ENDCG
        }
    }
    Fallback Off
}
