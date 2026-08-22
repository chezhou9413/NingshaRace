using System.Collections.Generic;
using RimWorld;
using Verse;

using NingshaRaceLib.DesertPit.AntColony.Buildings;

namespace NingshaRaceLib.DesertPit.AntColony.State
{
    //类职责：保存一个蚁巢在地图上的实体引用、有效规模、储藏位置、警戒状态和补员计时。
    public class AntColonyState : IExposable
    {
        //字段职责：保存地图内唯一的蚁巢编号。
        public int Id;

        //字段职责：保存当前蚁巢使用的独立敌对阵营。
        public Faction Faction;

        //字段职责：引用仍存活的蚁穴实体。
        public Building_DesertPitAntNest Nest;

        //字段职责：即使蚁穴被毁也保留其最后位置。
        public IntVec3 NestPosition;

        //字段职责：引用负责繁殖和升级营养结算的唯一蚁后。
        public Pawn Queen;

        //字段职责：保存蚁后、工蚁、兵蚁和爆浆蚁成员引用。
        public List<Pawn> Members = new List<Pawn>();

        //字段职责：保存蚁巢十二个实体储藏格。
        public List<IntVec3> StorageCells = new List<IntVec3>();

        //字段职责：保存当前等级或固定倍率对应的成员目标。
        public AntColonyPopulationSettings Population;

        //字段职责：标记当前巢群是否启用等级与营养升级机制。
        public bool LevelingEnabled;

        //字段职责：记录当前蚁巢等级。
        public int CurrentLevel = 1;

        //字段职责：记录该蚁巢能够达到的随机最高等级。
        public int MaxLevel = 1;

        //字段职责：记录升级七天冷却结束的游戏 Tick。
        public int NextUpgradeTick;

        //字段职责：记录蚁穴下一次允许消耗储藏营养修复的游戏 Tick。
        public int NextRepairTick;

        //字段职责：记录四小时伤亡撤退结束的游戏 Tick。
        public int RetreatUntilTick;

        //字段职责：保存一天内常规蚂蚁的死亡时间和地点。
        public List<AntDeathRecord> DeathRecords = new List<AntDeathRecord>();

        //字段职责：记录调查队下一次允许派遣的游戏 Tick。
        public int NextInvestigationTick;

        //字段职责：标记蚁穴是否已经被摧毁。
        public bool NestDestroyed;

        //字段职责：标记失去蚁穴后的永久狂暴状态。
        public bool Frenzy;

        //字段职责：记录蚁穴最近一次承受伤害的游戏 Tick。
        public int LastNestDamageTick = -1;

        //字段职责：记录完整警报下一波爆浆蚁补充时间。
        public int NextBoomWaveTick = -1;

        //字段职责：记录蚁后下一次允许补员的游戏 Tick。
        public int NextBirthTick;

        //字段职责：引用最近攻击蚁穴的 Pawn。
        public Pawn LastAggressor;

        //字段职责：缓存当前领地内可以攻击的外来实体。
        public List<Thing> Intruders = new List<Thing>();

        //函数职责：把需要跨存档保留的蚁巢实体引用、位置和计时写入地图存档。
        public void ExposeData()
        {
            Scribe_Values.Look(ref Id, "id");
            Scribe_References.Look(ref Faction, "faction");
            Scribe_References.Look(ref Nest, "nest");
            Scribe_Values.Look(ref NestPosition, "nestPosition");
            Scribe_References.Look(ref Queen, "queen");
            Scribe_Collections.Look(ref Members, "members", LookMode.Reference);
            Scribe_Collections.Look(ref StorageCells, "storageCells", LookMode.Value);
            Scribe_Deep.Look(ref Population, "population");
            Scribe_Values.Look(ref LevelingEnabled, "levelingEnabled", false);
            Scribe_Values.Look(ref CurrentLevel, "currentLevel", 1);
            Scribe_Values.Look(ref MaxLevel, "maxLevel", 1);
            Scribe_Values.Look(ref NextUpgradeTick, "nextUpgradeTick");
            Scribe_Values.Look(ref NextRepairTick, "nextRepairTick");
            Scribe_Values.Look(ref RetreatUntilTick, "retreatUntilTick");
            Scribe_Collections.Look(ref DeathRecords, "deathRecords", LookMode.Deep);
            Scribe_Values.Look(ref NextInvestigationTick, "nextInvestigationTick");
            Scribe_Values.Look(ref NestDestroyed, "nestDestroyed");
            Scribe_Values.Look(ref Frenzy, "frenzy");
            Scribe_Values.Look(ref LastNestDamageTick, "lastNestDamageTick", -1);
            Scribe_Values.Look(ref NextBoomWaveTick, "nextBoomWaveTick", -1);
            Scribe_Values.Look(ref NextBirthTick, "nextBirthTick");
            Scribe_References.Look(ref LastAggressor, "lastAggressor");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Members = Members ?? new List<Pawn>();
                StorageCells = StorageCells ?? new List<IntVec3>();
                DeathRecords = DeathRecords ?? new List<AntDeathRecord>();
                Intruders = new List<Thing>();
            }
        }
    }
}
