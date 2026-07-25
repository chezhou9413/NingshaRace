using HarmonyLib;
using Verse;

using NingshaRaceLib.Combat.FallingMountainSlash.Flight;
using NingshaRaceLib.Combat.FallingMountainSlash.Rendering;
using NingshaRaceLib.Combat.FallingMountainSlash.Verbs;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Combat.FallingMountainSlash.Patches
{
    //类职责：在坠岳斩飞行期间拦截原版常态持械绘制，只保留 Flyer 的自定义挥刀。
    [HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawCarriedWeapon))]
    public static class Patch_FallingMountainSlashCarriedWeaponRendering
    {
        //函数职责：仅允许不属于坠岳斩飞行 Pawn 的武器进入原版绘制流程。
        public static bool Prefix(ThingWithComps weapon)
        {
            return !FallingMountainSlashRenderUtility.ShouldHideOriginalWeapon(weapon);
        }
    }
}
