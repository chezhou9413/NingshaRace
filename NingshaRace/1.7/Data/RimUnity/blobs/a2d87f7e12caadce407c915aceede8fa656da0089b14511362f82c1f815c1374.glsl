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


#line 72 0
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = black


#line 74



#line 56
struct RU_MaterialData_0
{
    vec4 _DarkColor; // @default = vec4(0.035, 0.028, 0.05, 1) @color
    vec4 _EdgeColor; // @default = vec4(0.13, 0.11, 0.17, 1) @color
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    float _MaskEnd; // @default = 0.5
    float _MaskStart; // @default = 0.06
    float _Opacity; // @default = 0.38
};


#line 64
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    vec4 _DarkColor; // @default = vec4(0.035, 0.028, 0.05, 1) @color
    vec4 _EdgeColor; // @default = vec4(0.13, 0.11, 0.17, 1) @color
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    float _MaskEnd; // @default = 0.5
    float _MaskStart; // @default = 0.06
    float _Opacity; // @default = 0.38
    float _RimUnitySrgb_MainTex; // @default = 1
}RU_Material_0;

#line 14946 1
float saturate_0(float x_0)
{

#line 14954
    return clamp(x_0, 0.0, 1.0);
}


#line 76 0
vec4 RU_tex2D_MainTex_0(vec2 uv_0)
{

#line 76
    return (RU_SampleLinear(texture(_MainTex, (vec2(uv_0.x, 1.0 - uv_0.y))), RU_Material_0._RimUnitySrgb_MainTex));
}


#line 76
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 76
layout(location = 1) in vec2 i_uv_0; // @vertex TexCoord0 optional


#line 76
layout(location = 2) in vec4 i_color_0; // @vertex Color optional


#line 84
void RU_SourceMain()
{

#line 84
    vec4 sampleColor_0 = RU_tex2D_MainTex_0(i_uv_0);

#line 84
    float mask_0 = smoothstep(RU_Material_0._MaskStart, max(RU_Material_0._MaskStart + 0.00100000004749745, RU_Material_0._MaskEnd), dot(sampleColor_0.xyz, vec3(0.2125999927520752, 0.71520000696182251, 0.07220000028610229)));

#line 84
    entryPointParam_frag_0 = vec4(mix(RU_Linear(RU_Material_0._EdgeColor).xyz, RU_Linear(RU_Material_0._DarkColor).xyz, vec3(mask_0)), saturate_0(mask_0 * sampleColor_0.w * i_color_0.w * saturate_0(max(i_color_0.x, max(i_color_0.y, i_color_0.z))) * RU_Material_0._Opacity));

#line 84
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
