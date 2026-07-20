using HarmonyLib;
using NingshaRaceLib.SandGolem;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Patches
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
}
