using HarmonyLib;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Race.Rendering;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Race.Patches
{
    //类职责：为凝砂角色安装独立身体伤口绘制器。
    [HarmonyPatch(typeof(PawnRenderer), MethodType.Constructor, typeof(Pawn))]
    public static class NingshaWoundDrawerPatch
    {
        //函数职责：仅替换凝砂种族的伤口绘制，不修改其他种族的共享体型定义。
        private static void Postfix(Pawn pawn, ref PawnWoundDrawer ___woundOverlays)
        {
            if (pawn.def == DefOfRefs.NingshaRace) ___woundOverlays = new NingshaWoundDrawer(pawn);
        }
    }
}
