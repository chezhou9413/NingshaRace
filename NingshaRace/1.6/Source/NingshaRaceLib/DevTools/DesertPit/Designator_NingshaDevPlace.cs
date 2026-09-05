using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Buildings;
using NingshaRaceLib.DesertPit.Generation.Caves;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Landmarks;
using NingshaRaceLib.DesertPit.Generation.Steps;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DevTools.DesertPit
{
    //类职责：提供开发者模式下直接刷地形、建筑、植物和罐子的地图摆放 Designator。
    public class Designator_NingshaDevPlace : Designator_Place
    {
        //字段职责：记录当前工具正在摆放的 Def。
        private readonly BuildableDef placingDef;

        //字段职责：复用占地格列表，避免每帧绘制缺图物件预览时产生额外集合分配。
        private static readonly List<IntVec3> OccupiedPreviewCells = new List<IntVec3>();

        //属性职责：向原版摆放预览系统提供当前摆放 Def。
        public override BuildableDef PlacingDef => placingDef;

        //属性职责：开发者直接摆放不使用风格覆盖。
        public override ThingStyleDef ThingStyleDefForPreview => null;

        //属性职责：开发者直接摆放不使用材料。
        public override ThingDef StuffDef => null;

        //构造函数职责：初始化直接摆放工具的显示文本、图标和目标 Def。
        public Designator_NingshaDevPlace(BuildableDef placingDef)
        {
            this.placingDef = placingDef;
            defaultLabel = "摆放 " + placingDef.LabelCap;
            defaultDesc = "开发者工具：直接摆放 " + placingDef.LabelCap + "。";
            icon = placingDef.uiIcon;
            iconAngle = placingDef.uiIconAngle;
            iconOffset = placingDef.uiIconOffset;
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_PlaceBuilding;
        }

        //函数职责：判断当前格是否允许摆放目标地形或物件。
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            Map map = Find.CurrentMap;
            if (map == null || !loc.InBounds(map))
            {
                return "目标格不在当前地图内";
            }

            if (placingDef is TerrainDef)
            {
                return true;
            }

            ThingDef thingDef = (ThingDef)placingDef;
            if (!GenSpawn.CanSpawnAt(thingDef, loc, map, placingRot))
            {
                return "目标格无法摆放该物件";
            }

            return true;
        }

        //函数职责：在指定格直接刷入目标地形或物件。
        public override void DesignateSingleCell(IntVec3 loc)
        {
            Map map = Find.CurrentMap;
            if (map == null || !loc.InBounds(map))
            {
                return;
            }

            if (placingDef is TerrainDef terrainDef)
            {
                PlaceTerrain(map, loc, terrainDef);
                return;
            }

            PlaceThing(map, loc, (ThingDef)placingDef);
        }

        //函数职责：只允许本工具在开发者模式和地图存在时保持选中。
        public override bool CanRemainSelected()
        {
            return Prefs.DevMode && Find.CurrentMap != null;
        }

        //函数职责：绘制当前条目的地图预览，并对无普通贴图物件使用占地格高亮避免原版 Ghost 空引用。
        protected override void DrawGhost(Color ghostCol)
        {
            if (placingDef is TerrainDef)
            {
                GenDraw.DrawTargetHighlightWithLayer(Verse.UI.MouseCell(), AltitudeLayer.Terrain);
                return;
            }

            ThingDef thingDef = (ThingDef)placingDef;
            if (thingDef.graphicData != null && thingDef.graphic != null)
            {
                GhostDrawer.DrawGhostThing(Verse.UI.MouseCell(), placingRot, thingDef, thingDef.graphic, ghostCol, AltitudeLayer.Blueprint, null, drawPlaceWorkers: true, StuffDef);
                return;
            }

            DrawOccupiedCellsPreview(thingDef);
        }

        //函数职责：为无贴图或不可见物件绘制当前占地区域边框。
        private void DrawOccupiedCellsPreview(ThingDef thingDef)
        {
            OccupiedPreviewCells.Clear();
            CellRect occupiedRect = GenAdj.OccupiedRect(Verse.UI.MouseCell(), placingRot, thingDef.Size);
            foreach (IntVec3 cell in occupiedRect.Cells)
            {
                OccupiedPreviewCells.Add(cell);
            }

            GenDraw.DrawFieldEdges(OccupiedPreviewCells, CanDesignateCell(Verse.UI.MouseCell()).Accepted ? Designator_Place.CanPlaceColor : Designator_Place.CannotPlaceColor);
        }

        //函数职责：直接设置指定格的地形。
        private static void PlaceTerrain(Map map, IntVec3 loc, TerrainDef terrainDef)
        {
            map.terrainGrid.RemoveTempTerrain(loc);
            if (terrainDef.isFoundation)
            {
                map.terrainGrid.SetFoundation(loc, terrainDef);
            }
            else
            {
                map.terrainGrid.SetTerrain(loc, terrainDef);
            }
        }

        //函数职责：直接生成指定 ThingDef，并让植物以完整生长状态出现。
        private void PlaceThing(Map map, IntVec3 loc, ThingDef thingDef)
        {
            Thing thing = ThingMaker.MakeThing(thingDef);
            if (thing is Plant plant)
            {
                plant.Growth = 1f;
            }

            GenSpawn.Spawn(thing, loc, map, placingRot);
        }
    }
}
