using RimWorld;
using Verse;

namespace NingshaRaceLib
{
    //类职责：集中声明凝砂族代码需要直接访问的 Def，避免字符串分散在各个系统里。
    [DefOf]
    public static class DefOfRefs
    {
        //字段职责：凝砂族主种族 ThingDef。
        public static ThingDef NingshaRace;

        //字段职责：沙傀独立 Pawn 种族 ThingDef。
        public static ThingDef NingshaRace_SandGolem;

        //字段职责：用于生成沙傀 Pawn 的 PawnKindDef。
        public static PawnKindDef NingshaRace_SandGolemKind;

        //字段职责：玩家凝砂族使用的召唤沙傀能力。
        public static AbilityDef NingshaRace_Ability_SummonSandGolem;

        //字段职责：标记 Pawn 为沙傀并提供隐藏状态效果。
        public static HediffDef NingshaRace_SandGolemMarker;

        //字段职责：ChezhouLib 导入的沙偶渲染 ShaderTypeDef。
        public static ShaderTypeDef NingshaRace_PawnSandify_ShaderPro;

        //字段职责：沙傀专属童年背景故事。
        public static BackstoryDef NingshaRace_SandGolem_Childhood_FormlessSand;

        //字段职责：沙傀专属成年背景故事。
        public static BackstoryDef NingshaRace_SandGolem_Adulthood_BoundGolem;

        //字段职责：凝砂族鞭击逐帧 Mote 动画。
        public static ThingDef NingshaRace_Mote_WhipFrameAnimation;

        //字段职责：凝砂族战士必定携带的蛇腹剑武器。
        public static ThingDef NingshaRace_SnakeBellySword;

        //字段职责：凝砂族地刺召唤物远程武器。
        public static ThingDef NingshaRace_GroundSpikeSummoner;

        //字段职责：凝砂族地刺逐帧 Mote 动画。
        public static ThingDef NingshaRace_Mote_GroundSpikeFrameAnimation;

        //字段职责：地刺命中后执行一格击退的原版 PawnFlyer。
        public static ThingDef NingshaRace_PawnFlyer_GroundSpikeKnockback;

        //字段职责：可进入沙漠巨坑口袋地图的地表入口建筑。
        public static ThingDef NingshaRace_DesertPitGate;

        //字段职责：沙漠巨坑地下沙岩洞穴使用的口袋地图生成器。
        public static MapGeneratorDef NingshaRace_DesertPitMap;

        //静态构造函数职责：让 RimWorld 在 Def 初始化阶段填充本类字段。
        static DefOfRefs()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DefOfRefs));
        }
    }
}
