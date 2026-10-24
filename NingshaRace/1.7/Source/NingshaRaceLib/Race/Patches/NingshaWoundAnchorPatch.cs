using System.Collections.Generic;
using HarmonyLib;
using NingshaRaceLib.Core.Defs;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Race.Patches
{
    //类职责：让共用同一身体轮廓的凝砂体型使用一致的伤口锚点基准。
    [HarmonyPatch(typeof(PawnDrawUtility), nameof(PawnDrawUtility.FindAnchors))]
    public static class NingshaWoundAnchorPatch
    {
        //函数职责：按种族替换锚点来源，避免壮硕或肥胖体型把绷带推到身体之外。
        private static void Postfix(Pawn pawn, BodyPartRecord curPart, ref IEnumerable<BodyTypeDef.WoundAnchor> __result)
        {
            if (pawn.def == DefOfRefs.NingshaRace) __result = FindRaceAnchors(curPart);
        }

        //函数职责：沿受伤部位向父部位匹配统一基准中的标签或身体分组。
        private static IEnumerable<BodyTypeDef.WoundAnchor> FindRaceAnchors(BodyPartRecord part)
        {
            while (part != null)
            {
                bool found = false;
                foreach (BodyTypeDef.WoundAnchor anchor in BodyTypeDefOf.Female.woundAnchors)
                {
                    bool matches = part.woundAnchorTag != null ? anchor.tag == part.woundAnchorTag
                        : anchor.group != null && part.IsInGroup(anchor.group);
                    if (!matches) continue;
                    found = true;
                    yield return anchor;
                }
                if (found) yield break;
                part = part.parent;
            }
        }
    }
}
