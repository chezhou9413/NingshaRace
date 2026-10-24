using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

using NingshaRaceLib.PocketMaps.Buildings;

namespace NingshaRaceLib.DesertPit.Generation.Progress
{
    //类职责：在原版进入传送门作业的抵达前摇与最终传送之间插入凝砂口袋地图生成等待阶段。
    [HarmonyPatch(typeof(JobDriver_EnterPortal), "MakeNewToils")]
    internal static class DesertPitEnterJobPatches
    {
        //函数职责：包装原版作业序列，仅为使用凝砂分帧入口基类的传送门插入生成步骤。
        private static void Postfix(JobDriver_EnterPortal __instance, ref IEnumerable<Toil> __result)
        {
            __result = InsertGenerationToil(__instance, __result);
        }

        //函数职责：保留原版移动与九十Tick前摇，并在最终跨图传送前延迟生成目标地图。
        private static IEnumerable<Toil> InsertGenerationToil(JobDriver_EnterPortal driver, IEnumerable<Toil> originalToils)
        {
            Toil pendingToil = null;
            foreach (Toil toil in originalToils)
            {
                if (pendingToil != null)
                {
                    yield return pendingToil;
                }

                pendingToil = toil;
            }

            if (pendingToil == null)
            {
                yield break;
            }

            Building_NingshaPocketMapPortal gate = driver.MapPortal as Building_NingshaPocketMapPortal;
            if (gate != null)
            {
                yield return CreateGenerationToil(driver, gate);
            }

            yield return pendingToil;
        }

        //函数职责：创建等待口袋地图分帧生成完成的作业步骤，并在失败时结束当前进入作业。
        private static Toil CreateGenerationToil(JobDriver_EnterPortal driver, Building_NingshaPocketMapPortal gate)
        {
            Toil toil = ToilMaker.MakeToil("GenerateDesertPitPocketMap");
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.initAction = delegate
            {
                if (gate.PocketMapExists)
                {
                    driver.ReadyForNextToil();
                    return;
                }

                gate.BeginPocketMapGeneration();
            };
            toil.tickAction = delegate
            {
                if (gate.PocketMapExists)
                {
                    driver.ReadyForNextToil();
                    return;
                }

                if (gate.GenerationFailed)
                {
                    driver.EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (!gate.GenerationInProgress)
                {
                    gate.BeginPocketMapGeneration();
                }
            };
            return toil;
        }
    }
}
