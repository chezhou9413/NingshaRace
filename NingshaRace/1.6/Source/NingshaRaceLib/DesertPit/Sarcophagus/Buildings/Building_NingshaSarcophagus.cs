using System;
using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Sarcophagus.Config;
using NingshaRaceLib.GiantTomb.Content.Generation;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.Sarcophagus.Buildings
{
    //类职责：初始化封闭砂岩石棺内容，并沿用原版开启工作弹出内容后替换为开启模型。
    public sealed class Building_NingshaSarcophagus : Building_Casket
    {
        private bool contentsInitialized;

        private DefModExtension_NingshaSarcophagus Settings => def.GetModExtension<DefModExtension_NingshaSarcophagus>();

        public override int OpenTicks => Settings.openTicks;

        //函数职责：首次生成时一次性填充古老凝砂族尸体和XML奖励。
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad && !contentsInitialized)
            {
                InitializeContents(map);
            }
        }

        //函数职责：保存一次性初始化状态并沿用原版ThingOwner存档。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref contentsInitialized, "ningshaContentsInitialized");
        }

        //函数职责：执行原版弹出动作后在原位置和朝向替换为开启石棺。
        public override void Open()
        {
            Map map = Map;
            IntVec3 position = Position;
            Rot4 rotation = Rotation;
            Faction faction = Faction;
            ThingDef openedDef = Settings.openedThingDef;
            base.Open();
            Destroy(DestroyMode.Vanish);
            Thing opened = ThingMaker.MakeThing(openedDef);
            GenSpawn.Spawn(opened, position, map, rotation);
            if (faction != null)
            {
                opened.SetFaction(faction);
            }
        }

        //函数职责：生成一具固定种类古老尸体和一次无放回随机奖励并收入石棺。
        private void InitializeContents(Map map)
        {
            DefModExtension_NingshaSarcophagus settings = Settings;
            if (settings == null)
            {
                throw new InvalidOperationException(def.defName + ": 缺少DefModExtension_NingshaSarcophagus。");
            }
            NingshaSarcophagusLootDef loot = settings.lootDef;
            Faction corpseFaction = Find.FactionManager.FirstFactionOfDef(loot.corpseFaction);
            if (corpseFaction == null)
            {
                throw new InvalidOperationException(loot.defName + ": 尸体阵营不存在: " + loot.corpseFaction.defName);
            }

            PawnGenerationRequest request = new PawnGenerationRequest(
                loot.corpseKind,
                corpseFaction,
                PawnGenerationContext.NonPlayer,
                map.Tile,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                allowGay: false,
                allowPregnant: false,
                allowFood: false,
                allowAddictions: false,
                worldPawnFactionDoesntMatter: true,
                forceNoIdeo: true,
                forceNoBackstory: true,
                forceDead: true,
                dontGiveWeapon: true,
                forceNoGear: true);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            Corpse corpse = pawn.Corpse;
            if (corpse == null)
            {
                throw new InvalidOperationException(loot.defName + ": 未能生成凝砂族尸体。");
            }
            corpse.Age = Rand.RangeInclusive(loot.corpseAgeYears.min, loot.corpseAgeYears.max) * GenDate.TicksPerYear;
            corpse.GetComp<CompRottable>().RotProgress += corpse.Age;
            AcceptOrThrow(corpse, loot.defName);

            int pickCount = loot.rewardPickCount.RandomInRange;
            List<Thing> rewards = GiantTombRewardSpawner.MakeRewards(map, loot.rewards, pickCount, loot.rewardWithReplacement, loot.defName);
            for (int i = 0; i < rewards.Count; i++)
            {
                AcceptOrThrow(rewards[i], loot.defName);
            }
            contentsInitialized = true;
        }

        //函数职责：把生成内容收入石棺，并在容器拒收时直接报告配置所有者。
        private void AcceptOrThrow(Thing thing, string owner)
        {
            if (!TryAcceptThing(thing, false))
            {
                throw new InvalidOperationException(owner + ": 石棺无法接收内容 " + thing.def.defName);
            }
        }
    }
}
