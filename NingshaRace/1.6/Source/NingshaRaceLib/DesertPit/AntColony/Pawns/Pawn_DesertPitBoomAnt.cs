using Verse;

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
                Map.GetComponent<MapComponent_DesertPitAntColonies>().ExplodeBoomAnt(this);
            }

            base.Kill(dinfo, exactCulprit);
            if (Corpse != null && !Corpse.Destroyed)
            {
                Corpse.Destroy();
            }
        }
    }
}
