using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Genes.Utility;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Genes.Patches
{
    //类职责：处理同类相食基因携带者的人肉负面思想过滤与生肉正面心情替换。
    [HarmonyPatch(typeof(FoodUtility), "TryAddIngestThought")]
    public static class Patch_CannibalismIngestThought
    {
        //函数职责：把直接食用生肉的原版惩罚替换为专属正面心情，并过滤人肉来源的其他负面思想。
        public static bool Prefix(
            Pawn ingester,
            ThoughtDef def,
            List<FoodUtility.ThoughtFromIngesting> ingestThoughts,
            ThingDef foodDef,
            MeatSourceCategory meatSourceCategory)
        {
            if (!NingshaGeneUtility.HasActiveGene(ingester, DefOfRefs.NingshaRace_Cannibalism))
            {
                return true;
            }

            if (def == ThoughtDefOf.AteRawFood && foodDef.IsMeat)
            {
                ingestThoughts.Add(new FoodUtility.ThoughtFromIngesting
                {
                    thought = DefOfRefs.NingshaRace_AteRawMeat
                });
                return false;
            }

            if (meatSourceCategory != MeatSourceCategory.Humanlike)
            {
                return true;
            }

            return def?.stages == null
                || !def.stages.Any(stage => stage.baseMoodEffect < 0f);
        }
    }
}
