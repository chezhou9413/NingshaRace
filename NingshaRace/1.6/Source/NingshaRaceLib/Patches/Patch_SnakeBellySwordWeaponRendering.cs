using HarmonyLib;
using Verse;
using NingshaRaceLib.Combat;

namespace NingshaRaceLib.Patches
{
    //类职责：在特殊武器动画期间拦截原版瞄准武器绘制，避免与自定义动画重叠。
    [HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAiming))]
    public static class Patch_SnakeBellySwordWeaponRendering
    {
        //函数职责：判断当前装备是否处于蛇腹剑攻击或坠岳斩飞行隐藏状态。
        public static bool Prefix(Thing eq)
        {
            return !SnakeBellySwordRenderState.IsHidden(eq)
                && !FallingMountainSlashRenderUtility.ShouldHideOriginalWeapon(eq);
        }
    }
}
