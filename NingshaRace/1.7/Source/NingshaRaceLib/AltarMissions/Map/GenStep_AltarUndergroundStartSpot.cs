using System;
using RimWorld;
using Verse;

using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.AltarMissions.Map
{
    //类职责：在全厚岩顶的祭坛地下任务中设置主洞室入场点，避免原版只接受露天格的起点搜索。
    public sealed class GenStep_AltarUndergroundStartSpot : GenStep
    {
        //属性职责：提供地下任务入场步骤使用的稳定随机种子片段。
        public override int SeedPart => 812740137;

        //函数职责：重建区域并从主洞室安全区选择可站立洞穴格作为玩家入场点。
        public override void Generate(Verse.Map map, GenStepParams parms)
        {
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
            if (!CellFinder.TryFindRandomCellNear(data.MainCenter, map, 5,
                cell => DesertPitGenUtility.IsCave(map, cell) && cell.Standable(map)
                    && !DesertPitGenUtility.IsWaterLikeTerrain(cell.GetTerrain(map)),
                out IntVec3 start, 300))
            {
                throw new InvalidOperationException("祭坛地下任务主洞室没有可用的玩家入场点。");
            }

            MapGenerator.PlayerStartSpot = start;
            map.GetComponent<MissionMapComponent>().SetUndergroundEntryCell(start);
        }
    }
}
