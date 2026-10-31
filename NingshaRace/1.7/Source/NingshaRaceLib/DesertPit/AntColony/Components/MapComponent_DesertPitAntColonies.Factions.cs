using RimWorld;
using Verse;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //每次生成都创建独立隐藏阵营，不跨地图复用。
    public partial class MapComponent_DesertPitAntColonies
    {
        //阵营生成器建立与全部现有阵营的关系，包括其他巢群。
        public Faction CreateColonyFaction()
        {
            Faction faction = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(
                DefOfRefs.NingshaRace_DesertPitAntColonyFaction, default(IdeoGenerationParms), true));
            Find.FactionManager.Add(faction);
            return faction;
        }
    }
}
