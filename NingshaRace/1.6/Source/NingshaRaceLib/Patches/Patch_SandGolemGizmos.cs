using System.Collections.Generic;
using HarmonyLib;
using NingshaRaceLib.SandGolem;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Patches
{
    //类职责：给沙傀选择面板添加手动收回按钮。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_SandGolemGizmos
    {
        //字段职责：缓存收回按钮使用的技能图标。
        private static readonly Texture2D RecallIcon = ContentFinder<Texture2D>.Get("UI/Abilities/SummonSandGolem", reportFailure: false);

        //函数职责：在原有 Pawn 操作按钮后追加沙傀收回按钮。
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
        {
            foreach (Gizmo gizmo in values)
            {
                yield return gizmo;
            }

            if (!SandGolemUtility.IsSandGolem(__instance) || __instance.Faction != Faction.OfPlayer || __instance.Dead)
            {
                yield break;
            }

            Command_Action command = new Command_Action
            {
                defaultLabel = "收回沙傀",
                defaultDesc = "让沙傀原地消散并返回散沙状态。",
                icon = RecallIcon,
                action = delegate
                {
                    GameComponent_SandGolemTracker.Current?.BeginDissolve(__instance, destroyPawn: true);
                }
            };

            if (GameComponent_SandGolemTracker.Current != null && GameComponent_SandGolemTracker.Current.TryGetState(__instance, out SandGolemRenderState state) && state.phase == SandGolemPhase.Dissolving)
            {
                command.Disabled = true;
                command.disabledReason = "沙傀正在消散";
            }

            yield return command;
        }
    }
}
