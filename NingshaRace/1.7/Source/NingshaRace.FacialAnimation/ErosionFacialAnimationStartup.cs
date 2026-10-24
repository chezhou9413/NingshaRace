using HarmonyLib;
using Verse;

namespace NingshaRaceLib.Compatibility.FA
{
    //仅由 FA 条件加载目录安装侵蚀体面部兼容补丁。
    [StaticConstructorOnStartup]
    internal static class ErosionFacialAnimationStartup
    {
        //让主程序集在未安装 FA 时不解析任何 FA 类型。
        static ErosionFacialAnimationStartup()
        {
            new Harmony("chezhou.race.ningsharace.erosion.fa")
                .PatchAll(typeof(ErosionFacialAnimationStartup).Assembly);
        }
    }
}
