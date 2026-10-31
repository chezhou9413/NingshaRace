using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Config;
using NingshaRaceLib.DesertPit.AntColony.Core;
using NingshaRaceLib.DesertPit.AntColony.Effects;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //工蚁每半天独立挖出一批资源，使用真实任务和地图物品。
    public partial class MapComponent_DesertPitAntColonies
    {
        //完成挖掘后随机结算一次产物，并从此次完成时间重新计算半天间隔。
        public void CompleteResourceDig(Pawn pawn)
        {
            Comp_DesertPitAntMember member = pawn.GetComp<Comp_DesertPitAntMember>();
            if (member.Caste != AntCaste.Worker || !TryGetColony(pawn, out State.AntColonyState state)
                || Find.TickManager.TicksGame < member.NextResourceDigTick) return;
            member.NextResourceDigTick = Find.TickManager.TicksGame + Settings.workerResourceIntervalTicks;
            AntWorkerResourceYield output = Settings.workerResourceYields.RandomElementByWeight(entry => entry.weight);
            AntColonyWorkEffects.ResourceEmerges(pawn);
            if (output.thingDef == null) return;
            Thing resource = ThingMaker.MakeThing(output.thingDef);
            resource.stackCount = output.count;
            resource.SetForbidden(true, false);
            if (!GenPlace.TryPlaceThing(resource, pawn.Position, map, ThingPlaceMode.Near, out Thing placed))
                throw new System.InvalidOperationException("工蚁挖出的资源无法放到地图上。");
            TryPlanHaul(state, placed, true, 0, pawn);
        }
    }
}
