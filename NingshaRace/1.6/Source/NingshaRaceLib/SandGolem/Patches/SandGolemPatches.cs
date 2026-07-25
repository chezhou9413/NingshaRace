using HarmonyLib;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.SandGolem.Health;
using NingshaRaceLib.SandGolem.Lifecycle;
using NingshaRaceLib.SandGolem.Rendering;
using NingshaRaceLib.SandGolem.Tracking;
using NingshaRaceLib.SandGolem.Utility;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.SandGolem.Patches
{
//类职责：让玩家沙傀作为无机械师机械体接受征召和右键移动命令。
    public static class Patch_SandGolemControl
    {
        //函数职责：判断 Pawn 是否是当前补丁允许玩家直接控制的沙傀。
        private static bool IsPlayerControlledSandGolem(Pawn pawn)
        {
            return SandGolemUtility.IsSandGolem(pawn) && pawn.Faction == Faction.OfPlayer && !pawn.Dead;
        }

        //类职责：在原版动态组件刷新后补回沙傀需要的玩家控制组件。
        [HarmonyPatch(typeof(PawnComponentsUtility), nameof(PawnComponentsUtility.AddAndRemoveDynamicComponents))]
        public static class Patch_PawnComponentsUtility_AddAndRemoveDynamicComponents
        {
            //函数职责：补齐沙傀的工作、玩家设置和征召控制器。
            public static void Postfix(Pawn pawn, bool actAsIfSpawned = false)
            {
                if (!IsPlayerControlledSandGolem(pawn))
                {
                    return;
                }

                SandGolemUtility.EnsurePlayerControlComponents(pawn, actAsIfSpawned);
                if (!SandGolemUtility.IsMovementLockedSandGolem(pawn))
                {
                    SandGolemUtility.SetMovementDisabled(pawn, false);
                }
            }
        }

        //类职责：允许玩家沙傀通过原版玩家可控 Pawn 判定。
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.IsPlayerControlled), MethodType.Getter)]
        public static class Patch_Pawn_IsPlayerControlled
        {
            //函数职责：沙傀在生成后始终视为玩家可控单位。
            public static void Postfix(Pawn __instance, ref bool __result)
            {
                if (__result || !IsPlayerControlledSandGolem(__instance) || !__instance.Spawned || __instance.InMentalState)
                {
                    return;
                }

                __result = true;
            }
        }

        //类职责：允许玩家沙傀通过右键命令入口的接单判定。
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.CanTakeOrder), MethodType.Getter)]
        public static class Patch_Pawn_CanTakeOrder
        {
            //函数职责：沙傀未倒地且不处于精神状态时允许接收玩家命令。
            public static void Postfix(Pawn __instance, ref bool __result)
            {
                if (__result || !IsPlayerControlledSandGolem(__instance) || !__instance.Spawned || __instance.Downed || __instance.InMentalState)
                {
                    return;
                }

                __result = true;
            }
        }

        //类职责：让没有机械师控制组的沙傀仍然显示征召按钮。
        [HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.ShowDraftGizmo), MethodType.Getter)]
        public static class Patch_Pawn_DraftController_ShowDraftGizmo
        {
            //函数职责：只为玩家沙傀覆盖原版机械体控制组隐藏条件。
            public static void Postfix(Pawn_DraftController __instance, ref bool __result)
            {
                if (__result || !IsPlayerControlledSandGolem(__instance?.pawn))
                {
                    return;
                }

                __result = true;
            }
        }

        //类职责：解除沙傀征召按钮对机械师、带宽和控制组的依赖。
        [HarmonyPatch(typeof(MechanitorUtility), nameof(MechanitorUtility.CanDraftMech))]
        public static class Patch_MechanitorUtility_CanDraftMech
        {
            //函数职责：玩家沙傀总是可征召，倒地和死亡等状态由征召控制器自身处理。
            public static bool Prefix(Pawn mech, ref AcceptanceReport __result)
            {
                if (!IsPlayerControlledSandGolem(mech))
                {
                    return true;
                }

                __result = true;
                return false;
            }
        }

        //类职责：解除沙傀移动命令对机械师指挥范围的依赖。
        [HarmonyPatch(typeof(MechanitorUtility), nameof(MechanitorUtility.InMechanitorCommandRange))]
        public static class Patch_MechanitorUtility_InMechanitorCommandRange
        {
            //函数职责：玩家沙傀不需要机械师即可接收地图内移动命令。
            public static bool Prefix(Pawn mech, ref bool __result)
            {
                if (!IsPlayerControlledSandGolem(mech))
                {
                    return true;
                }

                __result = true;
                return false;
            }
        }
    }

