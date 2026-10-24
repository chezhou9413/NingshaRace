using HarmonyLib;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //原版准备出手时先播放蓄力，由动作接触点继续原版攻击流程。
    [HarmonyPatch(typeof(Verb), nameof(Verb.WarmupComplete))]
    internal static class Patch_MeleeAnimationAttack
    {
        //不提前掷命中或造成伤害，也不直接调用伤害函数。
        private static bool Prefix(Verb __instance)
        {
            if (!(__instance is Verb_MeleeAttack) || !__instance.CasterIsPawn) return true;
            Pawn pawn = __instance.CasterPawn;
            var owner = pawn.TryGetComp<CompMeleeAnimation>();
            if (owner == null || !owner.Accepts(__instance.EquipmentSource) || owner.ResolvingVerb == __instance) return true;
            if (!pawn.Spawned || pawn.Dead || pawn.Downed || !__instance.CanHitTarget(__instance.CurrentTarget)) return false;
            if (pawn.stances.FullBodyBusy && !(pawn.stances.curStance is Stance_Warmup)) return false;
            pawn.rotationTracker.Face(__instance.CurrentTarget.CenterVector3);
            float angle = (__instance.CurrentTarget.CenterVector3 - pawn.DrawPos).AngleFlat();
            float cooldown = __instance.verbProps.AdjustedCooldownTicks(__instance, pawn) / 60f;
            owner.Playback.Attack(owner, angle, cooldown, true);
            pawn.stances.SetStance(new Stance_MeleeAnimation(owner, __instance));
            return false;
        }
    }

    //只有实际命中才使用动作配置的命中停顿，拥刀者默认全部为零。
    [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
    internal static class Patch_MeleeAnimationImpact
    {
        //不会重启动画或再次结算伤害。
        private static void Postfix(Verb_MeleeAttack __instance, bool __result)
        {
            var owner = __instance.CasterPawn.TryGetComp<CompMeleeAnimation>();
            if (owner?.ResolvingVerb == __instance && !__result) owner.Playback.Stop = 0;
        }
    }
}
