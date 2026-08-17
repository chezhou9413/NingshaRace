using System.Collections.Generic;
using NingshaRaceLib.Core.Defs;
using RimWorld;
using Verse;

namespace NingshaRaceLib.GiantTomb.Content.Config
{
    //类职责：声明一个普通Pawn威胁或一座独立洞穴蚁群的生成参数。
    public sealed class NingshaGiantTombThreatSpawn
    {
        public PawnKindDef pawnKind;
        public int count = 1;
        public FactionDef factionDef;
        public MentalStateDef permanentMentalState;
        public MutantDef mutantDef;
        public ThingDef antNestDef;
        public float scale;

        public bool IsAntColony => antNestDef != null;

        //函数职责：报告互斥威胁类型、数量、阵营和蚁群规模中的配置错误。
        public IEnumerable<string> ConfigErrors(string owner)
        {
            if ((pawnKind != null) == (antNestDef != null))
            {
                yield return owner + ": pawnKind与antNestDef必须且只能配置一个";
            }
            if (count < 1)
            {
                yield return owner + ": count必须大于零";
            }
            if (IsAntColony)
            {
                if (scale <= 0f)
                {
                    yield return owner + ": 蚁群scale必须大于零";
                }
                if (factionDef != null || permanentMentalState != null || mutantDef != null)
                {
                    yield return owner + ": 蚁群不能配置Pawn专用字段";
                }
            }
            else if (factionDef == null)
            {
                yield return owner + ": 普通Pawn威胁必须配置factionDef";
            }
            else if (mutantDef != null && mutantDef != DefOfRefs.NingshaRace_ErosionBodyMutant)
            {
                yield return owner + ": mutantDef只支持NingshaRace_ErosionBodyMutant";
            }
        }
    }
}
