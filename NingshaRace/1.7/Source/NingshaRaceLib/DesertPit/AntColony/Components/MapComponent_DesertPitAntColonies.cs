using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Buildings;
using NingshaRaceLib.DesertPit.AntColony.Config;
using NingshaRaceLib.DesertPit.AntColony.Core;
using NingshaRaceLib.DesertPit.AntColony.State;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //地图唯一管理器，巢群各自持有成员、建筑、库存账目和任务。
    public partial class MapComponent_DesertPitAntColonies : MapComponent
    {
        private List<AntColonyState> colonies = new List<AntColonyState>();
        private int nextColonyId = 1;
        private readonly Dictionary<int, AntColonyState> coloniesById = new Dictionary<int, AntColonyState>();
        private readonly Dictionary<Pawn, AntColonyState> coloniesByPawn = new Dictionary<Pawn, AntColonyState>();
        private readonly Dictionary<Building_DesertPitAntNest, AntColonyState> coloniesByNest = new Dictionary<Building_DesertPitAntNest, AntColonyState>();
        private readonly List<Thing> forageCandidates = new List<Thing>();
        private DefModExtension_AntColony Settings => DefOfRefs.NingshaRace_DesertPitAntNest.GetModExtension<DefModExtension_AntColony>();

        //绑定当前地图。
        public MapComponent_DesertPitAntColonies(Map map) : base(map) { }

        //巢群保存引用，建筑容器深度保存内部实体。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref colonies, "antColonies", LookMode.Deep);
            Scribe_Values.Look(ref nextColonyId, "nextAntColonyId", 1);
        }

        //读档后重建索引，任务预留直接来源于已保存任务。
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            foreach (AntColonyState state in colonies) IndexColony(state);
            RefreshForageCandidates();
        }

        //每巢独立错峰六十 tick，全地图共用三百 tick 物资缓存。
        public override void MapComponentTick()
        {
            if (colonies.Count == 0) return;
            int ticks = Find.TickManager.TicksGame;
            UpdateColonies(ticks);
            if (ticks % 300 == map.uniqueID % 300) RefreshForageCandidates();
        }

        //三个场景共用登记与初始化，成员列表不包含蚁后。
        public AntColonyState RegisterGeneratedColony(Building_DesertPitAntNest nest, Pawn queen,
            List<Pawn> members, List<IntVec3> storageCells, Faction faction, AntColonyPopulationSettings population,
            bool levelingEnabled, int currentLevel, int maximumLevel, List<IntVec3> buildArea, List<AntColonyTask> initialBuildings)
        {
            AntColonyState state = new AntColonyState
            {
                Id = nextColonyId++, Faction = faction, Nest = nest, NestPosition = nest.Position, Queen = queen,
                Members = members.FindAll(pawn => pawn != queen), StorageCells = new List<IntVec3>(storageCells), BuildArea = buildArea,
                Population = population, LevelingEnabled = levelingEnabled, CurrentLevel = currentLevel,
                MaxLevel = maximumLevel, NextRepairTick = Find.TickManager.TicksGame + Settings.repairIntervalTicks,
                NextBirthTick = Find.TickManager.TicksGame + Settings.reproductionCooldownTicks
            };
            nest.AssignColony(state.Id);
            queen.GetComp<Comp_DesertPitAntMember>().AssignColony(state.Id);
            foreach (Pawn member in state.Members) member.GetComp<Comp_DesertPitAntMember>().AssignColony(state.Id);
            colonies.Add(state);
            IndexColony(state);
            foreach (AntColonyTask placement in initialBuildings) SpawnStructure(state, placement.BuildingDef, placement.Cell);
            SpawnBoomAntsToCap(state);
            foreach (Pawn member in state.Members.ToArray())
                if (member.GetComp<Comp_DesertPitAntMember>().Caste == AntCaste.Boom) nest.Enter(member);
            state.NextBoomWaveTick = Find.TickManager.TicksGame + Settings.boomWaveCooldownTicks;
            return state;
        }

        //成员生成、出巢和读档使用相同登记入口。
        public void NotifyMemberSpawned(Pawn pawn, int colonyId)
        {
            if (!coloniesById.TryGetValue(colonyId, out AntColonyState state)) return;
            if (pawn.GetComp<Comp_DesertPitAntMember>().Caste == AntCaste.Queen) state.Queen = pawn;
            else if (!state.Members.Contains(pawn)) state.Members.Add(pawn);
            coloniesByPawn[pawn] = state;
            state.NutritionDirty = true;
        }

        //死亡只登记一次，死亡与销毁事件重复到达也不会重复计数。
        public void NotifyMemberDestroyed(Pawn pawn, int colonyId)
        {
            if (!coloniesById.TryGetValue(colonyId, out AntColonyState state)
                || !coloniesByPawn.Remove(pawn)) return;
            ReleaseTaskInState(state, pawn);
            if (pawn.Dead) RecordRegularAntDeath(state, pawn, pawn.GetComp<Comp_DesertPitAntMember>().Caste);
            state.Members.Remove(pawn);
            if (state.Queen == pawn) state.Queen = null;
            state.NutritionDirty = true;
        }

        //恢复蚁巢索引。
        public void NotifyNestSpawned(Building_DesertPitAntNest nest, int colonyId)
        {
            if (!coloniesById.TryGetValue(colonyId, out AntColonyState state)) return;
            state.Nest = nest;
            coloniesByNest[nest] = state;
        }

        //蚁巢或蚁后受击启动同一警报，取消行军及工作。
        public void NotifyNestDamaged(Building_DesertPitAntNest nest, int colonyId, Thing aggressor)
        {
            if (!coloniesById.TryGetValue(colonyId, out AntColonyState state)) return;
            int now = Find.TickManager.TicksGame;
            bool wasAlarmed = IsFullAlarm(state, now);
            state.LastNestDamageTick = now;
            state.NextRepairTick = System.Math.Max(state.NextRepairTick, now + Settings.repairDelayAfterDamageTicks);
            CancelInvestigation(state);
            RememberAggressor(state, aggressor);
            if (!wasAlarmed)
            {
                state.Tasks.Clear();
                InterruptAllMembers(state);
                if (state.Queen?.Spawned == true) state.Queen.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }

        //巢毁先释放内部成员，再移除状态；存活成员保留阵营并转入无巢行为。
        public void NotifyNestDestroyed(Building_DesertPitAntNest nest, int colonyId)
        {
            if (!coloniesById.TryGetValue(colonyId, out AntColonyState state)) return;
            state.NestDestroyed = true;
            state.Tasks.Clear();
            CancelInvestigation(state);
            nest.ReleaseAll(map, state.NestPosition);
            List<Pawn> survivors = new List<Pawn>(state.Members);
            if (state.Queen != null) survivors.Add(state.Queen);
            foreach (Pawn pawn in survivors)
            {
                coloniesByPawn.Remove(pawn);
                pawn.GetComp<Comp_DesertPitAntMember>().AssignColony(0);
                if (pawn.Spawned && !pawn.Dead) pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
            foreach (Building_AntColonyStructure building in state.Buildings) building.ColonyId = 0;
            coloniesByNest.Remove(nest);
            coloniesById.Remove(state.Id);
            colonies.Remove(state);
        }

        //提供统一成员与巢群查询。
        public bool IsManagedAnt(Pawn pawn) => pawn != null && coloniesByPawn.ContainsKey(pawn);
        public bool IsColonyMember(Pawn pawn, AntColonyState state) => pawn != null
            && coloniesByPawn.TryGetValue(pawn, out AntColonyState other) && other == state;
        public bool TryGetColony(Pawn pawn, out AntColonyState state) => coloniesByPawn.TryGetValue(pawn, out state);
        public bool IsFullAlarm(AntColonyState state, int ticks) => state.LastNestDamageTick >= 0
            && ticks < state.LastNestDamageTick + Settings.fullAlarmDurationTicks;

        //爆浆只免伤同巢成员和建筑，无巢成员沿用其独立阵营辨认同伴。
        public void ExplodeBoomAnt(Pawn boomAnt)
        {
            List<Thing> ignored = new List<Thing>();
            if (TryGetColony(boomAnt, out AntColonyState state))
            {
                ignored.Add(state.Nest);
                if (state.Queen != null) ignored.Add(state.Queen);
                ignored.AddRange(state.Members);
                ignored.AddRange(state.Buildings);
            }
            else foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                if (pawn.Faction == boomAnt.Faction) ignored.Add(pawn);
            GenExplosion.DoExplosion(boomAnt.Position, map, Settings.boomExplosionRadius, DamageDefOf.AcidBurn,
                boomAnt, Settings.boomExplosionDamage, damageFalloff: true, chanceToStartFire: 0f, ignoredThings: ignored);
        }

        //只索引仍有效的成员；深存档由地图或建筑持有者负责。
        private void IndexColony(AntColonyState state)
        {
            coloniesById[state.Id] = state;
            coloniesByNest[state.Nest] = state;
            if (state.Queen != null && !state.Queen.Dead) coloniesByPawn[state.Queen] = state;
            foreach (Pawn member in state.Members)
                if (member != null && !member.Destroyed && !member.Dead) coloniesByPawn[member] = state;
        }
    }
}
