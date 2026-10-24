using HarmonyLib;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Combat.SaltedFishGreatsword
{
    //只监听咸鱼大剑实际执行的近战攻击，命中、落空和闪避均可触发台词。
    [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
    internal static class Patch_SaltedFishAttack
    {
        //原版在未生成或全身忙碌时提前退出，排除这些没有出手的调用。
        private static void Prefix(Verb_MeleeAttack __instance, out Pawn __state)
        {
            Pawn pawn = __instance.CasterPawn;
            __state = pawn != null && pawn.Spawned && !pawn.stances.FullBodyBusy
                && __instance.EquipmentSource?.def.defName == "NingshaRace_SaltedFishGreatsword"
                ? pawn : null;
        }

        //只添加视听反馈，不改动原版伤害、命中或攻击冷却。
        private static void Postfix(Pawn __state)
        {
            if (__state == null || !__state.Spawned) return;
            __state.Map.GetComponent<SaltedFishChatOverlay>().Add(__state);
        }
    }
}
