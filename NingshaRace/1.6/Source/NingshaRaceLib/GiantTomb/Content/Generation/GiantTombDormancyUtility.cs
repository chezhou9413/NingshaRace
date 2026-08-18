using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NingshaRaceLib.GiantTomb.Content.Generation
{
    //类职责：复用原版遗迹休眠威胁机制，使每个墓葬房间的生物在被发现、受击或听见战斗前保持沉睡。
    internal static class GiantTombDormancyUtility
    {
        //函数职责：按派系建立原版休眠领主并冻结全部房间敌人，解除迷雾时由原版通知链统一唤醒。
        public static void PutToSleep(Map map, List<Pawn> pawns)
        {
            if (pawns.Count == 0)
            {
                return;
            }
            Dictionary<Faction, List<Pawn>> groups = new Dictionary<Faction, List<Pawn>>();
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn.Faction == null)
                {
                    throw new InvalidOperationException("墓葬休眠敌人缺少派系: " + pawn.LabelShort);
                }
                List<Pawn> group;
                if (!groups.TryGetValue(pawn.Faction, out group))
                {
                    group = new List<Pawn>();
                    groups.Add(pawn.Faction, group);
                }
                group.Add(pawn);
            }
            foreach (KeyValuePair<Faction, List<Pawn>> pair in groups)
            {
                LordJob_SleepThenAssaultColony lordJob = new LordJob_SleepThenAssaultColony(pair.Key, false)
                {
                    awakeOnClamor = true,
                    wakeOnPawnUnfogged = true
                };
                Lord lord = LordMaker.MakeNewLord(pair.Key, lordJob, map);
                lord.AddPawns(pair.Value);
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    StartDormantSleep(pair.Value[i]);
                }
            }
        }

        //函数职责：让没有原版休眠组件的动物、人形与蚁群统一进入强制睡眠并退出活动Pawn索引。
        private static void StartDormantSleep(Pawn pawn)
        {
            Job sleepJob = JobMaker.MakeJob(JobDefOf.LayDown, pawn.Position);
            sleepJob.forceSleep = true;
            pawn.jobs.StartJob(sleepJob, JobCondition.InterruptForced);
            pawn.mindState.Active = false;
        }
    }
}
