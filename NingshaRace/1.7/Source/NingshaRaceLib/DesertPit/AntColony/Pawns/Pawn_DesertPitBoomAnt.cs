using Verse;
using NingshaRaceLib.Core.Defs;

using NingshaRaceLib.DesertPit.AntColony.Components;

namespace NingshaRaceLib.DesertPit.AntColony.Pawns
{
    //类职责：保证爆浆蚁主动引爆或被击杀时都只结算一次酸液爆炸。
    public class Pawn_DesertPitBoomAnt : Pawn
    {
        private bool detonated;

        //函数职责：让抵达目标的爆浆蚁立即进入带爆炸的死亡流程。
        public void Detonate()
        {
            if (!Dead)
            {
                Kill(null);
            }
        }

        //函数职责：在基础死亡流程前结算酸液爆炸，并清除爆浆蚁遗体。
        public override void Kill(DamageInfo? dinfo, Hediff exactCulprit = null)
        {
            if (!detonated && Spawned)
            {
                detonated = true;
                Map previousMap = Map;
                IntVec3 previousCell = Position;
                previousMap.GetComponent<MapComponent_DesertPitAntColonies>().ExplodeBoomAnt(this);
                Thing honey = ThingMaker.MakeThing(DefOfRefs.NingshaRace_BurstHoneydew);
                honey.stackCount = 3;
                if (!GenPlace.TryPlaceThing(honey, previousCell, previousMap, ThingPlaceMode.Near))
                    throw new System.InvalidOperationException("爆浆蜜露无法落地。");
            }

            base.Kill(dinfo, exactCulprit);
            if (Corpse != null && !Corpse.Destroyed)
            {
                Corpse.Destroy();
            }
        }

        //单次结算标记随 Pawn 存档。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref detonated, "detonated");
        }
    }
}
