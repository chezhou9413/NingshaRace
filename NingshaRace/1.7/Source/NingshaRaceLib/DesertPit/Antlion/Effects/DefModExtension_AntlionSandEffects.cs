using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.DesertPit.Antlion.Effects
{
    //类职责：通过沙尘定义配置蚁狮视觉密度和尺寸，不改变伏击范围、动画时长或战斗数值。
    public sealed class DefModExtension_AntlionSandEffects : DefModExtension
    {
        public int buriedIntervalTicks = 16;
        public int transitionIntervalTicks = 5;
        public int buriedGrains = 12;
        public int transitionGrains = 14;
        public int eruptionGrains = 36;
        public float buriedRadius = 0.55f;
        public FloatRange buriedGrainScale = new FloatRange(0.085f, 0.15f);
        public FloatRange movingGrainScale = new FloatRange(0.10f, 0.20f);
        public FloatRange buriedDustScale = new FloatRange(0.55f, 0.80f);
        public FloatRange movingDustScale = new FloatRange(0.85f, 1.20f);

        //函数职责：检查发射间隔、单次粒子预算和尺度，避免配置制造持续满屏沙雾。
        public override IEnumerable<string> ConfigErrors()
        {
            if (buriedIntervalTicks < 8 || transitionIntervalTicks < 3)
                yield return "蚁狮潜伏粒子间隔不得小于八，切换粒子间隔不得小于三。";
            if (!ValidCount(buriedGrains) || !ValidCount(transitionGrains) || !ValidCount(eruptionGrains))
                yield return "蚁狮每次沙粒数量必须在零至六十四之间。";
            if (!FinitePositive(buriedRadius) || buriedRadius > 1.5f)
                yield return "蚁狮潜伏沙粒半径必须大于零且不超过一点五格。";
            if (!ValidScale(buriedGrainScale) || !ValidScale(movingGrainScale)
                || !ValidScale(buriedDustScale) || !ValidScale(movingDustScale))
                yield return "蚁狮沙粒和沙尘尺寸必须为有序正数范围，且最大值不超过两格。";
        }

        //函数职责：限制单次发射数量，保持每只蚁狮的特效负载有明确上界。
        private static bool ValidCount(int count)
        {
            return count >= 0 && count <= 64;
        }

        //函数职责：验证正数尺寸范围，不接受非数值或无穷值。
        private static bool ValidScale(FloatRange range)
        {
            return FinitePositive(range.min) && FinitePositive(range.max) && range.max >= range.min && range.max <= 2f;
        }

        //函数职责：提供尺寸与半径共同使用的有限正数检查。
        private static bool FinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }
    }
}
