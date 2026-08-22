using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Buildings;
using NingshaRaceLib.DesertPit.Generation.Caves;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Landmarks;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DesertPit.Generation.Steps
{
    //类职责：在沙漠巨坑主洞室放置返回地表的洞穴出口并绑定原入口。
    public class GenStep_DesertPitExit : GenStep
    {
        //属性职责：提供当前生成步骤的稳定随机种子片段。
        public override int SeedPart => 914027333;

        //函数职责：清理主洞室安全区、生成 CaveExit 并设置玩家进入点。
        public override void Generate(Map map, GenStepParams parms)
        {
            DesertPitGenUtility.SetGenerationStatus("洞穴出口");
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            IntVec3 exitCell = FindExitCell(map, data);
            DesertPitGenUtility.ClearSafeArea(map, exitCell, 4.5f);

            PocketMapExit exit = GenSpawn.Spawn(ThingMaker.MakeThing(DefOfRefs.NingshaRace_DesertPitCaveExit), exitCell, map) as PocketMapExit;
            MapPortal portal = PocketMapUtility.currentlyGeneratingPortal;
            if (portal != null)
            {
                portal.exit = exit;
            }

            MapGenerator.PlayerStartSpot = exitCell;
        }

        //函数职责：优先在主洞室中心附近寻找可站立出口位置。
        private static IntVec3 FindExitCell(Map map, DesertPitLayoutData data)
        {
            IntVec3 result;
            if (CellFinder.TryFindRandomCellNear(data.MainCenter, map, 5, (IntVec3 cell) => cell.Standable(map), out result))
            {
                return result;
            }

            if (CellFinder.TryFindRandomCell(map, (IntVec3 cell) => DesertPitGenUtility.IsCave(cell) && cell.Standable(map), out result))
            {
                return result;
            }

            return data.MainCenter;
        }
    }
}
