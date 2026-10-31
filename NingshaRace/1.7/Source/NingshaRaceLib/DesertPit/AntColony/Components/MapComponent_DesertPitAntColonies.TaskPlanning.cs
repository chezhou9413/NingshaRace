using System.Linq;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Buildings;
using NingshaRaceLib.DesertPit.AntColony.Core;
using NingshaRaceLib.DesertPit.AntColony.State;
using Verse;
using Verse.AI.Group;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //每巢六十 tick 规划一次，Pawn 请求工作时只领取已有任务。
    public partial class MapComponent_DesertPitAntColonies
    {
        //先清理无效任务，再按建筑、维修、升级与物资优先级补足列表。
        private void UpdateTasks(AntColonyState state, int now)
        {
            state.Tasks.RemoveAll(task => task.Worker != null && (task.Worker.Dead || !task.Worker.Spawned
                || task.Worker.CurJobDef != DefOfRefs.NingshaRace_Job_AntWork
                && task.Worker.CurJobDef != DefOfRefs.NingshaRace_Job_AntMine
                && task.Worker.CurJobDef != DefOfRefs.NingshaRace_Job_DesertPitAntHaul)
                || task.Worker == null && task.Target != null && task.Target.Destroyed);
            if (IsFullAlarm(state, now) || IsRetreating(state, now) || ColonyDormant(state)) return;
            if (state.Queen?.Spawned == true && now >= state.NextBirthTick
                && !state.Tasks.Any(task => task.Kind == AntTaskKind.Hatch)
                && TryGetMissingRegularCaste(state, out PawnKindDef kind, out float cost))
                state.Tasks.Add(new AntColonyTask { Kind = AntTaskKind.Hatch, Target = state.Nest,
                    Cell = state.NestPosition, HatchKind = kind, Nutrition = cost, Work = Settings.reproductionWorkTicks });

            PlanBuilding(state, DefOfRefs.NingshaRace_AntHoneyTower, 10);
            PlanBuilding(state, DefOfRefs.NingshaRace_AntAcidTurret, 20);
            PlanBuilding(state, DefOfRefs.NingshaRace_AntStoragePit, 30);
            PlanRepair(state, state.Nest, 40, now);
            if (state.LevelingEnabled && state.CurrentLevel < state.MaxLevel && now >= state.NextUpgradeTick
                && state.Queen != null && !state.Queen.Dead && !state.Tasks.Any(task => task.Kind == AntTaskKind.Upgrade))
                state.Tasks.Add(new AntColonyTask { Kind = AntTaskKind.Upgrade, Target = state.Nest,
                    Cell = state.NestPosition, Nutrition = Settings.GetUpgradeNutrition(state.CurrentLevel),
                    Work = Settings.upgradeWork, Priority = 50 });
            foreach (Building_AntColonyStructure building in state.Buildings) PlanRepair(state, building, 60, now);
            PlanHauling(state);
            foreach (Pawn worker in state.Members)
                if (worker.Spawned && !worker.Dead && !worker.Downed
                    && worker.GetComp<Comp_DesertPitAntMember>().Caste == AntCaste.Worker
                    && now >= worker.GetComp<Comp_DesertPitAntMember>().NextResourceDigTick
                    && !state.Tasks.Any(task => task.Kind == AntTaskKind.Dig && task.Target == worker))
                    state.Tasks.Add(new AntColonyTask { Kind = AntTaskKind.Dig, Target = worker,
                        Cell = worker.Position, Work = 180, Priority = 90 });
        }

        //墓葬初始领主仍休眠时不生产、派遣或补员。
        private static bool ColonyDormant(AntColonyState state) => state.Queen?.Spawned == true && !state.Queen.mindState.Active;

        //每种建筑同时只有一个施工选址，达到上限后停止补建。
        private void PlanBuilding(AntColonyState state, ThingDef def, int priority)
        {
            if (state.Tasks.Any(task => task.Kind == AntTaskKind.Build && task.BuildingDef == def)
                || state.Buildings.Count(building => building.def == def) >= BuildingCap(state, def)) return;
            if (TryFindBuildingCell(state, def, out IntVec3 cell))
                state.Tasks.Add(new AntColonyTask { Kind = AntTaskKind.Build, BuildingDef = def, Cell = cell,
                    Work = def.GetModExtension<Config.DefModExtension_AntStructure>().work, Priority = priority });
        }

        //损伤不足一百时按实际修复量折算营养。
        private void PlanRepair(AntColonyState state, Thing building, int priority, int now)
        {
            if (!RepairAllowed(state, building, now) || building.HitPoints >= building.MaxHitPoints
                || state.Tasks.Any(task => task.Kind == AntTaskKind.Repair && task.Target == building)) return;
            int amount = System.Math.Min(Settings.repairHitPoints, building.MaxHitPoints - building.HitPoints);
            state.Tasks.Add(new AntColonyTask { Kind = AntTaskKind.Repair, Target = building, Cell = building.Position,
                Count = amount, Nutrition = amount * Settings.repairNutritionCost / Settings.repairHitPoints,
                Work = (80f + amount * 20f) / 1.7f, Priority = priority });
        }

        //蚁巢与其他建筑都保留受击后的等待。
        private bool RepairAllowed(AntColonyState state, Thing building, int now) => building?.Spawned == true
            && (building == state.Nest ? now >= state.NextRepairTick : building is Building_AntColonyStructure structure
                && now >= structure.LastDamageTick + Settings.repairDelayAfterDamageTicks);
    }
}
