using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

using NingshaRaceLib.DesertPit.AntColony.Components;

namespace NingshaRaceLib.DesertPit.AntColony.Jobs
{
    //类职责：让蚁后从实体储藏区取食、回到蚁穴孵化，并请求地图组件结算一次补员。
    public class JobDriver_DesertPitAntReproduce : JobDriver
    {
        private const TargetIndex NestIndex = TargetIndex.A;
        private const TargetIndex FoodIndex = TargetIndex.B;
        private const float FeedingWorkFraction = 0.4f;

        //函数职责：预留当前巢群蚁穴和展示用食物，防止补员期间被其他成员取走。
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(FoodIndex), job, 1, -1, null, errorOnFailed)
                && pawn.Reserve(job.GetTarget(NestIndex), job, 1, -1, null, errorOnFailed);
        }

        //函数职责：依次执行取食和巢内孵化，并在完成时交由地图组件消耗物资和生成成员。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(NestIndex);
            this.FailOnDestroyedOrNull(FoodIndex);

            int feedingTicks = GetFeedingTicks();
            Thing food = job.GetTarget(FoodIndex).Thing;
            float ingestDurationMultiplier = feedingTicks / Mathf.Max(1f, food.def.ingestible.baseIngestTicks);

            yield return Toils_Goto.GotoThing(FoodIndex, PathEndMode.Touch);
            yield return Toils_Ingest.ChewIngestible(pawn, ingestDurationMultiplier, FoodIndex);
            yield return Toils_Goto.GotoThing(NestIndex, PathEndMode.Touch);
            yield return Toils_General.Wait(Mathf.Max(1, job.count - feedingTicks), NestIndex).WithProgressBarToilDelay(NestIndex);
            yield return Toils_General.Do(delegate
            {
                Map.GetComponent<MapComponent_DesertPitAntColonies>().CompleteReproduction(pawn, job.GetTarget(FoodIndex).Thing);
            });
        }

        //函数职责：把补员工作时间划分出自然取食阶段，并保证孵化阶段至少保留一个 Tick。
        private int GetFeedingTicks()
        {
            return Mathf.Clamp(Mathf.RoundToInt(job.count * FeedingWorkFraction), 1, Mathf.Max(1, job.count - 1));
        }
    }
}
