using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit
{
    //类职责：在沙漠巨坑全洞穴范围内稀疏随机散布从上方照下来的天光裂隙。
    public class GenStep_DesertPitGlow : GenStep
    {
        //属性职责：提供当前生成步骤的稳定随机种子片段。
        public override int SeedPart => 914027335;

        //函数职责：从全洞穴可站立格里随机选择少量位置放置天光裂隙。
        public override void Generate(Map map, GenStepParams parms)
        {
            DesertPitGenUtility.SetGenerationStatus("天光裂隙");
            ThingDef glowDef = DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitGlow");
            List<IntVec3> candidates = CollectGlowCandidates(map, glowDef);
            int count = Mathf.Min(Rand.RangeInclusive(5, 9), candidates.Count);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell = candidates.RandomElement();
                candidates.Remove(cell);
                GenSpawn.Spawn(glowDef, cell, map);
                RemoveNearbyCandidates(candidates, cell, 13f);
            }
        }

        //函数职责：收集全洞穴中可放置天光裂隙的候选格。
        private static List<IntVec3> CollectGlowCandidates(Map map, ThingDef glowDef)
        {
            List<IntVec3> candidates = new List<IntVec3>();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (CanPlaceGlow(map, cell, glowDef))
                {
                    candidates.Add(cell);
                }
            }

            return candidates;
        }

        //函数职责：移除已放置天光附近的候选点，保持光柱稀疏分布。
        private static void RemoveNearbyCandidates(List<IntVec3> candidates, IntVec3 placed, float radius)
        {
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (candidates[i].DistanceTo(placed) < radius)
                {
                    candidates.RemoveAt(i);
                }
            }
        }

        //函数职责：判断洞穴格是否可以放置天光裂隙。
        private static bool CanPlaceGlow(Map map, IntVec3 cell, ThingDef glowDef)
        {
            if (!cell.InBounds(map))
            {
                return false;
            }

            if (!DesertPitGenUtility.IsCave(map, cell) || !cell.Standable(map))
            {
                return false;
            }

            if (cell.GetEdifice(map) != null || cell.GetFirstThing(map, glowDef) != null)
            {
                return false;
            }

            return true;
        }
    }
}
