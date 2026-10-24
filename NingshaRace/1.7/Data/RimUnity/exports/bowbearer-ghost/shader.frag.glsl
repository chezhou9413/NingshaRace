#version 450
vec3 RU_Decode(vec3 c) {
    return mix(pow(max((c + 0.055) / 1.055, vec3(0.0)), vec3(2.4)), c / 12.92, lessThanEqual(c, vec3(0.04045)));
}
vec3 RU_Encode(vec3 c) {
    return mix(1.055 * pow(max(c, vec3(0.0)), vec3(1.0 / 2.4)) - 0.055, c * 12.92, lessThanEqual(c, vec3(0.0031308)));
}
vec4 RU_Linear(vec4 c) { return vec4(RU_Decode(c.rgb), c.a); }
vec3 RU_Linear(vec3 c) { return RU_Decode(c); }
vec2 RU_Linear(vec2 c) { return RU_Decode(vec3(c, 0.0)).xy; }
float RU_Linear(float c) { return RU_Decode(vec3(c)).x; }
vec4 RU_SampleLinear(vec4 c, float srgb) { return srgb > 0.5 ? RU_Linear(c) : c; }
#extension GL_EXT_control_flow_attributes : require
float RU_CompatPow2(float x) {
    float result = float(1.0);
    x *= x;
    result *= x;
    return result;
}
float RU_CompatPow4(float x) {
    float result = float(1.0);
    x *= x;
    x *= x;
    result *= x;
    return result;
}
float RU_CompatSmoothstep(float low, float high, float x) {
    if (low == high) return step(low, x);
    float t = clamp((x - low) / (high - low), 0.0, 1.0);
    return t * t * (3.0 - 2.0 * t);
}
vec2 RU_CompatPow2(vec2 x) {
    vec2 result = vec2(1.0);
    x *= x;
    result *= x;
    return result;
}
vec2 RU_CompatPow4(vec2 x) {
    vec2 result = vec2(1.0);
    x *= x;
    x *= x;
    result *= x;
    return result;
}
vec2 RU_CompatSmoothstep(vec2 low, vec2 high, vec2 x) {
    return vec2(RU_CompatSmoothstep(low.x, high.x, x.x), RU_CompatSmoothstep(low.y, high.y, x.y));
}
vec2 RU_CompatSmoothstep(float low, float high, vec2 x) { return RU_CompatSmoothstep(vec2(low), vec2(high), x); }
vec3 RU_CompatPow2(vec3 x) {
    vec3 result = vec3(1.0);
    x *= x;
    result *= x;
    return result;
}
vec3 RU_CompatPow4(vec3 x) {
    vec3 result = vec3(1.0);
    x *= x;
    x *= x;
    result *= x;
    return result;
}
vec3 RU_CompatSmoothstep(vec3 low, vec3 high, vec3 x) {
    return vec3(RU_CompatSmoothstep(low.x, high.x, x.x), RU_CompatSmoothstep(low.y, high.y, x.y), RU_CompatSmoothstep(low.z, high.z, x.z));
}
vec3 RU_CompatSmoothstep(float low, float high, vec3 x) { return RU_CompatSmoothstep(vec3(low), vec3(high), x); }
vec4 RU_CompatPow2(vec4 x) {
    vec4 result = vec4(1.0);
    x *= x;
    result *= x;
    return result;
}
vec4 RU_CompatPow4(vec4 x) {
    vec4 result = vec4(1.0);
    x *= x;
    x *= x;
    result *= x;
    return result;
}
vec4 RU_CompatSmoothstep(vec4 low, vec4 high, vec4 x) {
    return vec4(RU_CompatSmoothstep(low.x, high.x, x.x), RU_CompatSmoothstep(low.y, high.y, x.y), RU_CompatSmoothstep(low.z, high.z, x.z), RU_CompatSmoothstep(low.w, high.w, x.w));
}
vec4 RU_CompatSmoothstep(float low, float high, vec4 x) { return RU_CompatSmoothstep(vec4(low), vec4(high), x); }


#line 56 0
struct RU_MaterialData_0
{
    vec4 _Color; // @default = vec4(0.7, 0.38, 1, 1) @color
    float _Distortion; // @default = 0.65
    float _EffectTime; // @default = 0
    float _Glow; // @default = 1.2
    float _Intensity; // @default = 1
    float _Mode; // @default = 0
    float _Progress; // @default = 0
    float _Seed; // @default = 0
};


