using RimWorld;
using Verse;

using NingshaRaceLib.GiantTomb.Generation;

namespace NingshaRaceLib.AltarMissions.Map
{
    //类职责：为小型地下遗迹凿出三格宽的南侧入场通道并连接入口模板。
    public sealed class GenStep_AltarSmallRuinsEntry : GenStep
    {
        //属性职责：提供地图生成器使用的稳定随机种子片段。
        public override int SeedPart => 812740127;

        //函数职责：从地图南边缘向入口模板中心清空岩石、铺设粗糙砂岩并设置玩家起点。
        public override void Generate(Verse.Map map, GenStepParams parms)
        {
            GiantTombLayoutData data = GiantTombGenUtility.GetLayoutData();
            int centerX = data.Entrance.Bounds.CenterCell.x;
            int targetZ = data.Entrance.Bounds.minZ;
            TerrainDef floor = DefDatabase<TerrainDef>.GetNamed("Sandstone_Rough");
            for (int z = 0; z <= targetZ; z++)
            {
                for (int x = centerX - 1; x <= centerX + 1; x++)
                {
                    IntVec3 cell = new IntVec3(x, 0, z);
                    if (!cell.InBounds(map))
                    {
                        continue;
                    }
                    Thing edifice = cell.GetEdifice(map);
                    edifice?.Destroy(DestroyMode.Vanish);
                    map.terrainGrid.SetTerrain(cell, floor);
                    map.roofGrid.SetRoof(cell, RoofDefOf.RoofRockThick);
                }
            }
            MapGenerator.PlayerStartSpot = new IntVec3(centerX, 0, 1);
        }
    }
}
