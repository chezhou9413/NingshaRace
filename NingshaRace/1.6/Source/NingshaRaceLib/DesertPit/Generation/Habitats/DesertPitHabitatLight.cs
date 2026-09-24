using System;
using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Lighting;
using NingshaRaceLib.DesertPit.Generation.Resources;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Habitats
{
    //类职责：为每个生态洞室落实固定数量区间的天光裂隙，与坠砂效果独立生成。
    internal static class DesertPitHabitatLight
    {
        //函数职责：在非沼泽生态洞室的岸带和干地生成三到七处真实天光，不受坠砂概率影响。
        public static void Generate(Map map, DesertPitLayoutData data, ThingDef glow)
        {
            foreach (DesertPitHabitat habitat in data.Habitats)
            {
                if (habitat.marsh) continue;
                HashSet<IntVec3> dry = new HashSet<IntVec3>(habitat.floor);
                dry.ExceptWith(habitat.water);
                dry.ExceptWith(data.ProtectedRouteCells);
                List<IntVec3> candidates = new List<IntVec3>(dry);
                candidates.RemoveAll(cell => !DesertPitResourcePlacement.Empty(map, cell));
                HashSet<IntVec3> shore = new HashSet<IntVec3>(DesertPitHabitatShore.Collect(habitat, data));
                int count = Rand.RangeInclusive(3, 7);
                List<IntVec3> cells = SelectSpread(candidates, shore, count, habitat.room.Center);
                foreach (IntVec3 cell in cells) GenSpawn.Spawn(glow, cell, map);
            }
            DesertPitGenerationLighting.Refresh(map);
        }

        //函数职责：优先使用岸带，并以最远间距逐点选择天光位置，使光源覆盖不同岸段。
        private static List<IntVec3> SelectSpread(List<IntVec3> candidates, HashSet<IntVec3> shore, int count, IntVec3 center)
        {
            if (candidates.Count < count)
                throw new InvalidOperationException("生态洞室" + center + "天光落点不足，目标：" + count + "，合法格：" + candidates.Count);
            candidates.Shuffle();
            List<IntVec3> result = new List<IntVec3>();
            while (result.Count < count)
            {
                bool useShore = candidates.Exists(shore.Contains);
                IntVec3 best = IntVec3.Invalid;
                float bestDistance = -1f;
                foreach (IntVec3 candidate in candidates)
                {
                    if (useShore && !shore.Contains(candidate)) continue;
                    float nearest = float.MaxValue;
                    foreach (IntVec3 placed in result)
                        nearest = Math.Min(nearest, candidate.DistanceToSquared(placed));
                    if (nearest > bestDistance) { best = candidate; bestDistance = nearest; }
                }
                result.Add(best);
                candidates.Remove(best);
            }
            return result;
        }
    }
}
