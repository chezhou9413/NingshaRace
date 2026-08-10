using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

using NingshaRaceLib.DesertPit.Buildings;

namespace NingshaRaceLib.DesertPit.Generation.Progress
{
    //类职责：在原版进入传送门作业的抵达前摇与最终传送之间插入沙漠巨坑生成等待阶段。
    [HarmonyPatch(typeof(JobDriver_EnterPortal), "MakeNewToils")]
    internal static class DesertPitEnterJobPatches
    {
        //函数职责：包装原版作业序列，仅为沙漠巨坑入口在最后一个传送步骤前插入生成步骤。
        private static void Postfix(JobDriver_EnterPortal __instance, ref IEnumerable<Toil> __result)
        {
            __result = InsertGenerationToil(__instance, __result);
        }

        //函数职责：保留原版移动与九十 Tick 前摇，并在最终跨图传送前延迟生成目标地图。
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

            Building_DesertPitGate gate = driver.MapPortal as Building_DesertPitGate;
            if (gate != null)
            {
                yield return CreateGenerationToil(driver, gate);
            }

            yield return pendingToil;
        }

        //函数职责：创建等待口袋地图分帧生成完成的作业步骤，并在失败时结束当前进入作业。
        private static Toil CreateGenerationToil(JobDriver_EnterPortal driver, Building_DesertPitGate gate)
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
