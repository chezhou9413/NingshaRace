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


#line 56 0
struct RU_MaterialData_0
{
    float _Additive; // @default = 0.35
    vec4 _Color; // @default = vec4(0.7, 0.4, 1, 1) @color
    float _Glow; // @default = 0.7
    float _SlashDistortion; // @default = 0.65
    float _SlashTime; // @default = 0
};


#line 63
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _Additive; // @default = 0.35
    vec4 _Color; // @default = vec4(0.7, 0.4, 1, 1) @color
    float _Glow; // @default = 0.7
    float _SlashDistortion; // @default = 0.65
    float _SlashTime; // @default = 0
    float _RimUnitySrgb_DisplacementTex; // @default = 1
}RU_Material_0;

#line 70
layout(binding = 1, set = 2)
uniform sampler2D _DisplacementTex; // @default = gray


#line 72



#line 14946 1
float saturate_0(float x_0)
{

#line 14954
    return clamp(x_0, 0.0, 1.0);
}


#line 74 0
vec4 RU_tex2D_DisplacementTex_0(vec2 uv_0)
{

#line 74
    return (RU_SampleLinear(texture(_DisplacementTex, (vec2(uv_0.x, 1.0 - uv_0.y))), RU_Material_0._RimUnitySrgb_DisplacementTex));
}


#line 84
struct v2f_0
{
    vec4 vertex_0;
    vec2 uv_1;
    vec4 color_0;
};


#line 84
vec4 slash_0(v2f_0 i_0)
{

#line 84
    float _S1 = i_0.uv_1.x - 2.0;

#line 84
    float _S2 = i_0.uv_1.y;

#line 84
    vec2 flow_0 = vec2(_S1 * 1.79999995231628418, _S2 * 3.20000004768371582 - RU_Material_0._SlashTime * 2.79999995231628418);

#line 84
    float _S3 = (vec2(_S1, _S2) + ((RU_tex2D_DisplacementTex_0(flow_0).xy * 2.0 - 1.0) * 0.10000000149011612 + (RU_tex2D_DisplacementTex_0(flow_0 * 2.70000004768371582 + vec2(RU_Material_0._SlashTime, 0.0)).xy * 2.0 - 1.0) * 0.03500000014901161) * RU_Material_0._SlashDistortion).x;

#line 84
    return vec4(mix(RU_Linear(RU_Material_0._Color).xyz * 0.64999997615814209, vec3(1.0, 0.86000001430511475, 1.0), vec3((exp2((- pow((_S3 - 0.77999997138977051) * 10.0, 2.0)))) * 0.80000001192092896)) * i_0.color_0.xyz, smoothstep(0.02999999932944775, 0.36000001430511475, _S3) * (1.0 - smoothstep(0.85000002384185791, 1.0, _S3)) * RU_Linear(RU_Material_0._Color).w * i_0.color_0.w);
}


#line 84
vec4 spark_0(v2f_0 i_1)
{

#line 84
    float along_0 = saturate_0(i_1.uv_1.y);

#line 84
    float across_0 = (i_1.uv_1.x - 0.5) * 2.0 - sin(along_0 * 4.5) * (1.0 - along_0) * 0.18000000715255737;

#line 84
    float edge_0 = abs(across_0) / max(pow(saturate_0(sin(along_0 * 3.14159274101257324)), 0.69999998807907104) * mix(0.25, 1.0, along_0), 0.00100000004749745);

#line 84
    float _S4 = max((fwidth((edge_0))), 0.11999999731779099);

#line 84
    float _S5 = along_0 - 0.64999997615814209;

#line 84
    return vec4(mix(RU_Linear(RU_Material_0._Color).xyz, vec3(1.0, 0.89999997615814209, 1.0), vec3((exp2((- across_0 * across_0 * 18.0 - _S5 * _S5 * 28.0))) * 0.64999997615814209)) * i_1.color_0.xyz, (1.0 - smoothstep(1.0 - _S4, 1.0 + _S4, edge_0)) * (smoothstep(0.0, 0.11999999731779099, along_0) * (1.0 - smoothstep(0.80000001192092896, 1.0, along_0))) * i_1.color_0.w * RU_Linear(RU_Material_0._Color).w);
}


#line 84
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 84
layout(location = 1) in vec2 i_uv_0; // @vertex TexCoord0 optional


#line 84
layout(location = 2) in vec4 i_color_0; // @vertex Color optional


#line 84
void RU_SourceMain()
{

#line 84
    vec4 c_0;

#line 84
    if((i_uv_0.x) > 1.5)
    {

#line 84
        v2f_0 _S6 = v2f_0(gl_FragCoord, i_uv_0, i_color_0);

#line 84
        c_0 = slash_0(_S6);

#line 84
    }
    else
    {

#line 84
        v2f_0 _S7 = v2f_0(gl_FragCoord, i_uv_0, i_color_0);

#line 84
        c_0 = spark_0(_S7);

#line 84
    }

#line 84
    float _S8 = c_0.w;

#line 84
    entryPointParam_frag_0 = vec4(c_0.xyz * _S8 * (1.0 + RU_Material_0._Glow * 0.34999999403953552), _S8 * (1.0 - RU_Material_0._Additive));

#line 84
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
