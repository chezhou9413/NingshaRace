using System;
using System.Collections.Generic;
using NingshaRaceLib.DesertPit.AntColony.Components;
using NingshaRaceLib.DesertPit.AntColony.Config;
using NingshaRaceLib.DesertPit.Ecology.Habitats;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Buildings
{
    //巢群建筑唯一持有库存，持有物按原版更新，破坏时释放原物品。
    public class Building_AntColonyStructure : Building_AntHabitat, IThingHolderTickable
    {
        private ThingOwner<Thing> contents;
        public int ColonyId;
        public bool FoodOnly;
        public int LastDamageTick = -1;
        public bool ShouldTickContents => true;
        public override float Radius => 0f;
        protected override bool EmitsSpores => false;
        public DefModExtension_AntStructure Settings => def.GetModExtension<DefModExtension_AntStructure>();
        public ThingOwner<Thing> Contents => contents;

        //创建库存容器，堆叠容量由实际建筑接收时检查。
        public Building_AntColonyStructure() => contents = new ThingOwner<Thing>(this);

        //返回实际库存和其下级容器。
        public ThingOwner GetDirectlyHeldThings() => contents;
        public void GetChildHolders(List<IThingHolder> children) => ThingOwnerUtility.AppendThingHoldersFromThings(children, contents);

        //合并现有堆叠后仍有空槽才允许增加新堆叠。
        public int SpaceFor(Thing item)
        {
            int space = Math.Max(0, Settings.capacity - contents.Count) * item.def.stackLimit;
            foreach (Thing stored in contents)
                if (stored.CanStackWith(item)) space += Math.Max(0, stored.def.stackLimit - stored.stackCount);
            return space;
        }

        //转移携带物的实际所有权，同时使营养账目失效。
        public int Receive(Thing item, int count)
        {
            int accepted = contents.TryAddOrTransfer(item, Math.Min(count, SpaceFor(item)));
            if (accepted > 0) Map.GetComponent<MapComponent_DesertPitAntColonies>().NotifyInventoryChanged(ColonyId);
            return accepted;
        }

        //建筑入图时登记，避免周期性全图搜索建筑。
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (ColonyId > 0) map.GetComponent<MapComponent_DesertPitAntColonies>().RegisterStructure(this);
        }

        //受击时间用于维修等待，不对远离巢穴的建筑扩展警报范围。
        public override void PostApplyDamage(DamageInfo info, float totalDamageDealt)
        {
            base.PostApplyDamage(info, totalDamageDealt);
            if (totalDamageDealt > 0) LastDamageTick = Find.TickManager.TicksGame;
        }

        //先释放库存再移除建筑，不能静默吞掉未成功落地的物品。
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (Spawned)
            {
                if (!contents.TryDropAll(Position, Map, ThingPlaceMode.Near))
                    throw new InvalidOperationException("蚁群建筑库存无法释放：" + this);
                Map.GetComponent<MapComponent_DesertPitAntColonies>().UnregisterStructure(this);
            }
            base.Destroy(mode);
        }

        //持有物只深存档一次，巢群中只保存建筑引用。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref contents, "inventory", this);
            Scribe_Values.Look(ref ColonyId, "colonyId");
            Scribe_Values.Look(ref FoodOnly, "foodOnly");
            Scribe_Values.Look(ref LastDamageTick, "lastDamageTick", -1);
        }
    }
}
