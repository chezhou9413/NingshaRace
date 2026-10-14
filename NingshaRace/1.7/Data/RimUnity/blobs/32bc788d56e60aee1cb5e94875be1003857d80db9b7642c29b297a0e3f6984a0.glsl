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
layout(binding = 3, set = 2)
uniform sampler2D _NoiseTex; // @default = gray
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = white


#line 15 0
struct RU_CameraData_0
{
    mat4 ViewProj_0;
};


#line 16
layout(binding = 0, std140) uniform block_RU_CameraData_0
{
    mat4 ViewProj_0;
}RU_Camera_0;

#line 17
struct RU_ObjectData_0
{
    mat4 model_0;
};


#line 18
layout(push_constant, std430) uniform block_RU_ObjectData_0
{
    mat4 model_0;
}RU_Object_0;

#line 56
struct RU_MaterialData_0
{
    float _AutoFitMask; // @default = 1
    vec4 _BubbleColor; // @default = vec4(1, 1, 1, 0.45) @color
    float _BubbleDensity; // @default = 24
    float _BubbleSize; // @default = 0.055
    float _BubbleSpeed; // @default = 0.35
    float _BubbleStrength; // @default = 0.35
    vec4 _EdgeFlowDirection; // @default = vec4(0.25, 1, 0, 0)
    float _EdgeFlowScale; // @default = 8
    float _EdgeFlowSpeed; // @default = 0.55
    float _EdgeFlowStrength; // @default = 0.35
    float _EdgeFlowWidth; // @default = 0.035
    float _FillAmount; // @default = 0.5
    float _FillCurve; // @default = 1.8
    float _FillSoftness; // @default = 0.02
    float _LiquidAngle; // @default = -10
    float _LiquidCenterOffsetX; // @default = 0
    float _LiquidCenterOffsetY; // @default = 0
    vec4 _LiquidColor; // @default = vec4(0.15, 0.65, 1, 0.85) @color
    float _LiquidMaxHeight; // @default = 1.35
    float _LiquidMaxWidth; // @default = 1.35
    float _LiquidMinHeight; // @default = 0.12
    float _LiquidMinWidth; // @default = 0.18
    float _LiquidVerticalShift; // @default = 0.12
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    float _NoiseContrast; // @default = 1.4
    vec4 _NoiseDirection; // @default = vec4(1, 0.35, 0, 0)
    float _NoiseScale; // @default = 18
    float _NoiseSpeed; // @default = 0.35
    float _NoiseStrength; // @default = 0.22
    vec4 _NoiseTex_ST; // @default = vec4(0, 0, 1, 1)
    vec4 _Time;
};


#line 89
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _AutoFitMask; // @default = 1
    vec4 _BubbleColor; // @default = vec4(1, 1, 1, 0.45) @color
    float _BubbleDensity; // @default = 24
    float _BubbleSize; // @default = 0.055
    float _BubbleSpeed; // @default = 0.35
    float _BubbleStrength; // @default = 0.35
    vec4 _EdgeFlowDirection; // @default = vec4(0.25, 1, 0, 0)
    float _EdgeFlowScale; // @default = 8
    float _EdgeFlowSpeed; // @default = 0.55
    float _EdgeFlowStrength; // @default = 0.35
    float _EdgeFlowWidth; // @default = 0.035
    float _FillAmount; // @default = 0.5
    float _FillCurve; // @default = 1.8
    float _FillSoftness; // @default = 0.02
    float _LiquidAngle; // @default = -10
    float _LiquidCenterOffsetX; // @default = 0
    float _LiquidCenterOffsetY; // @default = 0
    vec4 _LiquidColor; // @default = vec4(0.15, 0.65, 1, 0.85) @color
    float _LiquidMaxHeight; // @default = 1.35
    float _LiquidMaxWidth; // @default = 1.35
    float _LiquidMinHeight; // @default = 0.12
    float _LiquidMinWidth; // @default = 0.18
    float _LiquidVerticalShift; // @default = 0.12
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    float _NoiseContrast; // @default = 1.4
    vec4 _NoiseDirection; // @default = vec4(1, 0.35, 0, 0)
    float _NoiseScale; // @default = 18
    float _NoiseSpeed; // @default = 0.35
    float _NoiseStrength; // @default = 0.22
    vec4 _NoiseTex_ST; // @default = vec4(0, 0, 1, 1)
    vec4 _Time;
    float _RimUnitySrgb_MainTex; // @default = 1
    float _RimUnitySrgb_NoiseTex; // @default = 1
}RU_Material_0;

#line 23
vec4 UnityObjectToClipPos_0(vec4 p_0)
{

#line 23
    return ((((((p_0) * (transpose(RU_Object_0.model_0))))) * (transpose(RU_Camera_0.ViewProj_0))));
}


#line 3346 1
layout(location = 1)
out vec2 entryPointParam_vert_uv_0;


#line 3346
layout(location = 5)
out vec2 entryPointParam_vert_rangeUv_0;


#line 3346
layout(location = 2)
out vec4 entryPointParam_vert_color_0;


#line 3346
layout(location = 0) in vec4 v_vertex_0; // @vertex Position required


#line 3346
layout(location = 1) in vec2 v_uv_0; // @vertex TexCoord0 optional


#line 3346
layout(location = 2) in vec4 v_color_0; // @vertex Color optional


#line 145 0
struct v2f_0
{
    vec4 vertex_0;
    vec2 uv_0;
    vec2 rangeUv_0;
    vec4 color_0;
};


#line 145
void main()
{

#line 145
    v2f_0 o_0;

#line 145
    o_0.vertex_0 = UnityObjectToClipPos_0(v_vertex_0);

#line 145
    o_0.uv_0 = v_uv_0.xy * RU_Material_0._MainTex_ST.zwxy.xy + RU_Material_0._MainTex_ST.zwxy.zw;

#line 145
    o_0.rangeUv_0 = v_uv_0;

#line 145
    o_0.color_0 = v_color_0;

#line 145
    v2f_0 _S1 = o_0;

#line 145
    gl_Position = o_0.vertex_0;

#line 145
    entryPointParam_vert_uv_0 = _S1.uv_0;

#line 145
    entryPointParam_vert_rangeUv_0 = _S1.rangeUv_0;

#line 145
    entryPointParam_vert_color_0 = _S1.color_0;

#line 145
    return;
}


// @rimunity-host-matrices 1

// @rimunity-linear-program 1
