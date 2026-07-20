using HarmonyLib;
using Verse;
using NingshaRaceLib.Combat;

namespace NingshaRaceLib.Patches
{
    //类职责：在蛇腹剑攻击动画期间拦截蛇腹剑本体绘制。
    [HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAiming))]
    public static class Patch_SnakeBellySwordWeaponRendering
    {
        //函数职责：判断当前装备是否处于蛇腹剑攻击隐藏状态。
        public static bool Prefix(Thing eq)
        {
            return !SnakeBellySwordRenderState.IsHidden(eq);
        }
    }
}
