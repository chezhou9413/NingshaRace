using RimWorld;
using Verse;


namespace NingshaRaceLib.Core.Defs
{
    //类职责：集中声明凝砂族代码需要直接访问的 Def，避免字符串分散在各个系统里。
    [DefOf]
    public static class DefOfRefs
    {
        //字段职责：凝砂族主种族 ThingDef。
        public static ThingDef NingshaRace;

        //字段职责：用于生成成年凝砂族殖民者的 PawnKindDef。
        public static PawnKindDef NingshaRace_Colonist;

        //字段职责：用于生成三至十七岁凝砂族儿童的 PawnKindDef。
        public static PawnKindDef NingshaRace_Child;

        //字段职责：凝砂族成员默认使用的可遗传异种基因组。
        public static XenotypeDef NingshaRace_Xenotype;

        //字段职责：凝砂族周期性排出的未受精食用卵。
        public static ThingDef NingshaRace_EggUnfertilized;

        //字段职责：保存父母数据并可在孵化巢中发育的受精卵。
        public static ThingDef NingshaRace_EggFertilized;

        //字段职责：容纳单枚凝砂卵并推进孵化的建筑。
        public static ThingDef NingshaRace_HatchNest;

        //字段职责：搬运者将凝砂卵放入孵化巢时使用的工作。
        public static JobDef NingshaRace_Job_PlaceEggInHatchNest;

        //字段职责：让近战命中有概率累计石化进度的毒牙基因。
        public static GeneDef NingshaRace_VenomFangs;

        //字段职责：过滤食用人肉负面思想的同类相食基因。
        public static GeneDef NingshaRace_Cannibalism;

        //字段职责：同类相食基因携带者直接食用生肉后获得的正面心情。
        public static ThoughtDef NingshaRace_AteRawMeat;

        //字段职责：沙傀独立 Pawn 种族 ThingDef。
        public static ThingDef NingshaRace_SandGolem;

        //字段职责：用于生成沙傀 Pawn 的 PawnKindDef。
        public static PawnKindDef NingshaRace_SandGolemKind;

        //字段职责：玩家凝砂族使用的召唤沙傀能力。
        public static AbilityDef NingshaRace_Ability_SummonSandGolem;

        //字段职责：玩家凝砂族固有的扇形石化砂潮能力。
        public static AbilityDef NingshaRace_Ability_PetrifyingSandwave;

        //字段职责：玩家凝砂族通过增加侵蚀值清除固有能力冷却的过载能力。
        public static AbilityDef NingshaRace_Ability_ErosionOverload;

        //字段职责：侵蚀体以二十秒冷却自动使用的强化凝砂之眼。
        public static AbilityDef NingshaRace_Ability_ErosionBodyPetrifyingSandwave;

        //字段职责：凝砂族实时计算基因、Hediff 和装备修正后的侵蚀值上限。
        public static StatDef NingshaRace_ErosionLimit;

        //字段职责：满侵蚀实体化动画期间锁定 Pawn 的转化 Hediff。
        public static HediffDef NingshaRace_ErosionTransformation;

        //字段职责：侵蚀体 Mutant 持有的永久实体状态 Hediff。
        public static HediffDef NingshaRace_ErosionBody;

        //字段职责：将凝砂族永久转换为原版实体阵营侵蚀体的 MutantDef。
        public static MutantDef NingshaRace_ErosionBodyMutant;

        //字段职责：ChezhouLib 导入的侵蚀体头部上层黑雾 ShaderTypeDef。
        public static ShaderTypeDef NingshaRace_UpperErosionBlackFog_ShaderPro;

        //字段职责：标记 Pawn 为沙傀并提供隐藏状态效果。
        public static HediffDef NingshaRace_SandGolemMarker;

        //字段职责：ChezhouLib 导入的沙偶渲染 ShaderTypeDef。
        public static ShaderTypeDef NingshaRace_PawnSandify_ShaderPro;

        //字段职责：可由外部累计并在满层时锁定 Pawn 的石化 Hediff。
        public static HediffDef NingshaRace_Petrification;

        //字段职责：ChezhouLib 导入的 Pawn 石化渲染 ShaderTypeDef。
        public static ShaderTypeDef NingshaRace_PawnPetrify_ShaderPro;

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

        //字段职责：凝砂族使用的扇形喷砂武器。
        public static ThingDef NingshaRace_SandBottle;

        //字段职责：沙瓶命中 Pawn 后施加的限时移动减速状态。
        public static HediffDef NingshaRace_SandBottleSlow;

        //字段职责：凝砂族地刺逐帧 Mote 动画。
        public static ThingDef NingshaRace_Mote_GroundSpikeFrameAnimation;

        //字段职责：沙漠适应基因认可的凝砂族地下沙漠生态。
        public static BiomeDef NingshaRace_DesertPitBiome;

        //字段职责：沙漠适应基因认可的原版极端沙漠生态。
        public static BiomeDef ExtremeDesert;

        //字段职责：地刺命中后执行一格击退的原版 PawnFlyer。
        public static ThingDef NingshaRace_PawnFlyer_GroundSpikeKnockback;

        //字段职责：开发者生成的凝砂族巨剑“葬岳”。
        public static ThingDef NingshaRace_BurialMountainGreatsword;

        //字段职责：“葬岳”装备后授予的坠岳斩能力。
        public static AbilityDef NingshaRace_Ability_FallingMountainSlash;

        //字段职责：承载坠岳斩位移、挥砍和伤害结算的 PawnFlyer。
        public static ThingDef NingshaRace_PawnFlyer_FallingMountainSlash;

        //字段职责：坠岳斩使用的程序化土元素月牙刀光。
        public static ThingDef NingshaRace_Mote_TerraCrescentSlash;

        //字段职责：坠岳斩落地时显示的单张地裂 Mote。
        public static ThingDef NingshaRace_Mote_FallingMountainGroundCrack;

        //字段职责：ChezhouLib 导入的土元素月牙刀光 ShaderTypeDef。
        public static ShaderTypeDef NingshaRace_TerraCrescentSlash_ShaderPro;

        //字段职责：葬岳格挡模式常驻沙土护盾 Mote。
        public static ThingDef NingshaRace_Mote_BurialMountainGuardShield;

        //字段职责：葬岳格挡蓄满后的沙土爆发 Mote。
        public static ThingDef NingshaRace_Mote_BurialMountainGuardBurst;

        //字段职责：ChezhouLib 导入的葬岳沙土护盾 ShaderTypeDef。
        public static ShaderTypeDef NingshaRace_BurialMountainGuardShield_ShaderPro;

        //字段职责：ChezhouLib 导入的葬岳格挡爆发 ShaderTypeDef。
        public static ShaderTypeDef NingshaRace_BurialMountainGuardBurst_ShaderPro;

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
