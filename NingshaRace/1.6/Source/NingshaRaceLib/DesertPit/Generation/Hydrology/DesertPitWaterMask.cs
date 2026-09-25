using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Hydrology
{
    //类职责：整理噪声水面，只保留靠近指定河心或潭心的四邻接连通部分。
    internal static class DesertPitWaterMask
    {
        //函数职责：从中心附近的水格遍历连续水面，去除边缘噪声和保护区裁切产生的孤立碎片。
        public static void KeepConnected(HashSet<IntVec3> water, IntVec3 center)
        {
            if (water.Count == 0) return;
            IntVec3 seed = center;
            if (!water.Contains(seed))
            {
                float nearest = float.MaxValue;
                foreach (IntVec3 cell in water)
                {
                    float distance = cell.DistanceToSquared(center);
                    if (distance < nearest || (distance == nearest
                        && (cell.x < seed.x || (cell.x == seed.x && cell.z < seed.z))))
                    {
                        nearest = distance;
                        seed = cell;
                    }
                }
            }

            HashSet<IntVec3> connected = new HashSet<IntVec3> { seed };
            Queue<IntVec3> pending = new Queue<IntVec3>();
            pending.Enqueue(seed);
            while (pending.Count > 0)
            {
                IntVec3 cell = pending.Dequeue();
                foreach (IntVec3 direction in GenAdj.CardinalDirections)
                {
                    IntVec3 next = cell + direction;
                    if (water.Contains(next) && connected.Add(next)) pending.Enqueue(next);
                }
            }
            water.IntersectWith(connected);
        }
    }
}
