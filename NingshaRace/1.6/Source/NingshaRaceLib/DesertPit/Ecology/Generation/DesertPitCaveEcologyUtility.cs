using System.Collections.Generic;
using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Ecology.Config;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DesertPit.Ecology.Generation
{
    //类职责：提供原版虫巢与野生洞穴动物共享的候选格验证、物种抽取和场景保留逻辑。
    public static class DesertPitCaveEcologyUtility
    {
        //函数职责：验证格子是否适合作为原版虫巢中心，并执行入口、蚁穴与生成保留区避让。
        public static bool CanPlaceHiveAt(Map map, DesertPitLayoutData data, DefModExtension_DesertPitFauna settings, IntVec3 cell)
        {
            if (!IsClearNaturalCaveCell(map, cell) || cell.DistanceTo(data.MainCenter) < settings.entranceAvoidRadius || NearThingDef(map, cell, ThingDefOf.CaveExit, settings.entranceAvoidRadius))
            {
                return false;
            }

            if (data.ProtectedRouteCells.Contains(cell) || data.ReservedSceneCells.Contains(cell) || NearThingDef(map, cell, DefOfRefs.NingshaRace_DesertPitAntNest, settings.antNestAvoidRadius))
            {
                return false;
            }

            return true;
        }

        //函数职责：验证格子是否适合生成一只野生动物，并避开入口、全部保留场景和现有实体。
        public static bool CanPlaceAnimalAt(Map map, DesertPitLayoutData data, DefModExtension_DesertPitFauna settings, IntVec3 cell)
        {
            if (!IsClearNaturalCaveCell(map, cell) || cell.DistanceTo(data.MainCenter) < settings.animalEntranceAvoidRadius || NearThingDef(map, cell, ThingDefOf.CaveExit, settings.animalEntranceAvoidRadius))
            {
                return false;
            }

            return !data.ProtectedRouteCells.Contains(cell) && !data.ReservedSceneCells.Contains(cell);
        }

        //函数职责：计算虫巢候选权重，使虫巢优先落在远离主洞的侧洞或小洞室。
        public static float HivePlacementWeight(Map map, DesertPitLayoutData data, IntVec3 cell)
        {
            float weight = 1f;
            for (int i = 0; i < data.SmallRooms.Count; i++)
            {
                float distance = cell.DistanceTo(data.SmallRooms[i]);
                if (distance <= 7f)
                {
                    weight += 12f;
                }
                else if (distance <= 14f)
                {
                    weight += 4f;
                }
            }

            if (DesertPitGenUtility.NearCaveEdge(map, cell, 5))
            {
                weight += 2f;
            }

            return weight;
        }

        //函数职责：按配置生成完整动物种类序列，保证必有物种、总量范围和单物种上限。
        public static List<PawnKindDef> BuildAnimalKinds(DefModExtension_DesertPitFauna settings)
        {
            int targetCount = settings.animalCountRange.RandomInRange;
            List<PawnKindDef> result = new List<PawnKindDef>(settings.guaranteedAnimals);
            Dictionary<PawnKindDef, int> counts = CountKinds(result);
            while (result.Count < targetCount)
            {
                PawnKindDef selected = SelectWeightedKind(settings, counts);
                if (selected == null)
                {
                    throw new System.InvalidOperationException("沙漠巨坑洞穴动物池无法满足配置的数量和单物种上限。");
                }

                result.Add(selected);
                counts[selected] = GetCount(counts, selected) + 1;
            }

            result.Shuffle();
            return result;
        }

        //函数职责：把指定中心周围的场景半径登记为后续生成步骤不可占用的区域。
        public static void ReserveScene(Map map, DesertPitLayoutData data, IntVec3 center, float radius)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (cell.InBounds(map))
                {
                    data.ReservedSceneCells.Add(cell);
                }
            }
        }

        //函数职责：判断格子是否是自然岩顶覆盖、干燥、可站立且没有建筑、物品、植物或 Pawn 的洞穴地面。
        private static bool IsClearNaturalCaveCell(Map map, IntVec3 cell)
        {
            RoofDef roof = cell.InBounds(map) ? cell.GetRoof(map) : null;
            if (!cell.InBounds(map) || !DesertPitGenUtility.IsCave(map, cell) || !cell.Standable(map) || roof == null || !roof.isNatural)
            {
                return false;
            }

            if (DesertPitGenUtility.IsWaterLikeTerrain(cell.GetTerrain(map)) || cell.GetEdifice(map) != null || cell.GetPlant(map) != null || cell.GetFirstPawn(map) != null)
            {
                return false;
            }

            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                ThingCategory category = things[i].def.category;
                if (category == ThingCategory.Item || category == ThingCategory.Building || category == ThingCategory.Plant || category == ThingCategory.Pawn)
                {
                    return false;
                }
            }

            return true;
        }

        //函数职责：判断指定格子是否处于某类地图实体的避让半径内。
        private static bool NearThingDef(Map map, IntVec3 cell, ThingDef def, float radius)
        {
            List<Thing> things = map.listerThings.ThingsOfDef(def);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing.Spawned && cell.DistanceTo(thing.Position) < radius)
                {
                    return true;
                }
            }

            return false;
        }

        //函数职责：统计当前动物序列中每种 PawnKindDef 的数量。
        private static Dictionary<PawnKindDef, int> CountKinds(List<PawnKindDef> kinds)
        {
            Dictionary<PawnKindDef, int> result = new Dictionary<PawnKindDef, int>();
            for (int i = 0; i < kinds.Count; i++)
            {
                PawnKindDef kind = kinds[i];
                result[kind] = GetCount(result, kind) + 1;
            }

            return result;
        }

        //函数职责：按相对权重抽取一只尚未达到配置上限的洞穴动物种类。
        private static PawnKindDef SelectWeightedKind(DefModExtension_DesertPitFauna settings, Dictionary<PawnKindDef, int> counts)
        {
            float totalWeight = 0f;
            for (int i = 0; i < settings.animalPool.Count; i++)
            {
                DesertPitAnimalWeight entry = settings.animalPool[i];
                if (CanSelectKind(settings, counts, entry.pawnKind))
                {
                    totalWeight += entry.weight;
                }
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float value = Rand.Range(0f, totalWeight);
            PawnKindDef lastSelectable = null;
            for (int i = 0; i < settings.animalPool.Count; i++)
            {
                DesertPitAnimalWeight entry = settings.animalPool[i];
                if (!CanSelectKind(settings, counts, entry.pawnKind))
                {
                    continue;
                }

                lastSelectable = entry.pawnKind;
                value -= entry.weight;
                if (value <= 0f)
                {
                    return entry.pawnKind;
                }
            }

            return lastSelectable;
        }

        //函数职责：检查候选种类是否有效且没有超过配置的单物种数量上限。
        private static bool CanSelectKind(DefModExtension_DesertPitFauna settings, Dictionary<PawnKindDef, int> counts, PawnKindDef kind)
        {
            return kind != null && (kind != settings.cappedAnimal || GetCount(counts, kind) < settings.cappedAnimalCount);
        }

        //函数职责：安全读取种类数量字典中指定 PawnKindDef 的当前数量。
        private static int GetCount(Dictionary<PawnKindDef, int> counts, PawnKindDef kind)
        {
            int value;
            return kind != null && counts.TryGetValue(kind, out value) ? value : 0;
        }
    }
}
