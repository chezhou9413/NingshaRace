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


#line 56 0
struct RU_MaterialData_0
{
    float _AccretionRadius; // @default = 0.8
    float _AccretionWidth; // @default = 0.99
    float _AutoFitBox; // @default = 1
    float _BlackHoleAspect; // @default = 1.7777778
    vec4 _BlackHoleCenter; // @default = vec4(0.5, 0.72, 0, 0)
    float _BlackHoleRadius; // @default = 0.72
    float _BlackHoleSpin; // @default = 1
    float _BlackHoleStrength; // @default = 1
    vec4 _BlackHoleView; // @default = vec4(0, 0, 0, 0)
    float _BoxPadding; // @default = 0.08
    float _DiskBrightness; // @default = 1
    vec4 _DiskColdColor; // @default = vec4(0.5, 0.35, 0.1, 1) @color
    float _DiskFlatten; // @default = 50
    vec4 _DiskHotColor; // @default = vec4(1, 0.8, 0.6, 1) @color
    float _EmissionSoftness; // @default = 0.3
    float _EmissionThreshold; // @default = 0.22
    float _EventHorizon; // @default = 0.3
    float _GlowStrength; // @default = 1
    float _HorizonSoftness; // @default = 0.01
    vec4 _LensColor; // @default = vec4(1, 0.9, 0.7, 1) @color
    float _PixelAlphaThreshold; // @default = 0.05
    vec4 _PositionTex_TexelSize;
    float _RayStepSize; // @default = 0.02
    float _RaymarchSteps; // @default = 200
    vec4 _Time;
    float _ViewportFadeStart; // @default = 0.62
};


#line 84
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _AccretionRadius; // @default = 0.8
    float _AccretionWidth; // @default = 0.99
    float _AutoFitBox; // @default = 1
    float _BlackHoleAspect; // @default = 1.7777778
    vec4 _BlackHoleCenter; // @default = vec4(0.5, 0.72, 0, 0)
    float _BlackHoleRadius; // @default = 0.72
    float _BlackHoleSpin; // @default = 1
    float _BlackHoleStrength; // @default = 1
    vec4 _BlackHoleView; // @default = vec4(0, 0, 0, 0)
    float _BoxPadding; // @default = 0.08
    float _DiskBrightness; // @default = 1
    vec4 _DiskColdColor; // @default = vec4(0.5, 0.35, 0.1, 1) @color
    float _DiskFlatten; // @default = 50
    vec4 _DiskHotColor; // @default = vec4(1, 0.8, 0.6, 1) @color
    float _EmissionSoftness; // @default = 0.3
    float _EmissionThreshold; // @default = 0.22
    float _EventHorizon; // @default = 0.3
    float _GlowStrength; // @default = 1
    float _HorizonSoftness; // @default = 0.01
    vec4 _LensColor; // @default = vec4(1, 0.9, 0.7, 1) @color
    float _PixelAlphaThreshold; // @default = 0.05
    vec4 _PositionTex_TexelSize;
    float _RayStepSize; // @default = 0.02
    float _RaymarchSteps; // @default = 200
    vec4 _Time;
    float _ViewportFadeStart; // @default = 0.62
    float _RimUnitySrgb_PositionTex; // @default = 1
}RU_Material_0;

#line 112
layout(binding = 1, set = 2)
uniform sampler2D _PositionTex; // @default = white


#line 114



#line 9392 1
void clip_0(float x_0)
{

#line 9398
    if(x_0 < 0.0)
    {

#line 9398
        discard;

#line 9398
    }

    return;
}


#line 14946
float saturate_0(float x_1)
{

#line 14954
    return clamp(x_1, 0.0, 1.0);
}


#line 116 0
vec4 RU_tex2D_PositionTex_0(vec2 uv_0)
{

#line 116
    return (RU_SampleLinear(texture(_PositionTex, (vec2(uv_0.x, 1.0 - uv_0.y))), RU_Material_0._RimUnitySrgb_PositionTex));
}


