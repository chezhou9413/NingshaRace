using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Generation
{
    //类职责：按巢群实际规模生成初始口粮和有限贵重物资。
    public static partial class DesertPitAntSceneUtility
    {
        //函数职责：按完整初始巢群的两天消耗配置分堆口粮，并放置一至两堆随机贵重品。
        private static void SpawnInitialStock(Map map, List<IntVec3> storageCells, List<Pawn> members)
        {
            float nutrition = 0f;
            foreach (Pawn member in members)
                nutrition += member.needs.food.FoodFallPerTickAssumingCategory(HungerCategory.Fed) * GenDate.TicksPerDay * 2f;
            int remaining = Mathf.CeilToInt(nutrition / ThingDefOf.InsectJelly.GetStatValueAbstract(StatDefOf.Nutrition));
            int foodCells = 0;
            while (remaining > 0)
            {
                if (foodCells >= storageCells.Count - 2) throw new System.InvalidOperationException("蚁巢储藏格不足以容纳初始两天口粮。");
                Thing jelly = ThingMaker.MakeThing(ThingDefOf.InsectJelly);
                jelly.stackCount = System.Math.Min(remaining, jelly.def.stackLimit);
                GenSpawn.Spawn(jelly, storageCells[foodCells++], map);
                remaining -= jelly.stackCount;
            }

            List<ThingDef> valuables = new List<ThingDef>
            {
                ThingDefOf.Silver,
                ThingDefOf.Gold,
                ThingDefOf.Jade,
                ThingDefOf.ComponentIndustrial,
                ThingDefOf.ComponentSpacer
            };
            int pileCount = Rand.RangeInclusive(1, 2);
            for (int i = 0; i < pileCount; i++)
            {
                ThingDef def = valuables.RandomElement();
                valuables.Remove(def);
                Thing thing = ThingMaker.MakeThing(def);
                thing.stackCount = InitialValuableCount(def);
                GenSpawn.Spawn(thing, storageCells[storageCells.Count - 2 + i], map);
            }
        }

        //函数职责：按贵重品类型给出适合作为小型巢穴战利品的初始堆叠数量。
        private static int InitialValuableCount(ThingDef def)
        {
            if (def == ThingDefOf.Silver)
            {
                return Rand.RangeInclusive(50, 120);
            }

            if (def == ThingDefOf.ComponentIndustrial)
            {
                return Rand.RangeInclusive(2, 5);
            }

            if (def == ThingDefOf.ComponentSpacer)
            {
                return Rand.RangeInclusive(1, 2);
            }

            return Rand.RangeInclusive(10, 25);
        }
    }
}
