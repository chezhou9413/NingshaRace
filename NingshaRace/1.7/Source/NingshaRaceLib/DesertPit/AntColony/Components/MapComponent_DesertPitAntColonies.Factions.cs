using System.Collections.Generic;
using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //类职责：维护全世界按需扩展的隐藏蚁群阵营，并按地图内巢群顺序稳定分配。
    public partial class MapComponent_DesertPitAntColonies
    {
        //函数职责：取得指定巢群顺序对应的隐藏阵营，不足时按需创建并登记到世界阵营管理器。
        public Faction GetColonyFaction(int colonyIndex)
        {
            if (colonyIndex < 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(colonyIndex), "蚁群顺序不能为负数。");
            }

            List<Faction> pool = GetFactionPool();
            while (pool.Count <= colonyIndex)
            {
                Faction faction = FactionGenerator.NewGeneratedFaction(
                    new FactionGeneratorParms(DefOfRefs.NingshaRace_DesertPitAntColonyFaction, default(IdeoGenerationParms), true));
                Find.FactionManager.Add(faction);
                pool.Add(faction);
            }

            return pool[colonyIndex];
        }

        //函数职责：按创建编号收集现有蚁群阵营，确保不同地图对相同巢群顺序使用同一实例。
        private static List<Faction> GetFactionPool()
        {
            List<Faction> result = new List<Faction>();
            List<Faction> factions = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < factions.Count; i++)
            {
                Faction faction = factions[i];
                if (faction.def == DefOfRefs.NingshaRace_DesertPitAntColonyFaction)
                {
                    result.Add(faction);
                }
            }

            result.Sort(delegate(Faction left, Faction right)
            {
                return left.loadID.CompareTo(right.loadID);
            });
            return result;
        }
    }
}
