using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.GiantTomb.Generation;

namespace NingshaRaceLib.AltarMissions.Map
{
    //类职责：向小型遗迹保证放置沙之热、加权武器和一组可清剿的墓葬威胁。
    public sealed class GenStep_AltarSmallRuinsRewards : GenStep
    {
        //属性职责：提供地图生成器使用的稳定随机种子片段。
        public override int SeedPart => 812740139;

        //函数职责：在布局结构内分别生成十至五十香料、一件固定权重武器和二至四具木乃伊。
        public override void Generate(Verse.Map map, GenStepParams parms)
        {
            GiantTombLayoutData data = GiantTombGenUtility.GetLayoutData();
            List<IntVec3> cells = CollectFreeStructureCells(map, data);
            if (cells.Count < 8)
            {
                throw new InvalidOperationException("小型遗迹没有足够的空闲结构格生成奖励与威胁。");
            }
            cells.Shuffle();
            Thing spice = ThingMaker.MakeThing(DefOfRefs.NingshaRace_SandHeat);
            spice.stackCount = Rand.RangeInclusive(10, 50);
            GenSpawn.Spawn(spice, cells[0], map);

            Thing weapon = ThingMaker.MakeThing(RandomWeaponDef());
            CompQuality quality = weapon.TryGetComp<CompQuality>();
            quality?.SetQuality(Rand.Chance(0.7f) ? QualityCategory.Normal : QualityCategory.Good, ArtGenerationContext.Colony);
            GenSpawn.Spawn(weapon, cells[1], map);

            int enemyCount = Rand.RangeInclusive(2, 4);
            for (int i = 0; i < enemyCount; i++)
            {
                Pawn enemy = PawnGenerator.GeneratePawn(DefOfRefs.NingshaRace_GiantTombMummyKind, Faction.OfAncientsHostile);
                GenSpawn.Spawn(enemy, cells[i + 2], map, Rot4.Random);
            }
        }

        //函数职责：收集已经完成模板、内容和岩层填充后仍可放置物品或Pawn的结构格。
        private static List<IntVec3> CollectFreeStructureCells(Verse.Map map, GiantTombLayoutData data)
        {
            List<IntVec3> cells = new List<IntVec3>();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (data.Contains(cell, map) && cell.Standable(map) && cell.GetFirstPawn(map) == null
                    && cell.GetThingList(map).TrueForAll(thing => thing.def.category != ThingCategory.Item))
                {
                    cells.Add(cell);
                }
            }
            return cells;
        }

        //函数职责：按30、25、20、15、10权重选择小型遗迹唯一武器奖励。
        private static ThingDef RandomWeaponDef()
        {
            float value = Rand.Value;
            if (value < 0.30f) return DefOfRefs.NingshaRace_SnakeBellySword;
            if (value < 0.55f) return DefOfRefs.NingshaRace_FlyingNeedle;
            if (value < 0.75f) return DefOfRefs.NingshaRace_SandBottle;
            if (value < 0.90f) return DefOfRefs.NingshaRace_GroundSpikeSummoner;
            return DefOfRefs.NingshaRace_BurialMountainGreatsword;
        }
    }
}
