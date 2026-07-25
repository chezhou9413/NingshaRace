using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.SandGolem.Health;
using NingshaRaceLib.SandGolem.Rendering;
using NingshaRaceLib.SandGolem.Tracking;
using NingshaRaceLib.SandGolem.Utility;

namespace NingshaRaceLib.SandGolem.Lifecycle
{
    //类职责：清理沙傀不应拥有的人类身份数据，让沙傀表现为临时造物。
    public static class SandGolemIdentityCleaner
    {
        //字段职责：访问背景故事缓存字段，清理后避免旧背景继续参与显示和工作限制计算。
        private static readonly FieldInfo BackstoriesCacheField = typeof(Pawn_StoryTracker).GetField("backstoriesCache", BindingFlags.Instance | BindingFlags.NonPublic);

        //字段职责：访问基因跟踪器的 xenogene 列表，补足公共接口只清一部分基因的问题。
        private static readonly FieldInfo XenogenesField = typeof(Pawn_GeneTracker).GetField("xenogenes", BindingFlags.Instance | BindingFlags.NonPublic);

        //字段职责：访问基因跟踪器的 endogene 列表，补足公共接口只清一部分基因的问题。
        private static readonly FieldInfo EndogenesField = typeof(Pawn_GeneTracker).GetField("endogenes", BindingFlags.Instance | BindingFlags.NonPublic);

        //字段职责：访问基因跟踪器的缓存列表，清理后避免旧基因缓存继续显示。
        private static readonly FieldInfo CachedGenesField = typeof(Pawn_GeneTracker).GetField("cachedGenes", BindingFlags.Instance | BindingFlags.NonPublic);

        //字段职责：访问基因跟踪器的 xenotype 字段，让沙傀保持基础空基因类型。
        private static readonly FieldInfo XenotypeField = typeof(Pawn_GeneTracker).GetField("xenotype", BindingFlags.Instance | BindingFlags.NonPublic);

        //字段职责：访问信仰字段，避免通过 SetIdeo(null) 触发原版空信仰流程。
        private static readonly FieldInfo IdeoField = typeof(Pawn_IdeoTracker).GetField("ideo", BindingFlags.Instance | BindingFlags.NonPublic);

        //字段职责：访问信仰历史字段，让沙傀不保留旧信仰记录。
        private static readonly FieldInfo PreviousIdeosField = typeof(Pawn_IdeoTracker).GetField("previousIdeos", BindingFlags.Instance | BindingFlags.NonPublic);

        //字段职责：访问爵位列表字段，清理沙傀不该拥有的爵位。
        private static readonly FieldInfo RoyalTitlesField = typeof(Pawn_RoyaltyTracker).GetField("titles", BindingFlags.Instance | BindingFlags.NonPublic);

        //字段职责：访问爵位恩惠字段，清理沙傀不该拥有的帝国资源。
        private static readonly FieldInfo RoyalFavorField = typeof(Pawn_RoyaltyTracker).GetField("favor", BindingFlags.Instance | BindingFlags.NonPublic);

        //字段职责：访问最高爵位缓存字段，清理沙傀不该拥有的爵位缓存。
        private static readonly FieldInfo RoyalHighestTitlesField = typeof(Pawn_RoyaltyTracker).GetField("highestTitles", BindingFlags.Instance | BindingFlags.NonPublic);

        //字段职责：访问继承人字段，清理沙傀不该拥有的继承关系。
        private static readonly FieldInfo RoyalHeirsField = typeof(Pawn_RoyaltyTracker).GetField("heirs", BindingFlags.Instance | BindingFlags.NonPublic);

        //字段职责：访问许可字段，清理沙傀不该拥有的许可能力。
        private static readonly FieldInfo RoyalPermitsField = typeof(Pawn_RoyaltyTracker).GetField("factionPermits", BindingFlags.Instance | BindingFlags.NonPublic);

        //字段职责：访问爵位授予的能力字段，清理沙傀不该拥有的能力。
        private static readonly FieldInfo RoyalAbilitiesField = typeof(Pawn_RoyaltyTracker).GetField("abilities", BindingFlags.Instance | BindingFlags.NonPublic);

