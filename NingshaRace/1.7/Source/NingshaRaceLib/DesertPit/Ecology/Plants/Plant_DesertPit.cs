using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Plants
{
    //单株洞穴植物把贴图根部固定在所在格中心，成长时按相同比例缩放偏移。
    public class Plant_DesertPit : Plant
    {
        private static readonly Color32[] WindColors = new Color32[4];

        //地面被铺成地板后停止生长，其他温度、光照条件仍由原版计算。
        public override float GrowthRate => CanGrowOn(Position.GetTerrain(Map)) ? base.GrowthRate : 0f;

        //沙地、沙砾、土地及可打磨的粗糙岩面适合洞穴菌类扎根。
        public static bool CanGrowOn(TerrainDef terrain)
        {
            return !terrain.IsWater && !terrain.layerable
                && (terrain == TerrainDefOf.Sand || terrain == TerrainDefOf.SoftSand
                    || terrain == TerrainDefOf.Gravel || terrain == TerrainDefOf.FungalGravel
                    || terrain.IsSoil || terrain.smoothedTerrain != null);
        }

        //保留原版风动、图集、雪层和阴影，只调整单株的落地锚点。
        public override void Print(SectionLayer layer)
        {
            if (LifeStage == PlantLifeStage.Sowing)
            {
                base.Print(layer);
                return;
            }

            Graphic graphic = Graphic;
            float scale = def.plant.visualSizeRange.LerpThroughRange(Growth);
            Vector2 size = def.graphicData.drawSize * scale;
            Vector3 root = this.TrueCenter();
            Vector3 center = root + graphic.DrawOffset(Rot4.North) * scale;
            PlantUtility.SetWindExposureColors(WindColors, this);
            PrintPlantPlane(layer, graphic.MatSingleFor(this), center, size);
            if (Position.GetSnowDepth(Map) > 0.8f && SnowOverlayGraphic != null)
            {
                PrintPlantPlane(layer, SnowOverlayGraphic.MatSingleFor(this),
                    center.WithYOffset(0.0003658537f), size);
            }

            ShadowData shadow = def.graphicData.shadowData;
            if (shadow != null)
            {
                Vector3 shadowCenter = root + shadow.offset * scale;
                shadowCenter.y -= 0.03658537f;
                Printer_Shadow.PrintShadow(layer, shadowCenter, shadow.volume * scale, Rot4.North);
            }
        }

        //使用原版植物图集和风动顶点参数，不翻转带有偏心根部的贴图。
        private void PrintPlantPlane(SectionLayer layer, Material material, Vector3 center, Vector2 size)
        {
            Graphic.TryGetTextureAtlasReplacementInfo(material, def.category.ToAtlasGroup(), false,
                vertexColors: false, out material, out var uvs, out _);
            Printer_Plane.PrintPlane(layer, center, size, material, 0f, false, uvs, WindColors,
                0.1f, this.HashOffset() % 1024);
        }
    }
}
