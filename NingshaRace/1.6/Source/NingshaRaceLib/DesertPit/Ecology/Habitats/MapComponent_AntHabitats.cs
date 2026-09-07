using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Habitats
{
    //类职责：维护本地图少量生态建筑，避免工蚁与植物反复扫描全图寻找作用源。
    public sealed class MapComponent_AntHabitats : MapComponent
    {
        private readonly List<Building_AntHabitat> habitats = new List<Building_AntHabitat>();

        //函数职责：把生态建筑索引绑定到所属地图。
        public MapComponent_AntHabitats(Map map) : base(map) { }

        //函数职责：登记进入地图的生态建筑，不重复加入同一实体。
        public void Register(Building_AntHabitat habitat)
        {
            if (!habitats.Contains(habitat)) habitats.Add(habitat);
        }

        //函数职责：移除离开地图的生态建筑，使旧位置即时失去覆盖。
        public void Unregister(Building_AntHabitat habitat) => habitats.Remove(habitat);

        //函数职责：判断物资所在地是否被至少一座正在运行的驱蚁桩保护。
        public bool IsRepelled(IntVec3 cell)
        {
            for (int i = 0; i < habitats.Count; i++)
                if (habitats[i] is Building_AntRepellent stump && stump.Spawned && stump.Running
                    && cell.DistanceToSquared(stump.Position) <= stump.Radius * stump.Radius) return true;
            return false;
        }

        //函数职责：取覆盖该格的最大菌巢生长倍率，重叠范围不叠乘。
        public float GrowthMultiplierAt(IntVec3 cell)
        {
            float multiplier = 1f;
            for (int i = 0; i < habitats.Count; i++)
                if (habitats[i] is Building_FungalMound mound && mound.Spawned
                    && cell.DistanceToSquared(mound.Position) <= mound.Radius * mound.Radius)
                    multiplier = Mathf.Max(multiplier, mound.Settings.growthMultiplier);
            return multiplier;
        }

        //函数职责：取得本格食用菌采收倍率，多个菌巢重叠时只取最大值。
        public float HarvestMultiplierAt(IntVec3 cell)
        {
            float multiplier = 1f;
            for (int i = 0; i < habitats.Count; i++)
                if (habitats[i] is Building_FungalMound mound && mound.Spawned
                    && cell.DistanceToSquared(mound.Position) <= mound.Radius * mound.Radius)
                    multiplier = Mathf.Max(multiplier, mound.Settings.harvestMultiplier);
            return multiplier;
        }

        //函数职责：报告菌巢栖息地，供采食与普通野生菌群分开维护。
        public bool IsFungalHabitat(IntVec3 cell)
        {
            for (int i = 0; i < habitats.Count; i++)
                if (habitats[i] is Building_FungalMound mound && mound.Spawned
                    && cell.DistanceToSquared(mound.Position) <= mound.Radius * mound.Radius) return true;
            return false;
        }
    }
}
