using RimWorld;
using Verse;
using Verse.AI;

using NingshaRaceLib.Altar.Components;

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
            return AltarOfferingJobUtility.TryFindBestAltar(pawn, thing, forced, out _);
        }

        //函数职责：创建把足量生肉送往最近可用祭坛的搬运工作。
        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            if (!AltarOfferingJobUtility.TryFindBestAltar(pawn, thing, forced, out Thing altar))
            {
                return null;
            }
            CompAltarOffering comp = altar.TryGetComp<CompAltarOffering>();
            return AltarOfferingJobUtility.MakeFillJob(thing, altar, comp);
        }
    }
}