#line 66
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    vec4 _Color; // @default = vec4(0.7, 0.38, 1, 1) @color
    float _Distortion; // @default = 0.65
    float _EffectTime; // @default = 0
    float _Glow; // @default = 1.2
    float _Intensity; // @default = 1
    float _Mode; // @default = 0
    float _Progress; // @default = 0
    float _Seed; // @default = 0
    float _RimUnitySrgb_DisplacementTex; // @default = 1
}RU_Material_0;

#line 76
layout(binding = 1, set = 2)
uniform sampler2D _DisplacementTex; // @default = gray


#line 78



#line 14946 1
float saturate_0(float x_0)
{

#line 14954
    return clamp(x_0, 0.0, 1.0);
}


#line 80 0
vec4 RU_tex2D_DisplacementTex_0(vec2 uv_0)
{

#line 80
    return (RU_SampleLinear(texture(_DisplacementTex, (vec2(uv_0.x, 1.0 - uv_0.y))), RU_Material_0._RimUnitySrgb_DisplacementTex));
}


#line 94
struct v2f_0
{
    vec4 vertex_0;
    vec2 uv_1;
    vec4 color_0;
};


#line 94
vec4 trail_0(v2f_0 i_0)
{

#line 94
    float across_0 = i_0.uv_1.x * 2.0 - 1.0;

#line 94
    vec2 flow_0 = vec2(i_0.uv_1.y * 1.35000002384185791 - RU_Material_0._EffectTime * 1.70000004768371582, across_0 * 0.69999998807907104 + RU_Material_0._Seed);

#line 94
    vec2 noise_0 = RU_tex2D_DisplacementTex_0(flow_0).xy;

#line 94
    float x_1 = across_0 - (noise_0.x - 0.5) * 0.12999999523162842 * RU_Material_0._Distortion;

#line 94
    float _S1 = - x_1 * x_1;

#line 94
    float core_0 = (exp2((_S1 * 105.0)));

#line 94
    float veil_0 = (exp2((_S1 * (9.0 + RU_tex2D_DisplacementTex_0(flow_0 * 2.29999995231628418 + vec2(0.0, RU_Material_0._EffectTime * 0.69999998807907104)).x * 8.0))));

#line 94
    float flowLight_0 = 0.69999998807907104 + 0.30000001192092896 * noise_0.y;

#line 94
    float fade_0 = i_0.color_0.w * RU_Linear(RU_Material_0._Color).w * (1.0 - RU_CompatSmoothstep(0.64999997615814209, 1.0, abs(across_0)));

#line 94
    return vec4((mix(RU_Linear(RU_Material_0._Color).xyz, vec3(1.0, 0.86000001430511475, 1.0), vec3(0.69999998807907104)) * core_0 * (2.79999995231628418 + RU_Material_0._Glow) * flowLight_0 + RU_Linear(RU_Material_0._Color).xyz * (veil_0 * 0.64999997615814209 * flowLight_0 + (exp2((- across_0 * across_0 * 4.5))) * RU_Material_0._Glow * 0.23999999463558197)) * RU_Material_0._Intensity * i_0.color_0.xyz * fade_0, (veil_0 * 0.18000000715255737 + core_0 * 0.11999999731779099) * fade_0);
}


#line 94
vec4 burst_0(v2f_0 i_1)
{

#line 94
    vec2 p_0 = i_1.uv_1 * 2.0 - 1.0;

#line 94
    float r_0 = length(p_0);

#line 94
    float t_0 = saturate_0(RU_Material_0._Progress);

#line 94
    vec2 flow_1 = p_0 * 1.60000002384185791 + vec2(RU_Material_0._Seed, - RU_Material_0._EffectTime * 0.44999998807907104);

#line 94
    vec2 noise_1 = RU_tex2D_DisplacementTex_0(flow_1).xy;

#line 94
    float detail_0 = RU_tex2D_DisplacementTex_0(flow_1 * 2.70000004768371582 + vec2(RU_Material_0._EffectTime * 0.30000001192092896, 0.0)).x;

#line 94
    float _S2 = max(0.0, r_0 + ((noise_1.x - 0.5) * 0.18000000715255737 + (detail_0 - 0.5) * 0.05999999865889549) * RU_Material_0._Distortion);

#line 94
    float _S3 = r_0 * r_0;

#line 94
    float shellDistance_0 = (_S2 - (0.18000000715255737 + 0.52999997138977051 * sqrt(t_0))) / (0.12999999523162842 + t_0 * 0.07999999821186066);

#line 94
    float shell_0 = (exp2((- shellDistance_0 * shellDistance_0))) * (0.34999999403953552 + 0.64999997615814209 * noise_1.y);

#line 94
    float body_0 = (exp2((- _S2 * _S2 / (0.02500000037252903 + t_0 * 0.25)))) * (0.44999998807907104 + 0.55000001192092896 * detail_0) * sqrt(saturate_0(1.0 - _S3));

#line 94
    float fade_1 = (1.0 - RU_CompatSmoothstep(0.2199999988079071, 1.0, t_0)) * (1.0 - RU_CompatSmoothstep(0.72000002861022949, 1.0, r_0)) * RU_Linear(RU_Material_0._Color).w * i_1.color_0.w;

#line 94
    return vec4((mix(RU_Linear(RU_Material_0._Color).xyz, vec3(1.0, 0.87999999523162842, 1.0), vec3(0.75)) * ((exp2((- t_0 * 24.0 - _S3 * 32.0))) * 4.5 + body_0 * (1.29999995231628418 + RU_Material_0._Glow)) + RU_Linear(RU_Material_0._Color).xyz * (shell_0 * (1.0 + RU_Material_0._Glow * 0.60000002384185791) + (exp2((- r_0 * r_0 * 4.0))) * (1.0 - t_0) * RU_Material_0._Glow * 0.30000001192092896)) * RU_Material_0._Intensity * fade_1 * i_1.color_0.xyz, (body_0 * 0.2199999988079071 + shell_0 * 0.11999999731779099) * fade_1);
}


