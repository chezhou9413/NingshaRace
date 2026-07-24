using System.Collections.Generic;
using HarmonyLib;
using NingshaRaceLib.Combat;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Patches
{
    //类职责：给装备葬岳的 Pawn 追加格挡模式切换按钮。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_BurialMountainGuardGizmos
    {
        private static readonly Texture2D GuardIcon = ContentFinder<Texture2D>.Get("NingshaRace/Weapons/BurialMountainGreatsword", false);

        //函数职责：在原有 Pawn 操作按钮后追加葬岳格挡开关。
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
        {
            foreach (Gizmo gizmo in values)
            {
                yield return gizmo;
            }

            Comp_BurialMountainGuardMode comp;
            if (__instance == null || __instance.Dead || __instance.Faction != Faction.OfPlayer || !BurialMountainGuardUtility.TryGetGuardComp(__instance, out comp))
            {
                yield break;
            }

            Command_Action command = new Command_Action
            {
                defaultLabel = comp.GuardMode ? "关闭格挡" : "格挡模式",
                defaultDesc = "切换葬岳格挡姿态。格挡中无法攻击，每次受到伤害时最多抵消二十点并积蓄岩土之力，蓄满后震击周围敌人。",
                icon = GuardIcon,
                action = delegate
                {
                    comp.ToggleGuardMode(__instance);
                }
            };

            if (comp.GuardMode)
            {
                command.defaultDesc += "\n\n当前蓄力：" + comp.StoredDamage.ToString("F0");
            }

            yield return command;
        }
    }

    //类职责：让葬岳格挡模式在 Pawn 受伤前抵消伤害并积蓄能量。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
    public static class Patch_BurialMountainGuardDamage
    {
        //函数职责：在原版伤害预处理后按葬岳格挡规则扣减剩余伤害。
        public static void Postfix(Pawn __instance, ref DamageInfo dinfo, ref bool absorbed)
        {
            Comp_BurialMountainGuardMode comp;
            if (BurialMountainGuardUtility.TryGetGuardComp(__instance, out comp))
            {
                comp.AbsorbDamage(__instance, ref dinfo, ref absorbed);
            }
        }
    }

    //类职责：阻止格挡模式中的葬岳持有者开始普通近战，同时允许坠岳斩。
    [HarmonyPatch(typeof(Verb), nameof(Verb.TryStartCastOn), new[]
    {
        typeof(LocalTargetInfo),
        typeof(LocalTargetInfo),
        typeof(bool),
        typeof(bool),
        typeof(bool),
        typeof(bool)
    })]
    public static class Patch_BurialMountainGuardVerb
    {
        //函数职责：在普通近战开始前检查格挡状态，并放行坠岳斩能力。
        public static bool Prefix(Verb __instance, ref bool __result)
        {
            Pawn casterPawn = __instance?.CasterPawn;
            Comp_BurialMountainGuardMode comp;
            if (casterPawn != null
                && BurialMountainGuardUtility.TryGetGuardComp(casterPawn, out comp)
                && comp.GuardMode
                && BurialMountainGuardUtility.ShouldBlockVerb(__instance))
            {
                __result = false;
                if (casterPawn.Faction == Faction.OfPlayer && comp.TryConsumeAttackBlockedMessage())
                {
                    Messages.Message(BurialMountainGuardUtility.GuardDisabledReason, casterPawn, MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }

            return true;
        }
    }

    //类职责：让装备容器内的葬岳格挡 Comp 持续驱动常驻护盾 Mote。
    [HarmonyPatch(typeof(Pawn), "Tick")]
    public static class Patch_BurialMountainGuardTick
    {
        //函数职责：每 Tick 更新装备中的格挡护盾显示。
        public static void Postfix(Pawn __instance)
        {
            Comp_BurialMountainGuardMode comp;
            if (BurialMountainGuardUtility.TryGetGuardComp(__instance, out comp))
            {
                comp.TickEquipped(__instance);
            }
        }
    }
}
