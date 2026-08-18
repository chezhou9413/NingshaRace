using System;
using System.Collections.Generic;
using System.Linq;
using NingshaRaceLib.GiantTomb.Layout;
using RimWorld;
using Verse;

namespace NingshaRaceLib.GiantTomb.Generation.Steps
{
    //类职责：在入口走廊尽头放置并绑定返回地表的原版洞穴出口。
    public sealed class GenStep_GiantTombExit : GenStep
    {
        //属性职责：提供地图生成器使用的稳定随机种子片段。
        public override int SeedPart => 197428389;

        //函数职责：寻找完整三格出口占地、生成CaveExit并设置玩家进入点。
        public override void Generate(Map map, GenStepParams parms)
        {
            GiantTombLayoutData data = GiantTombGenUtility.GetLayoutData();
            IntVec3 exitCell = FindExitCell(map, data);
            PocketMapExit exit = GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.CaveExit), exitCell, map, Rot4.North, WipeMode.Vanish) as PocketMapExit;
            if (exit == null)
            {
                throw new InvalidOperationException("巨型墓葬无法生成原版CaveExit");
            }
            MapPortal portal = PocketMapUtility.currentlyGeneratingPortal;
            if (portal == null)
            {
                throw new InvalidOperationException("巨型墓葬生成时缺少当前原版入口");
            }
            portal.exit = exit;
            MapGenerator.PlayerStartSpot = exitCell;
        }

        //函数职责：在入口结构内选择远离唯一连接点且不会覆盖建筑的三乘三中心。
        private static IntVec3 FindExitCell(Map map, GiantTombLayoutData data)
        {
            GiantTombPlacement entrance = data.Entrance;
            List<IntVec3> connectorCells = entrance.Connectors.SelectMany((GiantTombPlacedConnector connector) => connector.Cells).ToList();
            IntVec3 connectorCenter = connectorCells[connectorCells.Count / 2];
            List<IntVec3> candidates = new List<IntVec3>();
            foreach (IntVec3 cell in entrance.Bounds)
            {
                CellRect occupied = GenAdj.OccupiedRect(cell, Rot4.North, ThingDefOf.CaveExit.Size);
                bool valid = true;
                foreach (IntVec3 occupiedCell in occupied)
                {
                    if (!data.Contains(occupiedCell, map) || occupiedCell.GetEdifice(map) != null)
                    {
                        valid = false;
                        break;
                    }
                }
                if (valid) candidates.Add(cell);
            }
            if (candidates.Count == 0)
            {
                throw new InvalidOperationException("巨型墓葬入口模板没有可容纳CaveExit的三乘三区域");
            }
            return candidates.OrderByDescending((IntVec3 cell) => cell.DistanceToSquared(connectorCenter)).First();
        }
    }
}
