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


#line 66 0
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = black


#line 68



#line 56
struct RU_MaterialData_0
{
    vec4 _Color; // @default = vec4(3.56487, 3.56487, 3.56487, 1) @color
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    float _WhiteCorePower; // @default = 4
};


#line 61
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    vec4 _Color; // @default = vec4(3.56487, 3.56487, 3.56487, 1) @color
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    float _WhiteCorePower; // @default = 4
    float _RimUnitySrgb_MainTex; // @default = 1
}RU_Material_0;

#line 14572 1
float rcp_0(float x_0)
{

#line 14578
    return 1.0 / x_0;
}


#line 14946
float saturate_0(float x_1)
{

#line 14954
    return clamp(x_1, 0.0, 1.0);
}


#line 70 0
vec4 RU_tex2D_MainTex_0(vec2 uv_0)
{

#line 70
    return (RU_SampleLinear(texture(_MainTex, (vec2(uv_0.x, 1.0 - uv_0.y))), RU_Material_0._RimUnitySrgb_MainTex));
}


#line 70
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 70
layout(location = 1) in vec2 i_uv_0; // @vertex TexCoord0 optional


#line 70
layout(location = 2) in vec4 i_color_0; // @vertex Color optional


#line 78
void RU_SourceMain()
{

#line 78
    vec4 sampleColor_0 = RU_tex2D_MainTex_0(i_uv_0);

#line 78
    vec3 _S1 = sampleColor_0.xyz;

#line 78
    entryPointParam_frag_0 = vec4(_S1 * rcp_0(1.0 + (RU_Material_0._WhiteCorePower - 1.0) * (1.0 - saturate_0(dot(_S1, vec3(0.2125999927520752, 0.71520000696182251, 0.07220000028610229))))), sampleColor_0.w) * RU_Linear(RU_Material_0._Color) * i_color_0;

#line 78
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
