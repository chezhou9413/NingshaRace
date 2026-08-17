using System;
using System.Collections.Generic;
using System.Linq;
using NingshaRaceLib.GiantTomb.Content.Config;
using RimWorld;
using Verse;

namespace NingshaRaceLib.GiantTomb.Content.Generation
{
    //类职责：按XML奖励池抽取资源、解析当地石砖、拆分堆叠并分散生成。
    internal static class GiantTombRewardSpawner
    {
        //函数职责：执行一个房间的完整奖励抽取与生成流程。
        public static void SpawnRoomRewards(Map map, GiantTombContentCellPool cells, NingshaGiantTombRoomContentDef content)
        {
            SpawnRewards(map, cells, content.rewards, content.rewardPickCount, content.rewardWithReplacement, content.defName);
        }

        //函数职责：按给定抽取规则生成可供房间和石棺共同使用的奖励结果。
        public static List<Thing> MakeRewards(Map map, IList<NingshaWeightedThingEntry> rewards, int pickCount, bool withReplacement, string owner)
        {
            List<NingshaWeightedThingEntry> pool = new List<NingshaWeightedThingEntry>(rewards);
            List<Thing> result = new List<Thing>();
            for (int pickIndex = 0; pickIndex < pickCount; pickIndex++)
            {
                NingshaWeightedThingEntry entry = GiantTombWeightedUtility.Pick(pool, item => item.selectionWeight);
                if (!withReplacement)
                {
                    pool.Remove(entry);
                }
                NingshaWeightedCount quantity = GiantTombWeightedUtility.Pick(entry.quantities, item => item.weight);
                ThingDef def = ResolveThingDef(map, entry, owner);
                int remaining = quantity.count;
                while (remaining > 0)
                {
                    Thing thing = ThingMaker.MakeThing(def);
                    thing.stackCount = Math.Min(remaining, def.stackLimit);
                    SetWeaponQuality(thing);
                    result.Add(thing);
                    remaining -= thing.stackCount;
                }
            }
            return result;
        }

        //函数职责：把奖励实例尽量分散到房间空格，紧凑房间空间耗尽后允许多个物品堆共享落点。
        private static void SpawnRewards(Map map, GiantTombContentCellPool cells, IList<NingshaWeightedThingEntry> rewards, int pickCount, bool withReplacement, string owner)
        {
            List<Thing> things = MakeRewards(map, rewards, pickCount, withReplacement, owner);
            for (int i = 0; i < things.Count; i++)
            {
                IntVec3 cell = cells.TakeRewardCell("奖励 " + things[i].def.defName);
                GenSpawn.Spawn(things[i], cell, map, Rot4.Random);
            }
        }

        //函数职责：解析普通ThingDef或地图世界格自然岩石对应的石砖Def。
        private static ThingDef ResolveThingDef(Map map, NingshaWeightedThingEntry entry, string owner)
        {
            if (!entry.localStoneBlocks)
            {
                return entry.thingDef;
            }

            ThingDef rock = Find.World.NaturalRockTypesIn(map.Tile).RandomElementWithFallback();
            ThingDef chunk = rock?.building?.mineableThing;
            ThingDefCountClass blocks = chunk?.butcherProducts?.FirstOrDefault(product => product.thingDef != null && product.thingDef.IsStuff);
            if (blocks == null)
            {
                throw new InvalidOperationException(owner + ": 无法从地图世界格解析当地石材砖。tile=" + map.Tile);
            }
            return blocks.thingDef;
        }

        //函数职责：把所有带品质组件的武器固定为普通品质。
        private static void SetWeaponQuality(Thing thing)
        {
            if (!thing.def.IsWeapon)
            {
                return;
            }
            CompQuality quality = thing.TryGetComp<CompQuality>();
            quality?.SetQuality(QualityCategory.Normal, ArtGenerationContext.Outsider);
        }
    }
}
