using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using NingshaRaceLib.SandGolem.Utility;
using RimWorld;
using Verse;

namespace NingshaRaceLib.SandGolem.Patches
{
    //沙傀制作的半成品不绑定个人，派工时仍使用原版配方、技能、材料和预订检查。
    internal static class SandGolemUnfinishedWorkPatches
    {
        //随半成品存档的共享标记，不保留已经消散的沙傀引用。
        private sealed class SharedWork { public bool enabled; }
        private static readonly ConditionalWeakTable<UnfinishedThing, SharedWork> Shared = new();
        [ThreadStatic] private static Pawn candidate;

        //普通半成品不参与共享接力。
        private static bool IsShared(UnfinishedThing item)
            => item != null && Shared.TryGetValue(item, out SharedWork state) && state.enabled;

        //创建时记录共享属性并清除制作者绑定。
        [HarmonyPatch(typeof(UnfinishedThing), nameof(UnfinishedThing.Creator), MethodType.Setter)]
        private static class CreatorSetter
        {
            //保留原版名称缓存，实际制作者不写入存档。
            private static void Postfix(UnfinishedThing __instance, Pawn value, ref Pawn ___creatorInt,
                ref string ___creatorName)
            {
                if (!SandGolemUtility.IsSandGolem(value)) return;
                Shared.GetOrCreateValue(__instance).enabled = true;
                ___creatorInt = null;
                ___creatorName = "无绑定制作者";
            }
        }

        //只有原版派工检查期间才把当前候选视为制作者，不转移永久归属。
        [HarmonyPatch(typeof(UnfinishedThing), nameof(UnfinishedThing.Creator), MethodType.Getter)]
        private static class CreatorGetter
        {
            //原版查找、续作和技能检查使用同一候选人物。
            private static void Postfix(UnfinishedThing __instance, ref Pawn __result)
            {
                if (IsShared(__instance)) __result = candidate;
            }
        }

        //共享工单不因没有固定制作者而丢失半成品关联。
        [HarmonyPatch(typeof(Bill_ProductionWithUft), nameof(Bill_ProductionWithUft.BoundWorker), MethodType.Getter)]
        private static class BoundWorker
        {
            //工单界面显示无绑定者，派工时允许当前符合条件的候选接手。
            private static bool Prefix(Bill_ProductionWithUft __instance, ref Pawn __result)
            {
                if (!IsShared(__instance.BoundUft)) return true;
                __result = candidate;
                return false;
            }
        }

        //共享标记随物品保存，消散和读档都不会重新绑定沙傀。
        [HarmonyPatch(typeof(UnfinishedThing), nameof(UnfinishedThing.ExposeData))]
        private static class SaveSharedWork
        {
            //复用物品自身的存档节点。
            private static void Postfix(UnfinishedThing __instance)
                => Scribe_Values.Look(ref Shared.GetOrCreateValue(__instance).enabled, "ningshaSharedWork", false);
        }

        //把候选限定在一次派工调用内，允许嵌套检查并在异常时还原。
        [HarmonyPatch(typeof(WorkGiver_DoBill), nameof(WorkGiver_DoBill.JobOnThing))]
        private static class CandidateScope
        {
            //保存外层候选，原版仍负责完整工作资格判断。
            private static void Prefix(Pawn pawn, out Pawn __state)
            {
                __state = candidate;
                candidate = pawn;
            }

            //不吞掉原版异常，只清理本次查询状态。
            private static Exception Finalizer(Exception __exception, Pawn __state)
            {
                candidate = __state;
                return __exception;
            }
        }
    }
}
