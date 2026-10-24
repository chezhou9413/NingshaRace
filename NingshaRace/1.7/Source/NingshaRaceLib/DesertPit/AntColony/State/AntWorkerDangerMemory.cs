using System;
using System.Collections.Generic;
using NingshaRaceLib.DesertPit.AntColony.Config;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.State
{
    //类职责：从巢群死亡记录缓存工蚁避让区域，不受调查队消费标记影响。
    public sealed class AntWorkerDangerMemory
    {
        private readonly List<IntVec3> centers = new List<IntVec3>();
        private int nextRefreshTick;
        private int cachedRecordCount = -1;
        private int cachedPopulationCap = -1;

        //函数职责：共享短期热点缓存，避免每个搬运候选和任务 Tick 重复聚类全部死亡记录。
        public bool Contains(AntColonyState state, DefModExtension_AntColony settings, IntVec3 cell, int ticks)
        {
            if (ticks >= nextRefreshTick || cachedRecordCount != state.DeathRecords.Count
                || cachedPopulationCap != state.Population.RegularAntCap)
                Refresh(state, settings, ticks);
            float radiusSquared = settings.investigationHotspotRadius * settings.investigationHotspotRadius;
            foreach (IntVec3 center in centers)
                if (cell.DistanceToSquared(center) <= radiusSquared) return true;
            return false;
        }

        //函数职责：按最近一天内全部常规蚁死亡聚类，达到调查门槛的区域同时禁止工蚁采集。
        private void Refresh(AntColonyState state, DefModExtension_AntColony settings, int ticks)
        {
            centers.Clear();
            nextRefreshTick = ticks + 250;
            cachedRecordCount = state.DeathRecords.Count;
            cachedPopulationCap = state.Population.RegularAntCap;
            int oldestTick = ticks - settings.investigationLossWindowTicks;
            int threshold = Math.Max(settings.investigationMinimumDeaths,
                Mathf.CeilToInt(cachedPopulationCap * settings.investigationLossFraction));
            float radiusSquared = settings.investigationHotspotRadius * settings.investigationHotspotRadius;
            foreach (AntDeathRecord center in state.DeathRecords)
            {
                if (center.Tick < oldestTick || !center.Position.IsValid) continue;
                int count = 0;
                foreach (AntDeathRecord record in state.DeathRecords)
                {
                    if (record.Tick < oldestTick || !record.Position.IsValid
                        || center.Position.DistanceToSquared(record.Position) > radiusSquared) continue;
                    if (++count < threshold) continue;
                    centers.Add(center.Position);
                    break;
                }
            }
        }
    }
}