#line 94
void lobe_0(vec3 point_0, vec3 center_0, float radius_0, inout float density_0, inout vec3 normal_0)
{

#line 94
    vec3 delta_0 = (point_0 - center_0) / radius_0;

#line 94
    float weight_0 = (exp2((- dot(delta_0, delta_0) * 3.0)));

#line 94
    density_0 = density_0 + weight_0;

#line 94
    normal_0 = normal_0 + weight_0 * delta_0 / radius_0;

#line 94
    return;
}


#line 94
vec4 empoweredBurst_0(v2f_0 i_2)
{

#line 94
    vec2 p_1 = i_2.uv_1 * 2.0 - 1.0;

#line 94
    float t_1 = saturate_0(RU_Material_0._Progress);

#line 94
    float _S4 = - t_1;

#line 94
    float _S5 = 0.15000000596046448 + 0.73000001907348633 * (1.0 - (exp2((_S4 * 7.0))));

#line 94
    float fade_2 = (1.0 - RU_CompatSmoothstep(0.34999999403953552, 1.0, t_1)) * RU_Linear(RU_Material_0._Color).w * i_2.color_0.w;

#line 94
    vec3 _S6 = vec3(ivec3(0));

#line 94
    int step_0 = 0;

#line 94
    float transmission_0 = 1.0;

#line 94
    vec3 sum_0 = _S6;

#line 94
    [[unroll]]
    for(;;)
    {

#line 94
        if(step_0 < 14)
        {
        }
        else
        {

#line 94
            break;
        }

#line 94
        vec3 point_1 = vec3(p_1.x, p_1.y - t_1 * 0.11999999731779099, 0.94999998807907104 - float(step_0) * 0.14499999582767487) / _S5;

#line 94
        vec2 flow_2 = point_1.xy * 1.29999995231628418 + point_1.z * vec2(0.61000001430511475, -0.43000000715255737) + vec2(RU_Material_0._Seed, - RU_Material_0._EffectTime * 0.47999998927116394);

#line 94
        float noise_2 = RU_tex2D_DisplacementTex_0(flow_2).x;

#line 94
        float detail_1 = RU_tex2D_DisplacementTex_0(flow_2 * 2.79999995231628418 + RU_Material_0._EffectTime * 0.11999999731779099).y;

#line 94
        vec3 point_2 = point_1 + (vec3(noise_2, detail_1, noise_2 - detail_1) - vec3(0.5, 0.5, 0.0)) * 0.18000000715255737 * RU_Material_0._Distortion;

#line 94
        float density_1 = 0.0;

#line 94
        vec3 normal_1 = _S6;

#line 94
        lobe_0(point_2, vec3(0.0, -0.11999999731779099, 0.0), 0.57999998331069946, density_1, normal_1);

#line 94
        lobe_0(point_2, vec3(-0.34999999403953552, 0.10000000149011612, 0.07999999821186066), 0.43000000715255737, density_1, normal_1);

#line 94
        lobe_0(point_2, vec3(0.36000001430511475, 0.10000000149011612, -0.10000000149011612), 0.43999999761581421, density_1, normal_1);

#line 94
        lobe_0(point_2, vec3(-0.05999999865889549, 0.43999999761581421, -0.01999999955296516), 0.40000000596046448, density_1, normal_1);

#line 94
        lobe_0(point_2, vec3(0.18999999761581421, 0.31999999284744263, 0.25), 0.31999999284744263, density_1, normal_1);

#line 94
        float _S7 = max(0.0, density_1 - 0.10000000149011612) * (0.44999998807907104 + 0.80000001192092896 * noise_2);

#line 94
        density_1 = _S7;

#line 94
        float alpha_0 = (1.0 - (exp2((- _S7 * 0.85000002384185791)))) * fade_2;

#line 94
        float light_0 = 0.2199999988079071 + 0.77999997138977051 * saturate_0(dot(normalize(normal_1 + 0.00000999999974738), normalize(vec3(-0.5, 0.64999997615814209, 0.80000001192092896))));

#line 94
        vec3 sum_1 = sum_0 + transmission_0 * ((RU_Linear(RU_Material_0._Color).xyz * (0.11999999731779099 + 0.55000001192092896 * light_0) + vec3(0.25, 0.55000001192092896, 0.85000002384185791) * (RU_CompatPow2(saturate_0(1.0 - _S7)) * (0.34999999403953552 + 0.64999997615814209 * detail_1)) * light_0 * (0.75 + RU_Material_0._Glow) + mix(RU_Linear(RU_Material_0._Color).xyz, vec3(0.44999998807907104, 0.80000001192092896, 1.0), vec3(0.55000001192092896)) * (RU_CompatPow4(light_0) * sqrt(saturate_0(_S7)) * (0.69999998807907104 + 0.30000001192092896 * noise_2)) * 0.44999998807907104) * mix(0.5, 1.0, saturate_0(point_2.y * 0.60000002384185791 + 0.64999997615814209))) * alpha_0;

#line 94
        float transmission_1 = transmission_0 * (1.0 - alpha_0);

#line 94
        step_0 = step_0 + 1;

#line 94
        transmission_0 = transmission_1;

#line 94
        sum_0 = sum_1;

#line 94
    }

#line 94
    float _S8 = p_1.y;

#line 94
    float ring_0 = (exp2((- RU_CompatPow2((length(vec2(p_1.x, (_S8 + t_1 * 0.18000000715255737) * 1.64999997615814209)) - (0.11999999731779099 + 0.77999997138977051 * sqrt(t_1))) / 0.03500000014901161)))) * RU_CompatPow2(1.0 - t_1);

#line 94
    float flash_0 = (exp2((_S4 * 38.0 - dot(p_1, p_1) * 150.0)));

#line 94
    float edge_0 = 1.0 - RU_CompatSmoothstep(0.87999999523162842, 1.0, length(p_1));

#line 94
    float ringDepth_0;

#line 94
    if(_S8 > (_S4 * 0.18000000715255737))
    {

#line 94
        ringDepth_0 = transmission_0;

#line 94
    }
    else
    {

#line 94
        ringDepth_0 = 1.0;

#line 94
    }

#line 94
    return vec4((sum_0 + (RU_Linear(RU_Material_0._Color).xyz * ring_0 * 0.30000001192092896 * ringDepth_0 + vec3(0.69999998807907104, 0.85000002384185791, 1.0) * flash_0 * 1.5) * fade_2) * RU_Material_0._Intensity * edge_0 * i_2.color_0.xyz, (1.0 - transmission_0) * edge_0);
}


#line 94
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 94
layout(location = 1) in vec2 i_uv_0; // @vertex TexCoord0 optional


#line 94
layout(location = 2) in vec4 i_color_0; // @vertex Color optional


#line 94
void RU_SourceMain()
{

#line 94
    vec4 _S9;

#line 94
    if((RU_Material_0._Mode) < 0.5)
    {

#line 94
        v2f_0 _S10 = v2f_0(gl_FragCoord, i_uv_0, i_color_0);

#line 94
        _S9 = trail_0(_S10);

#line 94
    }
    else
    {

#line 94
        if((RU_Material_0._Mode) < 1.5)
        {

#line 94
            v2f_0 _S11 = v2f_0(gl_FragCoord, i_uv_0, i_color_0);

#line 94
            _S9 = burst_0(_S11);

#line 94
        }
        else
        {

#line 94
            v2f_0 _S12 = v2f_0(gl_FragCoord, i_uv_0, i_color_0);

#line 94
            _S9 = empoweredBurst_0(_S12);

#line 94
        }

#line 94
    }

#line 94
    entryPointParam_frag_0 = _S9;

#line 94
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
