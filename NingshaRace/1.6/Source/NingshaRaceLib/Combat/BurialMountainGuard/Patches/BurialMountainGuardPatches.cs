using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Combat.BurialMountainGuard.Components;
using NingshaRaceLib.Combat.BurialMountainGuard.Utility;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Combat.BurialMountainGuard.Patches
{
    //类职责：给装备葬岳的 Pawn 追加格挡模式切换按钮。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_BurialMountainGuardGizmos
    {
        //字段职责：缓存首次显示格挡按钮时取得的图标，避免补丁类型初始化阶段访问 Unity 资源。
        private static Texture2D guardIcon;

        //属性职责：在主线程实际构建 Gizmo 时按需读取格挡按钮图标。
        private static Texture2D GuardIcon
        {
            get
            {
                if (guardIcon == null)
                {
                    guardIcon = ContentFinder<Texture2D>.Get("NingshaRace/Weapons/BurialMountainGreatsword", false);
                }

                return guardIcon;
            }
        }

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

    //类职责：格挡期间阻止原版战斗等待和 AI 为 Pawn 选择普通攻击 Verb。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.TryGetAttackVerb))]
    public static class Patch_BurialMountainGuardAttackVerbSelection
    {
        //函数职责：格挡期间直接返回空攻击 Verb，避免 Wait_Combat 反复选择并尝试普通攻击。
        public static bool Prefix(Pawn __instance, ref Verb __result)
        {
            if (!BurialMountainGuardUtility.IsGuarding(__instance))
            {
                return true;
            }

            __result = null;
            return false;
        }
    }

    //类职责：格挡期间阻止直接访问近战系统的代码进入原版无可用 Verb 报错分支。
    [HarmonyPatch(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.TryGetMeleeVerb))]
    public static class Patch_BurialMountainGuardMeleeVerbSelection
    {
        //函数职责：格挡期间查询近战动作时直接返回空，其余 Pawn 继续执行原版选择逻辑。
        public static bool Prefix(Pawn_MeleeVerbs __instance, ref Verb __result)
        {
            if (!BurialMountainGuardUtility.IsGuarding(__instance?.Pawn))
            {
                return true;
            }

            __result = null;
            return false;
        }
    }

    //类职责：格挡期间阻止已经建立的近战 Job 从底层入口执行实际攻击。
    [HarmonyPatch(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.TryMeleeAttack))]
    public static class Patch_BurialMountainGuardMeleeAttack
    {
        //函数职责：格挡期间让近战尝试立即返回失败，避免绕过常规 Verb 起手检查。
        public static bool Prefix(Pawn_MeleeVerbs __instance, ref bool __result)
        {
            if (!BurialMountainGuardUtility.IsGuarding(__instance?.Pawn))
            {
                return true;
            }

            __result = false;
            return false;
        }
    }
}
