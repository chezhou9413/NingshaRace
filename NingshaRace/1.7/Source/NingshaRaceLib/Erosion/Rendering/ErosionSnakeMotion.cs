using UnityEngine;

namespace NingshaRaceLib.Erosion.Rendering
{
    //运行时与美术示意渲染共用的连续摆动曲线。
    public static class ErosionSnakeMotion
    {
        //叠加缓慢摆动与少量二次波动，让停顿和回摆平滑过渡。
        public static float Angle(float seconds, float period, float phase, float amplitude)
        {
            float cycle = seconds * (2 * Mathf.PI / period);
            return amplitude * (0.8f * Mathf.Sin(cycle + phase) + 0.2f * Mathf.Sin(2 * cycle + phase * 1.7f));
        }
    }
}
