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

        //字段职责：提高异常事件出现比例的索提斯叙事者定义。
        public static StorytellerDef Ningsha_SotisiStoryteller;

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

        //字段职责：保存供奉营养并发布三类探索任务的智慧之蛇祭坛。
        public static ThingDef NingshaRace_Altar;

        //字段职责：小型遗迹独占产出的不可食用炙热香料。
        public static ThingDef NingshaRace_SandHeat;

        //字段职责：消耗生菌制作的凝砂族厚实饼食。
        public static ThingDef NingshaRace_MushroomFlatbread;

        //字段职责：食用后降低侵蚀并提供正面心情的香料料理。
        public static ThingDef NingshaRace_ScorchingEarthStew;

        //字段职责：食用炙热地煲后持续一天的正面心情记忆。
        public static ThoughtDef NingshaRace_AteScorchingEarthStew;

        //字段职责：搬运者把生肉转化为祭坛供奉营养时使用的工作。
        public static JobDef NingshaRace_Job_FillWisdomSerpentAltar;

        //字段职责：殖民者在满值祭坛前祈求任务时使用的工作。
        public static JobDef NingshaRace_Job_ConsultWisdomSerpentAltar;

        //字段职责：智慧之蛇祭坛发布的小型遗迹任务定义。
        public static QuestScriptDef NingshaRace_Quest_AltarSmallRuins;

        //字段职责：智慧之蛇祭坛发布的清剿蚁巢任务定义。
        public static QuestScriptDef NingshaRace_Quest_AltarAntNest;

        //字段职责：智慧之蛇祭坛发布的解救同胞任务定义。
        public static QuestScriptDef NingshaRace_Quest_AltarRescueKinsfolk;

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

        //字段职责：以严重度保存一至二十次凝砂族蜕皮层数的可见状态。
        public static HediffDef NingshaRace_MoltingLayers;

        //字段职责：蜕皮营养达到六十后阻止原版立即死亡的隐藏就绪状态。
        public static HediffDef NingshaRace_MoltingRescueReady;

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

        //字段职责：小型遗迹武器池中的凝砂族飞针武器。
        public static ThingDef NingshaRace_FlyingNeedle;

        //字段职责：凝砂族地刺召唤物远程武器。
        public static ThingDef NingshaRace_GroundSpikeSummoner;

        //字段职责：锐沙方块装备后授予的环形地刺能力。
        public static AbilityDef NingshaRace_Ability_SandstoneSpikeRing;

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

        //字段职责：沙漠巨坑地下用于返回地表和双向货运的专用离洞绳。
        public static ThingDef NingshaRace_DesertPitCaveExit;

        //字段职责：沙漠巨坑内周期性生成化合燃料的天然油砂渗洞。
        public static ThingDef NingshaRace_DesertPitOilSeep;

        //字段职责：沙漠巨坑内等待玩家调查并揭示巨型墓葬入口的破损砂岩石棺。
        public static ThingDef NingshaRace_GiantTombBrokenCoffin;

        //字段职责：破损石棺调查完成后使用的巨型墓葬口袋地图入口。
        public static ThingDef NingshaRace_GiantTombCoffinEntrance;

        //字段职责：殖民者右键调查破损石棺并揭示墓葬入口时使用的工作。
        public static JobDef NingshaRace_Job_InvestigateGiantTombCoffin;

        //字段职责：可播种并收获生菌的蓝灰洞杯菌。
        public static ThingDef NingshaRace_DesertPitPlantA;

        //字段职责：可播种并收获生菌的浅青伞菇。
        public static ThingDef NingshaRace_DesertPitPlantC;

        //字段职责：可播种并收获草药的紫辉花簇菌。
        public static ThingDef NingshaRace_DesertPitPlantD;

        //字段职责：高级矿脉可能生成的原版压缩塑钢。
        public static ThingDef MineablePlasteel;

        //字段职责：高级矿脉可能生成的原版铀矿石。
        public static ThingDef MineableUranium;

        //字段职责：高级矿脉可能生成的原版黄金矿石。
        public static ThingDef MineableGold;

        //字段职责：沙漠巨坑地下沙岩洞穴使用的口袋地图生成器。
        public static MapGeneratorDef NingshaRace_DesertPitMap;

        //字段职责：沙漠巨坑巢群使用的可攻击蚁穴建筑。
        public static ThingDef NingshaRace_DesertPitAntNest;

        //字段职责：沙漠巨坑中的独立蚁巢实例共同使用的隐藏永久敌对阵营定义。
        public static FactionDef NingshaRace_DesertPitAntColonyFaction;

        //字段职责：生成负责全图采集和实体搬运的洞穴工蚁。
        public static PawnKindDef NingshaRace_DesertPitWorkerAntKind;

        //字段职责：生成负责蚁穴领地巡逻和拦截的洞穴兵蚁。
        public static PawnKindDef NingshaRace_DesertPitSoldierAntKind;

        //字段职责：生成每个巢群唯一且负责补员的洞穴蚁后。
        public static PawnKindDef NingshaRace_DesertPitQueenAntKind;

        //字段职责：生成蚁穴受击时追踪入侵者的爆浆蚁。
        public static PawnKindDef NingshaRace_DesertPitBoomAntKind;

        //字段职责：小型遗迹使用的敌对凝砂木乃伊PawnKind。
        public static PawnKindDef NingshaRace_GiantTombMummyKind;

        //字段职责：解救任务临时承载待救凝砂族的隐藏友方阵营。
        public static FactionDef NingshaRace_RescueFaction;

        //字段职责：小型遗迹的专用原版Site世界物体定义。
        public static WorldObjectDef NingshaRace_AltarSmallRuinsSite;

        //字段职责：清剿蚁巢的专用原版Site世界物体定义。
        public static WorldObjectDef NingshaRace_AltarAntNestSite;

        //字段职责：解救同胞地表地点的专用原版Site世界物体定义。
        public static WorldObjectDef NingshaRace_AltarRescueSurfaceSite;

        //字段职责：解救同胞地下地点的专用原版Site世界物体定义。
        public static WorldObjectDef NingshaRace_AltarRescueUndergroundSite;

        //字段职责：小型遗迹地图附加目标使用的SitePart定义。
        public static SitePartDef NingshaRace_AltarSmallRuinsPart;

        //字段职责：固定蚁巢地图附加目标使用的SitePart定义。
        public static SitePartDef NingshaRace_AltarAntNestPart;

        //字段职责：解救同胞地图附加目标使用的SitePart定义。
        public static SitePartDef NingshaRace_AltarRescuePart;

        //字段职责：工蚁把实体资源搬到本巢储藏格时使用的工作。
        public static JobDef NingshaRace_Job_DesertPitAntHaul;

        //字段职责：蚁后消耗巢穴食物并补充常规成员时使用的工作。
        public static JobDef NingshaRace_Job_DesertPitAntReproduce;

        //字段职责：爆浆蚁追到入侵者身边并触发酸液自爆时使用的工作。
        public static JobDef NingshaRace_Job_DesertPitBoomAntDetonate;

        //静态构造函数职责：让 RimWorld 在 Def 初始化阶段填充本类字段。
        static DefOfRefs()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DefOfRefs));
        }
    }
}
