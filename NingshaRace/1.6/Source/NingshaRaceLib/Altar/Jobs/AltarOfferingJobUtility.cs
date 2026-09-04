using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

using NingshaRaceLib.Altar.Components;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Altar.Jobs
{
    //类职责：统一智慧之蛇祭坛自动搬运与玩家强制填充使用的目标检查和工作创建规则。
    public static class AltarOfferingJobUtility
    {
        //函数职责：为指定殖民者和祭坛检查全部条件并创建一次手动优先填充工作。
        public static bool TryMakeManualFillJob(Pawn pawn, Thing altar, out Job job, out string rejectReason)
        {
            job = null;
            rejectReason = null;
            if (pawn == null || !pawn.IsColonistPlayerControlled)
            {
                rejectReason = "需要选择可控制的殖民者";
                return false;
            }

            CompAltarOffering comp = altar?.TryGetComp<CompAltarOffering>();
            if (comp == null)
            {
                rejectReason = "目标不是智慧之蛇祭坛";
                return false;
            }
            if (!comp.OccupiedByPlayer)
            {
                rejectReason = "需要先占用祭坛";
                return false;
            }
            if (comp.Full)
            {
                rejectReason = "供奉已经充满";
                return false;
            }
            if (!comp.OfferingEnabled)
            {
                rejectReason = "已禁止供奉";
                return false;
            }
            if (altar.IsForbidden(pawn))
            {
                rejectReason = "祭坛已被禁用";
                return false;
            }
            if (!pawn.CanReach(altar, PathEndMode.Touch, Danger.Deadly))
            {
                rejectReason = "无法到达祭坛";
                return false;
            }
            if (!pawn.CanReserve(altar, 1, 1))
            {
                rejectReason = "祭坛已被预定";
                return false;
            }

            Thing meat = FindClosestRawMeat(pawn, out rejectReason);
            if (meat == null)
            {
                return false;
            }

            job = MakeFillJob(meat, altar, comp);
            return true;
        }

        //函数职责：检查扫描到的生肉并寻找该殖民者当前最近的可用祭坛。
        public static bool TryFindBestAltar(Pawn pawn, Thing meat, bool forced, out Thing altar)
        {
            altar = null;
            if (!CanUseRawMeat(pawn, meat, forced))
            {
                return false;
            }

            List<Thing> altars = pawn.Map.listerThings.ThingsOfDef(DefOfRefs.NingshaRace_Altar)
                .Where(candidate => candidate.TryGetComp<CompAltarOffering>()?.CanAcceptOffering == true
                    && !candidate.IsForbidden(pawn)
                    && pawn.CanReserveAndReach(candidate, PathEndMode.Touch, Danger.Deadly, 1, 1, null, forced))
                .ToList();
            altar = GenClosest.ClosestThing_Global_Reachable(
                pawn.Position, pawn.Map, altars, PathEndMode.Touch, TraverseParms.For(pawn), 9999f);
            return altar != null;
        }

        //函数职责：使用指定生肉与祭坛缺失营养创建最小搬运数量的填充工作。
        public static Job MakeFillJob(Thing meat, Thing altar, CompAltarOffering comp)
        {
            float nutrition = meat.GetStatValue(StatDefOf.Nutrition);
            Job job = JobMaker.MakeJob(DefOfRefs.NingshaRace_Job_FillWisdomSerpentAltar, meat, altar);
            job.count = System.Math.Min(meat.stackCount, Mathf.CeilToInt(comp.MissingNutrition / nutrition));
            return job;
        }

        //函数职责：判断指定物品是否是祭坛接受的可食用生肉。
        public static bool IsAcceptedRawMeat(Thing thing)
        {
            if (thing == null || thing is Corpse || thing.def.ingestible == null)
            {
                return false;
            }

            FoodTypeFlags foodType = thing.def.ingestible.foodType;
            return (foodType & FoodTypeFlags.Meat) != 0
                && thing.def.thingCategories != null
                && thing.def.thingCategories.Exists(category => category.defName == "MeatRaw");
        }

        //函数职责：检查自动工作扫描到的生肉是否可由指定殖民者接近并预定。
        private static bool CanUseRawMeat(Pawn pawn, Thing meat, bool forced)
        {
            return pawn?.Map != null
                && IsAcceptedRawMeat(meat)
                && meat.Spawned
                && !meat.IsForbidden(pawn)
                && pawn.CanReserveAndReach(meat, PathEndMode.ClosestTouch, Danger.Deadly, 1, -1, null, forced);
        }

        //函数职责：按禁用、寻路与预定顺序筛选最近生肉，并返回可直接展示的主要阻塞原因。
        private static Thing FindClosestRawMeat(Pawn pawn, out string rejectReason)
        {
            rejectReason = null;
            List<Thing> rawMeat = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver)
                .Where(thing => IsAcceptedRawMeat(thing) && thing.Spawned)
                .ToList();
            if (rawMeat.Count == 0)
            {
                rejectReason = "地图上没有可用生肉";
                return null;
            }

            List<Thing> allowed = rawMeat.Where(thing => !thing.IsForbidden(pawn)).ToList();
            if (allowed.Count == 0)
            {
                rejectReason = "生肉均已被禁用";
                return null;
            }

            List<Thing> reachable = allowed.Where(thing => pawn.CanReach(thing, PathEndMode.ClosestTouch, Danger.Deadly)).ToList();
            if (reachable.Count == 0)
            {
                rejectReason = "没有可到达的生肉";
                return null;
            }

            List<Thing> reservable = reachable.Where(thing => pawn.CanReserve(thing, 1, -1)).ToList();
            if (reservable.Count == 0)
            {
                rejectReason = "生肉均已被预定";
                return null;
            }

            return GenClosest.ClosestThing_Global_Reachable(
                pawn.Position, pawn.Map, reservable, PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f);
        }
    }
}
