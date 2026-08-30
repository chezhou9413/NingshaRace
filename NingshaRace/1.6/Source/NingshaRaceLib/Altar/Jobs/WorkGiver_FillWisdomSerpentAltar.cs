using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

using NingshaRaceLib.Altar.Components;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Altar.Jobs
{
    //类职责：让搬运者为未充满的智慧之蛇祭坛寻找最近的合法生肉供品。
    public sealed class WorkGiver_FillWisdomSerpentAltar : WorkGiver_Scanner
    {
        //属性职责：让搬运工作扫描地图上可搬运的原料物品。
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.HaulableEver);

        //属性职责：规定搬运者接触生肉时采用最近接触寻路。
        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        //函数职责：检查生肉是否可预留，并确认地图上存在可达且未充满的祭坛。
        public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            return CompAltarOffering.IsAcceptedRawMeat(thing)
                && thing.Spawned
                && !thing.IsForbidden(pawn)
                && pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, Danger.Deadly, 1, -1, null, forced)
                && FindBestAltar(pawn, forced) != null;
        }

        //函数职责：创建把足量生肉送往最近可用祭坛的搬运工作。
        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            Thing altar = FindBestAltar(pawn, forced);
            if (altar == null)
            {
                return null;
            }
            CompAltarOffering comp = altar.TryGetComp<CompAltarOffering>();
            float nutrition = thing.GetStatValue(StatDefOf.Nutrition);
            Job job = JobMaker.MakeJob(DefOfRefs.NingshaRace_Job_FillWisdomSerpentAltar, thing, altar);
            job.count = System.Math.Min(thing.stackCount, UnityEngine.Mathf.CeilToInt(comp.MissingNutrition / nutrition));
            return job;
        }

        //函数职责：从殖民者祭坛中筛出可接收供品的最近目标。
        private static Thing FindBestAltar(Pawn pawn, bool forced)
        {
            List<Thing> altars = pawn.Map.listerThings.ThingsOfDef(DefOfRefs.NingshaRace_Altar)
                .Where(altar => altar.TryGetComp<CompAltarOffering>()?.CanAcceptOffering == true
                    && !altar.IsForbidden(pawn)
                    && pawn.CanReserveAndReach(altar, PathEndMode.Touch, Danger.Deadly, 1, 1, null, forced))
                .ToList();
            return GenClosest.ClosestThing_Global_Reachable(
                pawn.Position, pawn.Map, altars, PathEndMode.Touch, TraverseParms.For(pawn), 9999f);
        }
    }
}