#line 124
vec4 GetPositionBox_0()
{

#line 124
    const vec2 _S1 = vec2(1.0, 1.0);

#line 124
    const vec2 _S2 = vec2(0.0, 0.0);

#line 124
    vec2 minUv_0 = _S1;

#line 124
    vec2 maxUv_0 = _S2;

#line 124
    float hasPixels_0 = 0.0;

#line 124
    int y_0 = 0;

#line 124
    [[unroll]]
    for(;;)
    {

#line 124
        if(y_0 < 8)
        {
        }
        else
        {

#line 124
            break;
        }

#line 124
        int x_2 = 0;

#line 124
        [[unroll]]
        for(;;)
        {

#line 124
            if(x_2 < 8)
            {
            }
            else
            {

#line 124
                break;
            }

#line 124
            vec2 sampleUv_0 = (vec2(float(x_2), float(y_0)) + 0.5) * 0.125;

#line 124
            float isPixel_0 = step(RU_Material_0._PixelAlphaThreshold, RU_tex2D_PositionTex_0(sampleUv_0).w);

#line 124
            vec2 _S3 = vec2(isPixel_0);

#line 124
            vec2 _S4 = mix(minUv_0, min(minUv_0, sampleUv_0), _S3);

#line 124
            vec2 _S5 = mix(maxUv_0, max(maxUv_0, sampleUv_0), _S3);

#line 124
            float _S6 = max(hasPixels_0, isPixel_0);

#line 124
            int _S7 = x_2 + 1;

#line 124
            minUv_0 = _S4;

#line 124
            maxUv_0 = _S5;

#line 124
            hasPixels_0 = _S6;

#line 124
            x_2 = _S7;

#line 124
        }

#line 124
        y_0 = y_0 + 1;

#line 124
    }

#line 124
    vec2 texturePixels_0 = max(RU_Material_0._PositionTex_TexelSize.zw, _S1);

#line 124
    vec2 sampledPixels_0 = max(maxUv_0 - minUv_0 + 0.125, vec2(0.125, 0.125)) * texturePixels_0;

#line 124
    vec2 _S8 = vec2(hasPixels_0 * saturate_0(RU_Material_0._AutoFitBox));

#line 124
    return vec4(mix(vec2(0.5, 0.5), (minUv_0 + maxUv_0) * 0.5, _S8), max(mix(_S1, max(sampledPixels_0.x, sampledPixels_0.y) / texturePixels_0 * (1.0 + max(RU_Material_0._BoxPadding, 0.0) * 2.0), _S8), vec2(0.00009999999747379, 0.00009999999747379)));
}


#line 124
void BHBuildCameraRay_0(vec2 uv_1, out vec3 rayOrigin_0, out vec3 rayDirection_0)
{

#line 124
    vec2 screen_0 = -1.0 + 2.0 * uv_1;

#line 124
    screen_0[0] = screen_0[0] * max(RU_Material_0._BlackHoleAspect, 0.00100000004749745);

#line 124
    const vec3 lookAt_0 = vec3(0.0, -0.10000000149011612, 0.0);

#line 124
    float eyeAngle_0 = RU_Material_0._BlackHoleView.x * 3.14159274101257324 * 2.0;

#line 124
    float eyeAngleY_0 = (RU_Material_0._BlackHoleView.y - 0.23999999463558197) * 3.14159274101257324 * 2.0;

#line 124
    float _S9 = sin(eyeAngleY_0);

#line 124
    vec3 _S10 = vec3(2.0 * cos(eyeAngle_0) * _S9, 2.0 * cos(eyeAngleY_0), 2.0 * sin(eyeAngle_0) * _S9);

#line 124
    rayOrigin_0 = _S10;

#line 124
    vec3 front_0 = normalize(lookAt_0 - _S10);

#line 124
    vec3 left_0 = normalize(cross(normalize(vec3(0.0, 1.0, -0.10000000149011612)), front_0));

#line 124
    rayDirection_0 = normalize(front_0 * 1.5 + left_0 * screen_0.x + normalize(cross(front_0, left_0)) * screen_0.y);

#line 124
    return;
}


#line 124
float BHHash13_0(vec3 value_0)
{

#line 124
    vec3 _S11 = fract(value_0 * vec3(0.16531999409198761, 0.17369000613689423, 0.15786999464035034));

#line 124
    vec3 _S12 = _S11 + dot(_S11.xyz, _S11.yzx + 19.19000053405761719);

#line 124
    return fract(_S12.x * _S12.y * _S12.z);
}


#line 124
float BHDistanceSphere_0(vec3 position_0, float radius_0)
{

#line 124
    return length(position_0) - radius_0;
}


#line 124
float BHDistanceTorus_0(vec3 position_1, vec2 torus_0)
{

#line 124
    return length(vec2(length(position_1.xz) - torus_0.x, position_1.y)) - torus_0.y;
}


