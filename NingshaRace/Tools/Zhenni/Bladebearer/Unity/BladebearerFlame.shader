Shader "NingshaRace/Zhenni/Bladebearer/Flame"
{
    Properties
    {
        _MainTex ("火焰原图", 2D) = "white" {}
        _DisplacementTex ("循环置换图 RG", 2D) = "gray" {}
        _Color ("颜色与透明度", Color) = (1, 1, 1, 1)
        _Additive ("光束叠加比例", Range(0, 1)) = 0
        _Displacement ("横纵位移 UV", Vector) = (0.015, 0.004, 0, 0)
        _Root ("固定根部 UV", Vector) = (0.5, 0.1, 0, 0)
        _Tip ("飘动尾端 UV", Vector) = (0.5, 0.9, 0, 0)
        _RootLock ("根部固定比例", Range(0, 0.8)) = 0.12
        _BendPower ("尾端位移曲线", Range(0.5, 4)) = 1.3
        _NoiseScale ("横向细节与纵向波长", Vector) = (0.5, 0.7, 0, 0)
        _Speed ("沿飘散方向传播速度", Range(0, 6)) = 0.9
        _FlowAngle ("飘散偏转角度", Range(-180, 180)) = 0
        _DetailStrength ("细节扰动比例", Range(0, 1)) = 0.12
        _Breakup ("二维置换破碎幅度", Range(0, 2)) = 0
        _BreakupScale ("二维置换横纵密度", Vector) = (1.8, 2.2, 0, 0)
        _Phase ("动画相位", Float) = 0
    }
    //沿火焰方向滚动置换图，根部固定，颜色与透明轮廓一起形变。
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "BladebearerFlameCommon.cginc"
            ENDCG
        }
    }
    Fallback Off
}
