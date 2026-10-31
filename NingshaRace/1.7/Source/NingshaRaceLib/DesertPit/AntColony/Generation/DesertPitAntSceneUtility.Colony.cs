using System.Collections.Generic;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Buildings;
using NingshaRaceLib.DesertPit.AntColony.Components;
using NingshaRaceLib.DesertPit.AntColony.State;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Generation
{
    //沙漠巨坑、祭坛和墓葬只负责选场地，实体生成共用此入口。
    public static partial class DesertPitAntSceneUtility
    {
        //按场景给定规模建立独立阵营、全部蚁种、初始建筑和两天口粮。
        public static AntColonyState SpawnColony(Map map, ThingDef nestDef, IntVec3 center, List<IntVec3> storage,
            AntColonyPopulationSettings population, int level, int maxLevel, bool leveling, bool valuables, List<IntVec3> buildArea)
        {
            MapComponent_DesertPitAntColonies manager = map.GetComponent<MapComponent_DesertPitAntColonies>();
            if (!manager.TryPlanInitialBuildings(center, nestDef, storage, level, buildArea, out List<AntColonyTask> buildings))
                throw new System.InvalidOperationException("蚁巢初始建筑空间不足：" + center);
            AntColonyGround.Generate(map, center, buildArea, storage);
            Faction faction = manager.CreateColonyFaction();
            Building_DesertPitAntNest nest = (Building_DesertPitAntNest)ThingMaker.MakeThing(nestDef);
            nest.SetFaction(faction);
            GenSpawn.Spawn(nest, center, map);
            Pawn queen = SpawnInitialMember(map, center, DefOfRefs.NingshaRace_DesertPitQueenAntKind, faction);
            List<Pawn> members = new List<Pawn>();
            SpawnCaste(DefOfRefs.NingshaRace_DesertPitWorkerAntKind, population.WorkerTarget);
            SpawnCaste(DefOfRefs.NingshaRace_DesertPitSoldierAntKind, population.SoldierTarget);
            SpawnCaste(DefOfRefs.NingshaRace_DesertPitAcidAntKind, population.AcidTarget);
            SpawnCaste(DefOfRefs.NingshaRace_DesertPitBreakerAntKind, population.BreakerTarget);
            SpawnCaste(DefOfRefs.NingshaRace_DesertPitFlyingAntKind, population.FlyingTarget);
            AntColonyState state = manager.RegisterGeneratedColony(nest, queen, members, storage, faction, population, leveling, level, maxLevel, buildArea, buildings);
            List<Pawn> fed = new List<Pawn>(state.Members) { queen };
            SpawnInitialStock(map, storage, fed, valuables);
            manager.NotifyInventoryChanged(state.Id);
            return state;

            //同一生成规则用于不同阶级，不为墓葬保留另一套补员路径。
            void SpawnCaste(PawnKindDef kind, int count)
            {
                for (int i = 0; i < count; i++) members.Add(SpawnInitialMember(map, center, kind, faction));
            }
        }
    }
}
