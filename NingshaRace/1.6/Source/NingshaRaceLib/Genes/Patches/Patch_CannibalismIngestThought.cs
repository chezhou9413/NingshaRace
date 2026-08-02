using System.Linq;
using HarmonyLib;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Genes.Utility;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Genes.Patches
{
    //类职责：过滤同类相食基因携带者因食用人肉而产生的负面摄食思想。
    [HarmonyPatch(typeof(FoodUtility), "TryAddIngestThought")]
    public static class Patch_CannibalismIngestThought
    {
        //函数职责：仅跳过人类肉来源的负面思想，保留正面思想和其他食物产生的思想。
        public static bool Prefix(Pawn ingester, ThoughtDef def, MeatSourceCategory meatSourceCategory)
        {
            if (meatSourceCategory != MeatSourceCategory.Humanlike
                || !NingshaGeneUtility.HasActiveGene(ingester, DefOfRefs.NingshaRace_Cannibalism))
            {
                return true;
            }

            return def?.stages == null
                || !def.stages.Any(stage => stage.baseMoodEffect < 0f);
        }
    }
}
