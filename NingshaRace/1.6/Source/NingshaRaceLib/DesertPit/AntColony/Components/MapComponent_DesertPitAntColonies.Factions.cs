using System.Collections.Generic;
using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //类职责：维护全世界可复用的两个隐藏蚁群阵营，并按地图内巢群顺序稳定分配。
    public partial class MapComponent_DesertPitAntColonies
    {
        private const int ReusableFactionCount = 2;

        //函数职责：取得指定巢群顺序对应的隐藏阵营，不足两个时创建并登记到世界阵营管理器。
        public Faction GetColonyFaction(int colonyIndex)
        {
            if (colonyIndex < 0 || colonyIndex >= ReusableFactionCount)
            {
                throw new System.ArgumentOutOfRangeException(nameof(colonyIndex), "沙漠巨坑只允许为两个巢群分配独立阵营。");
            }

            List<Faction> pool = GetFactionPool();
            while (pool.Count < ReusableFactionCount)
            {
                Faction faction = FactionGenerator.NewGeneratedFaction(
                    new FactionGeneratorParms(DefOfRefs.NingshaRace_DesertPitAntColonyFaction, default(IdeoGenerationParms), true));
                Find.FactionManager.Add(faction);
                pool.Add(faction);
            }

            return pool[colonyIndex];
        }

        //函数职责：按创建编号收集现有蚁群阵营，确保不同地图对第一和第二巢群使用相同实例。
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
