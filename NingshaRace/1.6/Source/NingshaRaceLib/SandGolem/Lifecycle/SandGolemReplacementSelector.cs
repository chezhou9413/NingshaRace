using System;
using NingshaRaceLib.Core.Defs;
using RimWorld;
using Verse;

namespace NingshaRaceLib.SandGolem.Lifecycle
{
    //类职责：为被事件误选的沙傀寻找战斗力最接近的普通机械体。
    internal static class SandGolemReplacementSelector
    {
        //函数职责：按原版机械集群条件和随机候选标记选取替代种类，无候选时使用核心镰刀机械体。
        public static PawnKindDef Select(PawnKindDef original)
        {
            PawnKindDef best = null;
            float bestDifference = float.PositiveInfinity;
            foreach (PawnKindDef candidate in DefDatabase<PawnKindDef>.AllDefsListForReading)
            {
                if (candidate.race == DefOfRefs.NingshaRace_SandGolem
                    || !candidate.appearsRandomlyInCombatGroups || candidate.maxPerGroup <= 0
                    || candidate.combatPower <= 0f || !MechClusterGenerator.MechKindSuitableForCluster(candidate)) continue;
                float difference = Math.Abs(candidate.combatPower - original.combatPower);
                if (difference > bestDifference) continue;
                if (difference == bestDifference && best != null
                    && string.CompareOrdinal(candidate.defName, best.defName) >= 0) continue;
                best = candidate;
                bestDifference = difference;
            }
            return best ?? PawnKindDefOf.Mech_Scyther;
        }
    }
}
