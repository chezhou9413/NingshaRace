using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Scenarios.Parts
{
    //类职责：配置凝砂族开局人物，使必选成员和备用候选人均按种族异种表生成。
    public sealed class ScenPart_NingshaStartingPawns : ScenPart_ConfigPage_ConfigureStartingPawns_KindDefs
    {
        //函数职责：保留原版必选人物生成规则，并在原版补足候选人前生成具有正确基因配置的备用成员。
        protected override void GenerateStartingPawns()
        {
            Find.GameInitData.startingPawnKind = DefOfRefs.NingshaRace_Colonist;
            base.GenerateStartingPawns();
            while (Find.GameInitData.startingAndOptionalPawns.Count < pawnChoiceCount)
            {
                int index = Find.GameInitData.startingAndOptionalPawns.Count;
                PawnGenerationRequest request = StartingPawnUtility.GetGenerationRequest(index);
                request.KindDef = DefOfRefs.NingshaRace_Colonist;
                //原版默认请求强制基础人异种，清空后才能读取凝砂族种类的异种表。
                request.ForcedXenotype = null;
                StartingPawnUtility.SetGenerationRequest(index, request);
                StartingPawnUtility.AddNewPawn(index);
            }
        }

        //函数职责：复制场景编辑数据并保持凝砂族专用类型，避免编辑副本退回原版候选人生成流程。
        protected override ScenPart CopyForEditingInner()
        {
            ScenPart_NingshaStartingPawns copy = new ScenPart_NingshaStartingPawns
            {
                def = def,
                visible = visible,
                summarized = summarized,
                pawnChoiceCount = pawnChoiceCount
            };
            foreach (PawnKindCount entry in kindCounts)
            {
                copy.kindCounts.Add(new PawnKindCount
                {
                    kindDef = entry.kindDef,
                    count = entry.count,
                    requiredAtStart = entry.requiredAtStart
                });
            }
            return copy;
        }
    }
}
