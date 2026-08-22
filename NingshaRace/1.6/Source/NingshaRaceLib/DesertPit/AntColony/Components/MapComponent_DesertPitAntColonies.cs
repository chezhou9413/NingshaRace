using System.Collections.Generic;
using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Buildings;
using NingshaRaceLib.DesertPit.AntColony.Config;
using NingshaRaceLib.DesertPit.AntColony.Core;
using NingshaRaceLib.DesertPit.AntColony.State;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //类职责：统一管理当前地图上的全部洞穴蚁巢、成员、独立规模、警戒、补员和物资分配。
    public partial class MapComponent_DesertPitAntColonies : MapComponent
    {
        //字段职责：保存当前地图全部仍有效或尚有成员的巢群状态。
        private List<AntColonyState> colonies = new List<AntColonyState>();

        //字段职责：记录下一次注册巢群时分配的唯一编号。
        private int nextColonyId = 1;

        //字段职责：按巢群编号缓存运行时状态查询。
        private Dictionary<int, AntColonyState> coloniesById = new Dictionary<int, AntColonyState>();

        //字段职责：按成员 Pawn 缓存其所属巢群。
        private Dictionary<Pawn, AntColonyState> coloniesByPawn = new Dictionary<Pawn, AntColonyState>();

        //字段职责：按蚁穴建筑缓存其所属巢群。
        private Dictionary<Building_DesertPitAntNest, AntColonyState> coloniesByNest = new Dictionary<Building_DesertPitAntNest, AntColonyState>();

        //字段职责：缓存全图可供工蚁采集的实体物资。
        private List<Thing> forageCandidates = new List<Thing>();

        //字段职责：记录已经分配给工蚁的采集物，避免多只工蚁重复争抢。
        private Dictionary<Thing, Pawn> assignedForageThings = new Dictionary<Thing, Pawn>();

        //字段职责：记录已经分配给搬运工作的实体储藏格。
        private Dictionary<IntVec3, Pawn> assignedStorageCells = new Dictionary<IntVec3, Pawn>();

        //属性职责：统一读取蚁穴 Def 上的行为、等级、撤退与调查配置。
        private DefModExtension_AntColony Settings => DefOfRefs.NingshaRace_DesertPitAntNest.GetModExtension<DefModExtension_AntColony>();

        //构造函数职责：把蚁群管理组件绑定到指定地图。
        public MapComponent_DesertPitAntColonies(Map map) : base(map)
        {
        }

        //函数职责：保存地图中的所有巢群状态和下一个可用巢群编号。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref colonies, "desertPitAntColonies", LookMode.Deep);
            Scribe_Values.Look(ref nextColonyId, "nextDesertPitAntColonyId", 1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                colonies = colonies ?? new List<AntColonyState>();
            }
        }

        //函数职责：地图完成初始化后重建不写入存档的快速查询索引。
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            RebuildRuntimeIndices();
            RefreshForageCandidates();
        }

        //函数职责：按错峰间隔更新巢群状态，并定期刷新全图可搬运物资缓存。
        public override void MapComponentTick()
        {
            int ticks = Find.TickManager.TicksGame;
            if (ticks % 60 == map.uniqueID % 60)
            {
                UpdateColonies(ticks);
                ReleaseStaleForageAssignments();
            }

            if (ticks % 250 == map.uniqueID % 250)
            {
                RefreshForageCandidates();
            }
        }

        //函数职责：登记生成步骤创建的完整巢群，并向蚁穴和所有成员写入统一编号。
        public AntColonyState RegisterGeneratedColony(Building_DesertPitAntNest nest, Pawn queen, List<Pawn> members, List<IntVec3> storageCells, Faction faction, AntColonyPopulationSettings population, bool levelingEnabled, int currentLevel, int maximumLevel)
        {
            if (population == null)
            {
                throw new System.ArgumentNullException(nameof(population));
            }
            AntColonyState state = new AntColonyState
            {
                Id = nextColonyId++,
                Faction = faction,
                Nest = nest,
                NestPosition = nest.Position,
                Queen = queen,
                Members = new List<Pawn>(members),
                StorageCells = new List<IntVec3>(storageCells),
                Population = population,
                LevelingEnabled = levelingEnabled,
                CurrentLevel = currentLevel,
                MaxLevel = maximumLevel,
                NextRepairTick = Find.TickManager.TicksGame + Settings.repairIntervalTicks,
                NextBirthTick = Find.TickManager.TicksGame + Settings.reproductionCooldownTicks
            };

            nest.AssignColony(state.Id);
            for (int i = 0; i < state.Members.Count; i++)
            {
                Comp_DesertPitAntMember memberComp = state.Members[i].TryGetComp<Comp_DesertPitAntMember>();
                memberComp.AssignColony(state.Id);
            }

            colonies.Add(state);
            IndexColony(state);
            return state;
        }

        //函数职责：成员生成或读档恢复时把它登记到所属巢群及运行时索引。
        public void NotifyMemberSpawned(Pawn pawn, int colonyId)
        {
            AntColonyState state;
            if (pawn == null || !coloniesById.TryGetValue(colonyId, out state))
            {
                return;
            }

            if (!state.Members.Contains(pawn))
            {
                state.Members.Add(pawn);
            }

            coloniesByPawn[pawn] = state;
            Comp_DesertPitAntMember memberComp = pawn.TryGetComp<Comp_DesertPitAntMember>();
            if (memberComp != null && memberComp.Caste == AntCaste.Queen)
            {
                state.Queen = pawn;
            }
        }

        //函数职责：成员死亡或销毁时释放成员引用和它占用的搬运分配。
        public void NotifyMemberDestroyed(Pawn pawn, int colonyId)
        {
            AntColonyState state;
            if (pawn == null || !coloniesById.TryGetValue(colonyId, out state))
            {
                return;
            }

            Comp_DesertPitAntMember memberComp = pawn.TryGetComp<Comp_DesertPitAntMember>();
            if (pawn.Dead && memberComp != null)
            {
                RecordRegularAntDeath(state, pawn, memberComp.Caste);
            }

            state.Members.Remove(pawn);
            coloniesByPawn.Remove(pawn);
            if (state.Queen == pawn)
            {
                state.Queen = null;
            }

            ReleaseForageAssignments(pawn);
        }

        //函数职责：蚁穴生成或读档恢复时重建巢穴到巢群状态的索引。
        public void NotifyNestSpawned(Building_DesertPitAntNest nest, int colonyId)
        {
            AntColonyState state;
            if (nest != null && coloniesById.TryGetValue(colonyId, out state))
            {
                state.Nest = nest;
                state.NestPosition = nest.Position;
                coloniesByNest[nest] = state;
            }
        }

        //函数职责：蚁穴受击时进入完整警报，并在新一轮警报开始时立即补满爆浆蚁。
        public void NotifyNestDamaged(Building_DesertPitAntNest nest, int colonyId, Pawn aggressor)
        {
            AntColonyState state;
            if (!coloniesById.TryGetValue(colonyId, out state) || state.NestDestroyed)
            {
                return;
            }

            int ticks = Find.TickManager.TicksGame;
            bool wasAlarmed = IsFullAlarm(state, ticks);
            state.Nest = nest;
            state.NestPosition = nest.Position;
            state.LastNestDamageTick = ticks;
            state.NextRepairTick = System.Math.Max(state.NextRepairTick, ticks + Settings.repairDelayAfterDamageTicks);
            CancelInvestigation(state);
            if (aggressor != null && !aggressor.Dead)
            {
                state.LastAggressor = aggressor;
                if (!state.Intruders.Contains(aggressor))
                {
                    state.Intruders.Add(aggressor);
                }
            }
            if (!wasAlarmed)
            {
                SpawnBoomAntsToCap(state);
                state.NextBoomWaveTick = ticks + Settings.boomWaveCooldownTicks;
            }
        }

        //函数职责：蚁穴被摧毁时停止补员、触发狂暴并无视冷却补充最后一波爆浆蚁。
        public void NotifyNestDestroyed(Building_DesertPitAntNest nest, int colonyId)
        {
            AntColonyState state;
            if (!coloniesById.TryGetValue(colonyId, out state) || state.NestDestroyed)
            {
                return;
            }

            state.NestPosition = nest.Position;
            state.NestDestroyed = true;
            state.Frenzy = true;
            CancelInvestigation(state);
            SpawnBoomAntsToCap(state);
        }

        //函数职责：判断指定 Pawn 是否属于地图组件管理的任意蚁巢。
        public bool IsManagedAnt(Pawn pawn)
        {
            return pawn != null && coloniesByPawn.ContainsKey(pawn);
        }

        //函数职责：判断指定 Pawn 是否属于给定巢群，从而只排除真正的同巢成员。
        public bool IsColonyMember(Pawn pawn, AntColonyState state)
        {
            AntColonyState memberState;
            return pawn != null && state != null && coloniesByPawn.TryGetValue(pawn, out memberState) && memberState == state;
        }

        //函数职责：取得指定蚂蚁所属的巢群状态。
        public bool TryGetColony(Pawn pawn, out AntColonyState state)
        {
            if (pawn == null)
            {
                state = null;
                return false;
            }

            return coloniesByPawn.TryGetValue(pawn, out state);
        }

        //函数职责：返回指定巢群当前是否仍处于蚁穴受击后的完整警报阶段。
        public bool IsFullAlarm(AntColonyState state, int ticks)
        {
            return !state.NestDestroyed && state.LastNestDamageTick >= 0 && ticks < state.LastNestDamageTick + Settings.fullAlarmDurationTicks;
        }

        //函数职责：结算爆浆蚁酸液爆炸，并只让爆炸来源所属巢群的成员和蚁穴免疫。
        public void ExplodeBoomAnt(Pawn boomAnt)
        {
            List<Thing> ignoredThings = new List<Thing>();
            AntColonyState state;
            if (TryGetColony(boomAnt, out state))
            {
                if (state.Nest != null && !state.Nest.Destroyed)
                {
                    ignoredThings.Add(state.Nest);
                }

                for (int i = 0; i < state.Members.Count; i++)
                {
                    Pawn member = state.Members[i];
                    if (member != null && !member.Destroyed)
                    {
                        ignoredThings.Add(member);
                    }
                }
            }

            GenExplosion.DoExplosion(
                boomAnt.Position,
                map,
                Settings.boomExplosionRadius,
                DamageDefOf.AcidBurn,
                boomAnt,
                Settings.boomExplosionDamage,
                damageFalloff: true,
                chanceToStartFire: 0f,
                ignoredThings: ignoredThings);
        }

        //函数职责：重建巢群编号、成员和蚁穴的运行时查询索引。
        private void RebuildRuntimeIndices()
        {
            coloniesById.Clear();
            coloniesByPawn.Clear();
            coloniesByNest.Clear();
            for (int i = 0; i < colonies.Count; i++)
            {
                IndexColony(colonies[i]);
            }
        }

        //函数职责：把单个巢群及其仍有效的实体引用加入运行时索引。
        private void IndexColony(AntColonyState state)
        {
            coloniesById[state.Id] = state;
            if (state.Nest != null && !state.Nest.Destroyed)
            {
                coloniesByNest[state.Nest] = state;
            }

            for (int i = 0; i < state.Members.Count; i++)
            {
                Pawn member = state.Members[i];
                if (member != null && !member.Destroyed && !member.Dead)
                {
                    coloniesByPawn[member] = state;
                }
            }
        }
    }
}