        //字段职责：访问记录字段，清理沙傀不该拥有的个人履历统计。
        private static readonly FieldInfo RecordsField = typeof(Pawn_RecordsTracker).GetField("records", BindingFlags.Instance | BindingFlags.NonPublic);

        //字段职责：访问战斗记录字段，清理沙傀不该拥有的战斗履历。
        private static readonly FieldInfo BattleActiveField = typeof(Pawn_RecordsTracker).GetField("battleActive", BindingFlags.Instance | BindingFlags.NonPublic);

        //字段职责：访问战斗退出时间字段，清理沙傀不该拥有的战斗履历。
        private static readonly FieldInfo BattleExitTickField = typeof(Pawn_RecordsTracker).GetField("battleExitTick", BindingFlags.Instance | BindingFlags.NonPublic);

        //函数职责：清理 Pawn 上和个体身份相关的数据。
        public static void Clean(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            ClearBackstories(pawn);
            ClearGenes(pawn);
            ClearIdeologyAndRoyalty(pawn);
            ClearMindAndRecords(pawn);
        }

        //函数职责：移除童年、成年背景、个人称号和喜好颜色。
        private static void ClearBackstories(Pawn pawn)
        {
            if (pawn.story == null)
            {
                return;
            }

            pawn.story.Childhood = DefOfRefs.NingshaRace_SandGolem_Childhood_FormlessSand;
            pawn.story.Adulthood = DefOfRefs.NingshaRace_SandGolem_Adulthood_BoundGolem;
            pawn.story.title = null;
            pawn.story.birthLastName = null;
            pawn.story.favoriteColor = null;
            pawn.story.traits?.allTraits?.Clear();
            BackstoriesCacheField?.SetValue(pawn.story, null);
        }

        //函数职责：移除所有基因和自定义基因型显示数据。
        private static void ClearGenes(Pawn pawn)
        {
            if (pawn.genes == null || !ModsConfig.BiotechActive)
            {
                return;
            }

            List<Gene> genes = new List<Gene>(pawn.genes.GenesListForReading);
            for (int i = genes.Count - 1; i >= 0; i--)
            {
                pawn.genes.RemoveGene(genes[i]);
            }

            pawn.genes.xenotypeName = null;
            pawn.genes.iconDef = null;
            pawn.genes.hybrid = false;
            XenogenesField?.SetValue(pawn.genes, new List<Gene>());
            EndogenesField?.SetValue(pawn.genes, new List<Gene>());
            CachedGenesField?.SetValue(pawn.genes, null);
            XenotypeField?.SetValue(pawn.genes, XenotypeDefOf.Baseliner);
            pawn.skills?.DirtyAptitudes();
            pawn.Notify_DisabledWorkTypesChanged();
        }

        //函数职责：移除信仰、爵位和联结这类人类社会身份。
        private static void ClearIdeologyAndRoyalty(Pawn pawn)
        {
            if (pawn.ideo != null)
            {
                IdeoField?.SetValue(pawn.ideo, null);
                PreviousIdeosField?.SetValue(pawn.ideo, new List<Ideo>());
            }
            if (pawn.royalty != null)
            {
                RoyalTitlesField?.SetValue(pawn.royalty, new List<RoyalTitle>());
                RoyalFavorField?.SetValue(pawn.royalty, new Dictionary<Faction, int>());
                RoyalHighestTitlesField?.SetValue(pawn.royalty, new Dictionary<Faction, RoyalTitleDef>());
                RoyalHeirsField?.SetValue(pawn.royalty, new Dictionary<Faction, Pawn>());
                RoyalPermitsField?.SetValue(pawn.royalty, new List<FactionPermit>());
                RoyalAbilitiesField?.SetValue(pawn.royalty, new List<Ability>());
            }
            pawn.connections?.Notify_PawnKilled();
        }

        //函数职责：清理思想、记录和关系类运行时身份痕迹。
        private static void ClearMindAndRecords(Pawn pawn)
        {
            pawn.relations?.ClearAllRelations();
            if (pawn.records != null)
            {
                RecordsField?.SetValue(pawn.records, new DefMap<RecordDef, float>());
                BattleActiveField?.SetValue(pawn.records, null);
                BattleExitTickField?.SetValue(pawn.records, 0);
            }
            pawn.mindState?.mentalStateHandler?.Reset();
            pawn.guest?.SetGuestStatus(null);
        }
    }
}