#line 124
vec4 BHRaymarch_0(vec2 uv_2)
{

#line 124
    vec3 rayPosition_0;

#line 124
    vec3 rayVelocity_0;

#line 124
    BHBuildCameraRay_0(uv_2, rayPosition_0, rayVelocity_0);

#line 124
    float time_0 = RU_Material_0._Time.y * RU_Material_0._BlackHoleSpin;

#line 124
    const vec3 _S13 = vec3(0.0, 0.0, 0.0);

#line 124
    float _S14 = max(RU_Material_0._EventHorizon, 0.00100000004749745);

#line 124
    float _S15 = 5.0 * RU_Material_0._BlackHoleStrength * 0.00100000004749745;

#line 124
    rayPosition_0 = rayPosition_0 + rayVelocity_0 * BHHash13_0(rayVelocity_0 + vec3(time_0, time_0, time_0)) * 0.01999999955296516;

#line 124
    float noncaptured_0 = 1.0;

#line 124
    float captured_0 = 0.0;

#line 124
    int stepIndex_0 = 0;

#line 124
    vec3 color_0 = _S13;

#line 124
    [[dont_unroll]]
    for(;;)
    {

#line 124
        if(stepIndex_0 < 200)
        {
        }
        else
        {

#line 124
            break;
        }

#line 124
        if(stepIndex_0 >= int(RU_Material_0._RaymarchSteps))
        {

#line 124
            break;
        }

#line 124
        vec3 _S16 = rayPosition_0 + rayVelocity_0 * RU_Material_0._RayStepSize * noncaptured_0;

#line 124
        rayPosition_0 = _S16;

#line 124
        vec3 gravityVector_0 = _S13 - _S16;

#line 124
        float _S17 = max(dot(gravityVector_0, gravityVector_0), 9.99999997475242708e-07);

#line 124
        rayVelocity_0 = rayVelocity_0 + normalize(gravityVector_0) * (_S15 / _S17);

#line 124
        float noncaptured_1 = smoothstep(0.0, max(RU_Material_0._HorizonSoftness, 0.00009999999747379), BHDistanceSphere_0(_S16, _S14));

#line 124
        float _S18 = max(captured_0, 1.0 - noncaptured_1);

#line 124
        float _S19 = length(gravityVector_0) - _S14;

#line 124
        vec3 color_1 = color_0 + max(_S13, mix(RU_Linear(RU_Material_0._DiskHotColor).xyz, RU_Linear(RU_Material_0._DiskColdColor).xyz, vec3(pow(max(_S19, 0.0), 2.0))) * (0.05000000074505806 * (4.0 / (0.00100000004749745 + _S19 * 50.0))) * step(0.0, - BHDistanceTorus_0(_S16 * vec3(1.0, RU_Material_0._DiskFlatten, 1.0), vec2(RU_Material_0._AccretionRadius, RU_Material_0._AccretionWidth))) * noncaptured_1) * RU_Material_0._DiskBrightness + RU_Linear(RU_Material_0._LensColor).xyz * (1.0 / _S17) * 0.00300000002607703 * RU_Material_0._GlowStrength * noncaptured_1;

#line 124
        int _S20 = stepIndex_0 + 1;

#line 124
        noncaptured_0 = noncaptured_1;

#line 124
        captured_0 = _S18;

#line 124
        stepIndex_0 = _S20;

#line 124
        color_0 = color_1;

#line 124
    }

#line 124
    return vec4(color_0, saturate_0(captured_0));
}


#line 124
float GetBlackHoleEmissionMask_0(vec2 blackHoleUv_0, vec3 emission_0)
{

#line 124
    return smoothstep(RU_Material_0._EmissionThreshold, RU_Material_0._EmissionThreshold + max(RU_Material_0._EmissionSoftness, 0.00100000004749745), max(emission_0.x, max(emission_0.y, emission_0.z))) * (1.0 - smoothstep(min(RU_Material_0._ViewportFadeStart, 0.87000000476837158), 0.87999999523162842, max(abs(blackHoleUv_0.x - 0.5), abs(blackHoleUv_0.y - 0.5))));
}


#line 124
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 124
layout(location = 1) in vec2 i_effectUv_0; // @vertex TexCoord0 optional


#line 124
void RU_SourceMain()
{

#line 124
    vec4 positionBox_0 = GetPositionBox_0();

#line 124
    vec2 blackHoleUv_1 = ((i_effectUv_0 - positionBox_0.xy) / positionBox_0.zw + 0.5 - RU_Material_0._BlackHoleCenter.xy) / vec2(max(RU_Material_0._BlackHoleRadius * RU_Material_0._BlackHoleAspect, 0.00100000004749745), max(RU_Material_0._BlackHoleRadius, 0.00100000004749745)) + 0.5;

#line 124
    vec2 blackHoleEdge_0 = abs(blackHoleUv_1 - 0.5);

#line 124
    const vec4 _S21 = vec4(0.0, 0.0, 0.0, 0.0);

#line 124
    vec4 blackHole_0;

#line 124
    if((max(blackHoleEdge_0.x, blackHoleEdge_0.y)) <= 0.89999997615814209)
    {

#line 124
        blackHole_0 = BHRaymarch_0(blackHoleUv_1);

#line 124
    }
    else
    {

#line 124
        blackHole_0 = _S21;

#line 124
    }

#line 124
    vec3 emission_1 = blackHole_0.xyz;

#line 124
    vec3 emission_2 = emission_1 * GetBlackHoleEmissionMask_0(blackHoleUv_1, emission_1);

#line 124
    float _S22 = blackHole_0.w;

#line 124
    clip_0(max(_S22, max(emission_2.x, max(emission_2.y, emission_2.z))) - 0.00100000004749745);

#line 124
    entryPointParam_frag_0 = vec4(emission_2, _S22);

#line 124
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
