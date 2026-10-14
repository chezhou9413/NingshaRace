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
    vec4 _StoneColor; // @default = vec4(0.13, 0.105, 0.075, 1) @color
    vec4 _VeinColor; // @default = vec4(0.23, 0.18, 0.115, 1) @color
};


#line 60
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    vec4 _StoneColor; // @default = vec4(0.13, 0.105, 0.075, 1) @color
    vec4 _VeinColor; // @default = vec4(0.23, 0.18, 0.115, 1) @color
}RU_Material_0;

#line 14946 1
float saturate_0(float x_0)
{

#line 14954
    return clamp(x_0, 0.0, 1.0);
}


#line 65 0
float grain_0(vec2 p_0)
{

#line 65
    return fract(sin(dot(p_0, vec2(127.09999847412109375, 311.70001220703125))) * 43758.546875);
}


#line 65
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 65
layout(location = 1) in vec2 input_uv_0; // @vertex TexCoord0 optional


#line 65
void RU_SourceMain()
{

#line 65
    float sand_0 = grain_0(floor(input_uv_0 * 256.0));

#line 65
    entryPointParam_frag_0 = vec4(mix(RU_Linear(RU_Material_0._StoneColor).xyz, RU_Linear(RU_Material_0._VeinColor).xyz, vec3(pow(saturate_0(sin(input_uv_0.y * 50.26548004150390625 + sin(input_uv_0.x * 12.56637001037597656) * 1.70000004768371582)), 12.0) * 0.31999999284744263 + sand_0 * 0.18000000715255737)) * (0.93999999761581421 + sand_0 * 0.11999999731779099 - step(0.9869999885559082, sand_0) * 0.18000000715255737), 1.0);

#line 65
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
