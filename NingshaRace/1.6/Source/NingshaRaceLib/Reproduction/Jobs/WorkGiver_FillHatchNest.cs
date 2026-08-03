using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Reproduction.Buildings;

namespace NingshaRaceLib.Reproduction.Jobs
{
    //类职责：为地图上的凝砂卵寻找允许装填且为空的孵化巢。
    public class WorkGiver_FillHatchNest : WorkGiver_Scanner
    {
        //属性职责：让工作扫描器检查地图上的可搬运物品。
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.HaulableEver);

        //属性职责：规定搬运者接触凝砂卵时使用最近接触寻路模式。
        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        //函数职责：检查卵、搬运者和目标孵化巢是否都满足工作条件。
        public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            if (!Building_NingshaHatchNest.IsNingshaEgg(thing) || !thing.Spawned || thing.IsForbidden(pawn))
            {
                return false;
            }
            if (!pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, Danger.Deadly, 1, -1, null, forced))
            {
                return false;
            }
            return FindBestNest(pawn, thing, forced) != null;
        }

        //函数职责：为指定凝砂卵创建只搬运一枚到最佳孵化巢的工作。
        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            Building_NingshaHatchNest nest = FindBestNest(pawn, thing, forced);
            if (nest == null)
            {
                return null;
            }
            Job job = JobMaker.MakeJob(DefOfRefs.NingshaRace_Job_PlaceEggInHatchNest, thing, nest);
            job.count = 1;
            return job;
        }

        //函数职责：筛选自动装填或玩家强制允许的空巢，并按可达距离选择最近目标。
        private static Building_NingshaHatchNest FindBestNest(Pawn pawn, Thing egg, bool forced)
        {
            List<Building_NingshaHatchNest> nests = pawn.Map.listerBuildings
                .AllBuildingsColonistOfClass<Building_NingshaHatchNest>()
                .Where(nest => nest.Empty
                    && (forced || nest.AllowAutoLoad)
                    && !nest.IsForbidden(pawn)
                    && pawn.CanReserveAndReach(nest, PathEndMode.Touch, Danger.Deadly, 1, 1, null, forced))
                .ToList();
            return GenClosest.ClosestThing_Global_Reachable(
                egg.Position,
                pawn.Map,
                nests,
                PathEndMode.Touch,
                TraverseParms.For(pawn),
                9999f) as Building_NingshaHatchNest;
        }
    }
}
