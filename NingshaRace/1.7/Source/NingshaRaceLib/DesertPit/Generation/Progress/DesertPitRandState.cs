using System.Reflection;
using HarmonyLib;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Progress
{
    //结构职责：保存原版随机数生成器的种子与迭代位置，使同一生成步骤能够跨帧延续确定性随机流。
    internal struct DesertPitRandState
    {
        //字段职责：访问原版随机数生成器的当前种子。
        private static readonly FieldInfo SeedField = AccessTools.Field(typeof(Rand), "seed");

        //字段职责：访问原版随机数生成器在当前种子上的迭代位置。
        private static readonly FieldInfo IterationsField = AccessTools.Field(typeof(Rand), "iterations");

        //字段职责：保存当前生成步骤使用的随机种子。
        private readonly uint seed;

        //字段职责：保存当前生成步骤已经消耗的随机数数量。
        private readonly uint iterations;

        //函数职责：根据种子与迭代位置构造一份可恢复的随机状态。
        private DesertPitRandState(uint seed, uint iterations)
        {
            this.seed = seed;
            this.iterations = iterations;
        }

        //函数职责：创建尚未消耗任何随机数的步骤初始状态。
        public static DesertPitRandState FromSeed(int seed)
        {
            return new DesertPitRandState(unchecked((uint)seed), 0u);
        }

        //函数职责：捕获原版随机数生成器当前的种子与迭代位置。
        public static DesertPitRandState Capture()
        {
            return new DesertPitRandState(
                (uint)SeedField.GetValue(null),
                (uint)IterationsField.GetValue(null));
        }

        //函数职责：把保存的种子与迭代位置恢复到原版随机数生成器。
        public void Restore()
        {
            SeedField.SetValue(null, seed);
            IterationsField.SetValue(null, iterations);
        }
    }
}
