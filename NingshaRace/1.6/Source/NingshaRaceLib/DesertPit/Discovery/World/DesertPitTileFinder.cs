using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace NingshaRaceLib.DesertPit.Discovery.World
{
    //类职责：按世界地块邻接距离选择最近沙漠周围的合法巨坑位置。
    internal static class DesertPitTileFinder
    {
        //函数职责：寻找二十格外最近沙漠，并在其五格内选择可到达且未占用的沙漠。
        public static bool TryFind(PlanetTile origin, out PlanetTile result, out string reason)
        {
            result = PlanetTile.Invalid;
            reason = null;
            if (!origin.Valid || origin.LayerDef != PlanetLayerDefOf.Surface)
            {
                reason = "必须在星球地表对应的地图上解读巨坑坐标。";
                return false;
            }
            Dictionary<PlanetTile, int> distances = Distances(origin, int.MaxValue);
            PlanetTile nearest = PlanetTile.Invalid;
            int nearestDistance = int.MaxValue;
            foreach (KeyValuePair<PlanetTile, int> pair in distances)
            {
                if (pair.Value <= 20 || pair.Value > nearestDistance
                    || Find.WorldGrid[pair.Key].PrimaryBiome != BiomeDefOf.Desert)
                    continue;
                if (pair.Value < nearestDistance || !nearest.Valid || pair.Key.tileId < nearest.tileId)
                {
                    nearest = pair.Key;
                    nearestDistance = pair.Value;
                }
            }
            if (!nearest.Valid)
            {
                reason = "当前星球没有距离此处超过20格的沙漠，无法确定巨坑坐标。";
                return false;
            }
            HashSet<PlanetTile> occupied = new HashSet<PlanetTile>(
                Find.WorldObjects.AllWorldObjects.Select(worldObject => worldObject.Tile));
            List<PlanetTile> candidates = new List<PlanetTile>();
            foreach (PlanetTile tile in Distances(nearest, 5).Keys)
            {
                if (distances[tile] > 20 && Find.WorldGrid[tile].PrimaryBiome == BiomeDefOf.Desert
                    && !occupied.Contains(tile) && Find.WorldReachability.CanReach(origin, tile))
                    candidates.Add(tile);
            }
            if (candidates.Count == 0)
            {
                reason = "最近的20格外沙漠周围5格内，没有未占用且远行队可到达的合规沙漠地点。";
                return false;
            }
            result = candidates.RandomElement();
            return true;
        }

        //函数职责：以广度优先搜索计算纯邻接距离，不把绕过海洋的行军路线误作地理距离。
        private static Dictionary<PlanetTile, int> Distances(PlanetTile origin, int limit)
        {
            Dictionary<PlanetTile, int> distances = new Dictionary<PlanetTile, int> { [origin] = 0 };
            Queue<PlanetTile> queue = new Queue<PlanetTile>();
            List<PlanetTile> neighbors = new List<PlanetTile>();
            queue.Enqueue(origin);
            while (queue.Count > 0)
            {
                PlanetTile tile = queue.Dequeue();
                int depth = distances[tile];
                if (depth >= limit)
                    continue;
                Find.WorldGrid.GetTileNeighbors(tile, neighbors);
                foreach (PlanetTile neighbor in neighbors)
                {
                    if (distances.ContainsKey(neighbor))
                        continue;
                    distances.Add(neighbor, depth + 1);
                    queue.Enqueue(neighbor);
                }
            }
            return distances;
        }
    }
}