//类职责：拦截沙傀死亡，让其播放消散动画而不是生成普通尸体。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill_SandGolem
    {
        //函数职责：沙傀被击杀时启动消散并跳过原版死亡流程。
        public static bool Prefix(Pawn __instance)
        {
            if (!SandGolemUtility.IsSandGolem(__instance))
            {
                return true;
            }

            GameComponent_SandGolemTracker.Current?.BeginDissolve(__instance, destroyPawn: true);
            return false;
        }
    }

//类职责：给沙傀选择面板添加手动收回按钮。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_SandGolemGizmos
    {
        //字段职责：缓存首次显示收回按钮时取得的技能图标，避免补丁类型初始化阶段访问 Unity 资源。
        private static Texture2D recallIcon;

        //属性职责：在主线程实际构建 Gizmo 时按需读取沙傀收回图标。
        private static Texture2D RecallIcon
        {
            get
            {
                if (recallIcon == null)
                {
                    recallIcon = ContentFinder<Texture2D>.Get("UI/Abilities/SummonSandGolem", reportFailure: false);
                }

                return recallIcon;
            }
        }

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

//类职责：显式隐藏沙傀头顶状态图标，避免精神崩溃、惊恐等图标绘制。
    [HarmonyPatch(typeof(PawnRenderNodeWorker_OverlayStatus), nameof(PawnRenderNodeWorker_OverlayStatus.CanDrawNow))]
    public static class Patch_PawnRenderNodeWorker_OverlayStatus_CanDrawNow_SandGolem
    {
        //函数职责：沙傀不绘制头顶状态图标。
        public static void Postfix(PawnDrawParms parms, ref bool __result)
        {
            if (__result && SandGolemUtility.IsSandGolem(parms.pawn))
            {
                __result = false;
            }
        }
    }

//类职责：把玩家沙傀追加进原版工作面板，让沙傀使用原版工作优先级 UI。
    public static class Patch_SandGolemWorkTab
    {
        //类职责：扩展原版工作窗口的 Pawn 来源。
        [HarmonyPatch(typeof(MainTabWindow_Work), "Pawns", MethodType.Getter)]
        public static class Patch_MainTabWindow_Work_Pawns
        {
            //函数职责：在原版自由殖民者后追加当前地图可工作的玩家沙傀。
            public static void Postfix(ref IEnumerable<Pawn> __result)
            {
                __result = WithSandGolems(__result);
            }
        }

        //函数职责：合并原版 Pawn 枚举和当前地图玩家沙傀。
        private static IEnumerable<Pawn> WithSandGolems(IEnumerable<Pawn> original)
        {
            HashSet<Pawn> yielded = new HashSet<Pawn>();
            if (original != null)
            {
                foreach (Pawn pawn in original)
                {
                    if (pawn == null || !yielded.Add(pawn))
                    {
                        continue;
                    }

                    yield return pawn;
                }
            }

            Map map = Find.CurrentMap;
            if (map == null)
            {
                yield break;
            }

            List<Pawn> playerPawns = map.mapPawns.PawnsInFaction(Faction.OfPlayer);
            for (int i = 0; i < playerPawns.Count; i++)
            {
                Pawn pawn = playerPawns[i];
                if (!IsPlayerSandGolem(pawn) || !yielded.Add(pawn))
                {
                    continue;
                }

                SandGolemUtility.EnsurePlayerControlComponents(pawn);
                if (CanShowInWorkTab(pawn))
                {
                    yield return pawn;
                }
            }
        }

        //函数职责：判断 Pawn 是否是当前地图玩家阵营沙傀。
        private static bool IsPlayerSandGolem(Pawn pawn)
        {
            return SandGolemUtility.IsSandGolem(pawn)
                && pawn.Faction == Faction.OfPlayer
                && !pawn.Dead
                && !pawn.DevelopmentalStage.Baby();
        }

        //函数职责：判断沙傀是否已经具备原版工作表绘制所需的运行时组件。
        private static bool CanShowInWorkTab(Pawn pawn)
        {
            return pawn.skills != null
                && pawn.workSettings != null
                && pawn.workSettings.EverWork;
        }
    }
}
