using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit
{
    //类职责：处理沙漠巨坑古旧砂陶罐被击碎后的随机资源掉落和低概率眼镜蛇释放。
    public class Building_DesertPitPot : Building
    {
        //字段职责：控制罐子被击碎时释放猎杀眼镜蛇的独立概率。
        private const float CobraChance = 0.1f;

        //字段职责：标记本罐子是否已经结算过击碎奖励，避免重复销毁流程多次掉落。
        private bool rewardsDropped;

        //函数职责：在罐子被击碎结算时生成资源奖励并低概率释放猎杀眼镜蛇。
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Map cachedMap = Map;
            IntVec3 cachedPosition = Position;
            bool shouldDropRewards = mode == DestroyMode.KillFinalize && !rewardsDropped && cachedMap != null;
            base.Destroy(mode);
            if (!shouldDropRewards)
            {
                return;
            }

            rewardsDropped = true;
            DropRewards(cachedMap, cachedPosition);
            TryReleaseCobra(cachedMap, cachedPosition);
        }

        //函数职责：生成二到三个单件随机资源并尝试放置到罐子附近。
        private static void DropRewards(Map map, IntVec3 position)
        {
            int count = Rand.RangeInclusive(2, 3);
            for (int i = 0; i < count; i++)
            {
                ThingDef lootDef = ChooseLootDef();
                Thing loot = ThingMaker.MakeThing(lootDef);
                loot.stackCount = 1;
                if (!GenPlace.TryPlaceThing(loot, position, map, ThingPlaceMode.Near))
                {
                    loot.Destroy();
                }
            }
        }

        //函数职责：按权重从低膨胀资源和零部件池中选择一个掉落物。
        private static ThingDef ChooseLootDef()
        {
            List<KeyValuePair<ThingDef, float>> pool = new List<KeyValuePair<ThingDef, float>>
            {
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("Steel"), 20f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("WoodLog"), 16f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("Silver"), 8f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("Jade"), 5f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("MedicineHerbal"), 5f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("ComponentIndustrial"), 4f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("Plasteel"), 4f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("Uranium"), 3f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("Gold"), 2f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("ComponentSpacer"), 1f)
            };

            return pool.RandomElementByWeight((KeyValuePair<ThingDef, float> entry) => entry.Value).Key;
        }

        //函数职责：按低概率生成一条眼镜蛇并强制进入永久猎杀人类状态。
        private static void TryReleaseCobra(Map map, IntVec3 position)
        {
            if (!Rand.Chance(CobraChance))
            {
                return;
            }

            PawnKindDef cobraKind = DefDatabase<PawnKindDef>.GetNamed("Cobra");
            Pawn cobra = PawnGenerator.GeneratePawn(cobraKind, null);
            if (!GenPlace.TryPlaceThing(cobra, position, map, ThingPlaceMode.Near))
            {
                cobra.Destroy(DestroyMode.Vanish);
                return;
            }

            cobra.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent, null, forced: true);
        }
    }
}
