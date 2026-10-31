using System;
using System.Collections.Generic;
using System.Linq;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Buildings;
using NingshaRaceLib.DesertPit.AntColony.Config;
using NingshaRaceLib.DesertPit.AntColony.State;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //建筑登记、选址和原石预留与全部场景共享。
    public partial class MapComponent_DesertPitAntColonies
    {
        //建筑与成员共用休眠状态和目标缓存。
        public bool StructureCanOperate(int id) => coloniesById.TryGetValue(id, out AntColonyState state) && !ColonyDormant(state);
        //按同一喷酸射程上下限选取可见入侵者。
        public Thing TurretTarget(int id, Thing turret, float range, float minRange)
        {
            if (!coloniesById.TryGetValue(id, out AntColonyState state)) return null;
            return state.Intruders.Where(target => target.Spawned && !target.Destroyed
                && !(target is Pawn pawn && (pawn.Dead || pawn.Downed))
                && target.Position.DistanceTo(turret.Position) <= range
                && target.Position.DistanceTo(turret.Position) >= minRange
                && GenSight.LineOfSight(turret.Position, target.Position, map, true))
                .OrderBy(target => target.Position.DistanceToSquared(turret.Position)).FirstOrDefault();
        }
        //建筑通过事件加入巢群，并保证至少一座储物穴优先存粮。
        public void RegisterStructure(Building_AntColonyStructure building)
        {
            if (!coloniesById.TryGetValue(building.ColonyId, out AntColonyState state)) return;
            if (!state.Buildings.Contains(building)) state.Buildings.Add(building);
            AssignFoodStorage(state);
            state.NutritionDirty = true;
        }

        //移除建筑会使所有引用它的工作立即失效。
        public void UnregisterStructure(Building_AntColonyStructure building)
        {
            if (!coloniesById.TryGetValue(building.ColonyId, out AntColonyState state)) return;
            state.Buildings.Remove(building);
            state.Tasks.RemoveAll(task => task.Target == building || task.Destination == building);
            AssignFoodStorage(state);
            state.NutritionDirty = true;
        }

        //粮仓被毁后把职责移交给剩余储物穴。
        private static void AssignFoodStorage(AntColonyState state)
        {
            Building_AntStoragePit pit = state.Buildings.OfType<Building_AntStoragePit>().FirstOrDefault();
            if (pit != null) pit.FoodOnly = true;
        }

        //按等级计算上限，与是否允许升级无关。
        private static int BuildingCap(AntColonyState state, ThingDef def) => def == DefOfRefs.NingshaRace_AntAcidTurret
            ? state.CurrentLevel * 2 : (def == DefOfRefs.NingshaRace_AntHoneyTower ? 2 : 1)
                * (1 + (state.CurrentLevel >= 3 ? 1 : 0) + (state.CurrentLevel >= 5 ? 1 : 0));

        //在生成任何实体前确认全部初始建筑能落地，场景保留通道不参与选址。
        public bool TryPlanInitialBuildings(IntVec3 center, ThingDef nestDef, List<IntVec3> storage,
            int level, List<IntVec3> buildArea, out List<AntColonyTask> placements)
        {
            CellRect nestRect = GenAdj.OccupiedRect(center, Rot4.North, nestDef.size);
            AntColonyState preview = new AntColonyState
            {
                NestPosition = center, StorageCells = storage,
                BuildArea = buildArea.Where(cell => !nestRect.Contains(cell)).ToList()
            };
            placements = preview.Tasks;
            List<ThingDef> defs = new List<ThingDef> { DefOfRefs.NingshaRace_AntStoragePit, DefOfRefs.NingshaRace_AntHoneyTower };
            for (int i = 0; i < level; i++) defs.Add(DefOfRefs.NingshaRace_AntAcidTurret);
            foreach (ThingDef def in defs)
            {
                if (!TryFindBuildingCell(preview, def, out IntVec3 cell)) return false;
                placements.Add(new AntColonyTask { Kind = AntTaskKind.Build, BuildingDef = def, Cell = cell });
            }
            return true;
        }

        //生成后自动登记建筑，蜜露塔在此一次性播种。
        private void SpawnStructure(AntColonyState state, ThingDef def, IntVec3 cell)
        {
            Generation.AntColonyGround.CoverBuilding(map, cell, def.size, state.BuildArea);
            Building_AntColonyStructure building = (Building_AntColonyStructure)ThingMaker.MakeThing(def);
            foreach (IntVec3 occupied in GenAdj.OccupiedRect(cell, Rot4.North, def.size)) occupied.GetPlant(map)?.Destroy();
            building.ColonyId = state.Id;
            building.SetFaction(state.Faction);
            GenSpawn.Spawn(building, cell, map);
            if (building is Building_AntHoneyTower tower) tower.SeedHabitat();
        }

        //有限范围内按距离选址，避免把储藏格或已有任务预留覆盖。
        private bool TryFindBuildingCell(AntColonyState state, ThingDef def, out IntVec3 result)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(state.NestPosition,
                def.GetModExtension<DefModExtension_AntStructure>().radius, true))
                if (CanBuildAt(state, def, cell)) { result = cell; return true; }
            result = IntVec3.Invalid;
            return false;
        }

        //用完整建筑占地检查障碍、储藏及其他在建任务。
        private bool CanBuildAt(AntColonyState state, ThingDef def, IntVec3 cell, AntColonyTask except = null)
        {
            CellRect rect = GenAdj.OccupiedRect(cell, Rot4.North, def.size);
            foreach (IntVec3 occupied in rect)
            {
                if (!state.BuildArea.Contains(occupied) || !occupied.InBounds(map) || !occupied.Standable(map) || occupied.GetEdifice(map) != null
                    || state.StorageCells.Contains(occupied) || IsColonyStorageCell(occupied) || occupied.GetTerrain(map).IsWater) return false;
                if (occupied.GetThingList(map).Any(thing => thing.def.category == ThingCategory.Item
                    || thing is Building || thing is Blueprint || thing is Frame)) return false;
                foreach (AntColonyTask task in state.Tasks)
                    if (task != except && task.Kind == AntTaskKind.Build
                        && GenAdj.OccupiedRect(task.Cell, Rot4.North, task.BuildingDef.size).Contains(occupied)) return false;
            }
            if (def == DefOfRefs.NingshaRace_AntHoneyTower && state.Buildings.Any(building =>
                building.def == def && building.Position.DistanceTo(cell) <= 3f)) return false;
            return true;
        }

        //只把天然原石块作为建材，可混用石种。
        private static bool IsStoneChunk(Thing thing) => thing.def.IsWithinCategory(ThingCategoryDefOf.StoneChunks);

        //枚举已入库原石，远处原石必须先通过搬运任务入库。
        private IEnumerable<Thing> StoredStone(AntColonyState state)
        {
            foreach (IntVec3 cell in state.StorageCells)
                foreach (Thing thing in cell.GetThingList(map)) if (IsStoneChunk(thing)) yield return thing;
            foreach (Building_AntStoragePit pit in state.Buildings.OfType<Building_AntStoragePit>())
                foreach (Thing thing in pit.Contents) if (IsStoneChunk(thing)) yield return thing;
        }

        //预留全部原石后才允许领取建造任务，失败时清空临时预留。
        private bool ReserveStone(AntColonyState state, AntColonyTask task)
        {
            task.Materials.Clear();
            task.MaterialCounts.Clear();
            int needed = task.BuildingDef.GetModExtension<DefModExtension_AntStructure>().stoneChunks;
            foreach (Thing stone in StoredStone(state))
            {
                int reserved = state.Tasks.Where(other => other != task && other.Worker != null)
                    .Sum(other => other.Materials.Contains(stone) ? other.MaterialCounts[other.Materials.IndexOf(stone)] : 0);
                int take = Math.Min(needed, stone.stackCount - reserved);
                if (take <= 0) continue;
                task.Materials.Add(stone);
                task.MaterialCounts.Add(take);
                needed -= take;
                if (needed == 0) return true;
            }
            task.Materials.Clear();
            task.MaterialCounts.Clear();
            return false;
        }

        //重新确认每份预留仍在本巢库存，再一起扣除，缺一份也不扣款。
        private bool PayStone(AntColonyState state, AntColonyTask task)
        {
            HashSet<Thing> stock = new HashSet<Thing>(StoredStone(state));
            if (task.Materials.Count == 0) return false;
            for (int i = 0; i < task.Materials.Count; i++)
                if (!stock.Contains(task.Materials[i]) || task.Materials[i].stackCount < task.MaterialCounts[i]) return false;
            for (int i = 0; i < task.Materials.Count; i++)
            {
                Thing stone = task.Materials[i];
                (stone.holdingOwner != null ? stone.holdingOwner.Take(stone, task.MaterialCounts[i])
                    : stone.SplitOff(task.MaterialCounts[i])).Destroy();
            }
            return true;
        }
    }
}
