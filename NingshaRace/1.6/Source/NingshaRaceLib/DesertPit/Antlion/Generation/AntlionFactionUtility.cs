using NingshaRaceLib.Core.Defs;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.Antlion.Generation
{
    //类职责：复用世界内单个隐藏蚁狮派系，与蚁群派系分离并保持永久敌对。
    internal static class AntlionFactionUtility
    {
        //函数职责：仅在首次自然生成时创建派系，后续地图使用世界中已保存的同一实例。
        public static Faction GetOrCreate()
        {
            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
                if (faction.def == DefOfRefs.NingshaRace_AntlionFaction) return faction;
            Faction created = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(
                DefOfRefs.NingshaRace_AntlionFaction, default(IdeoGenerationParms), true));
            Find.FactionManager.Add(created);
            return created;
        }
    }
}
