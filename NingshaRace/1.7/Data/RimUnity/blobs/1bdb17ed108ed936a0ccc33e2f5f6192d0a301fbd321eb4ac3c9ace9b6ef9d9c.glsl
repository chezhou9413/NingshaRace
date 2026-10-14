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
    float _Additive; // @default = 0
    float _BloomCapture; // @default = 0
    float _BloomIntensity; // @default = 0.8
    float _BloomThreshold; // @default = 0.5
    vec4 _Color; // @default = vec4(0.7, 0.4, 1, 1) @color
};


#line 63
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _Additive; // @default = 0
    float _BloomCapture; // @default = 0
    float _BloomIntensity; // @default = 0.8
    float _BloomThreshold; // @default = 0.5
    vec4 _Color; // @default = vec4(0.7, 0.4, 1, 1) @color
}RU_Material_0;

#line 14946 1
float saturate_0(float x_0)
{

#line 14954
    return clamp(x_0, 0.0, 1.0);
}


#line 74 0
struct v2f_0
{
    vec4 vertex_0;
    vec2 uv_0;
    vec4 color_0;
};


#line 74
vec4 spark_0(v2f_0 i_0)
{

#line 74
    float along_0 = saturate_0(i_0.uv_0.y);

#line 74
    float across_0 = (i_0.uv_0.x - 0.5) * 2.0 - sin(along_0 * 4.5) * (1.0 - along_0) * 0.18000000715255737;

#line 74
    float edge_0 = abs(across_0) / max(pow(saturate_0(sin(along_0 * 3.14159274101257324)), 0.69999998807907104) * mix(0.25, 1.0, along_0), 0.00100000004749745);

#line 74
    float _S1 = max((fwidth((edge_0))), 0.11999999731779099);

#line 74
    float _S2 = along_0 - 0.64999997615814209;

#line 74
    return vec4(mix(RU_Linear(RU_Material_0._Color).xyz, vec3(1.0, 0.89999997615814209, 1.0), vec3((exp2((- across_0 * across_0 * 18.0 - _S2 * _S2 * 28.0))) * 0.64999997615814209)) * i_0.color_0.xyz, (1.0 - smoothstep(1.0 - _S1, 1.0 + _S1, edge_0)) * (smoothstep(0.0, 0.11999999731779099, along_0) * (1.0 - smoothstep(0.80000001192092896, 1.0, along_0))) * i_0.color_0.w * RU_Linear(RU_Material_0._Color).w);
}


#line 74
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 74
layout(location = 1) in vec2 i_uv_0; // @vertex TexCoord0 optional


#line 74
layout(location = 2) in vec4 i_color_0; // @vertex Color optional


#line 74
void RU_SourceMain()
{

#line 74
    v2f_0 _S3 = v2f_0(gl_FragCoord, i_uv_0, i_color_0);

#line 74
    vec4 c_0 = spark_0(_S3);

#line 74
    if((RU_Material_0._BloomCapture) < 0.5)
    {

#line 74
        float _S4 = c_0.w;

#line 74
        entryPointParam_frag_0 = vec4(c_0.xyz * _S4, _S4 * (1.0 - RU_Material_0._Additive));

#line 74
        return;
    }

#line 74
    float _S5 = max(c_0.x, max(c_0.y, c_0.z));

#line 74
    float _S6 = max(RU_Material_0._BloomThreshold * 0.5, 0.00009999999747379);

#line 74
    float soft_0 = clamp(_S5 - RU_Material_0._BloomThreshold + _S6, 0.0, 2.0 * _S6);

#line 74
    float _S7 = c_0.w;

#line 74
    entryPointParam_frag_0 = vec4(c_0.xyz * _S7 * (max(_S5 - RU_Material_0._BloomThreshold, soft_0 * soft_0 / (4.0 * _S6)) / max(_S5, 0.00009999999747379)) * RU_Material_0._BloomIntensity, _S7 * (1.0 - RU_Material_0._Additive));

#line 74
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
