using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Components;
using NingshaRaceLib.Erosion.Utility;
using NingshaRaceLib.SandGolem.Utility;
using Verse;

namespace NingshaRaceLib.Race.Utility
{
    //统一识别不能被转化为蹒跚怪的凝砂相关生物。
    internal static class NingshaShamblerUtility
    {
        //侵蚀体按身份判断，覆盖其他人形种族的侵蚀体。
        internal static bool IsImmune(Pawn pawn)
            => pawn != null && (pawn.def == DefOfRefs.NingshaRace
                || ErosionPawnUtility.HasErosionBodyIdentity(pawn)
                || SandGolemUtility.IsSandGolem(pawn)
                || pawn.TryGetComp<Comp_DesertPitAntMember>() != null
                || pawn.def.defName == "NingshaRace_Antlion"
                || pawn.def.defName == "NingshaRace_GiantTombMummy");
    }
}
