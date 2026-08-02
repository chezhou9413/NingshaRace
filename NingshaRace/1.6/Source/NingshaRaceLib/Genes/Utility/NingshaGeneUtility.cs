using Verse;

namespace NingshaRaceLib.Genes.Utility
{
    //类职责：集中处理凝砂族基因的活动状态查询，统一排除被覆盖或未激活的基因。
    public static class NingshaGeneUtility
    {
        //函数职责：判断 Pawn 是否携带且当前启用了指定基因。
        public static bool HasActiveGene(Pawn pawn, GeneDef geneDef)
        {
            Gene gene = pawn?.genes?.GetGene(geneDef);
            return gene?.Active == true;
        }
    }
}
