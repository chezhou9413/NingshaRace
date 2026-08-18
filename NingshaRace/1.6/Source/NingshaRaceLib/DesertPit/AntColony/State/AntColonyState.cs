using System.Collections.Generic;
using RimWorld;
using Verse;

using NingshaRaceLib.DesertPit.AntColony.Buildings;

namespace NingshaRaceLib.DesertPit.AntColony.State
{
    //类职责：保存一个蚁巢在地图上的实体引用、有效规模、储藏位置、警戒状态和补员计时。
    public class AntColonyState : IExposable
    {
        public int Id;
        public Faction Faction;
        public Building_DesertPitAntNest Nest;
        public IntVec3 NestPosition;
        public Pawn Queen;
        public List<Pawn> Members = new List<Pawn>();
        public List<IntVec3> StorageCells = new List<IntVec3>();
        public AntColonyPopulationSettings Population;
        public bool NestDestroyed;
        public bool Frenzy;
        public int LastNestDamageTick = -1;
        public int NextBoomWaveTick = -1;
        public int NextBirthTick;
        public Pawn LastAggressor;
        public List<Thing> Intruders = new List<Thing>();

        //函数职责：把需要跨存档保留的蚁巢实体引用、位置和计时写入地图存档。
        public void ExposeData()
        {
            Scribe_Values.Look(ref Id, "id");
            Scribe_References.Look(ref Faction, "faction");
            Scribe_References.Look(ref Nest, "nest");
            Scribe_Values.Look(ref NestPosition, "nestPosition");
            Scribe_References.Look(ref Queen, "queen");
            Scribe_Collections.Look(ref Members, "members", LookMode.Reference);
            Scribe_Collections.Look(ref StorageCells, "storageCells", LookMode.Value);
            Scribe_Deep.Look(ref Population, "population");
            Scribe_Values.Look(ref NestDestroyed, "nestDestroyed");
            Scribe_Values.Look(ref Frenzy, "frenzy");
            Scribe_Values.Look(ref LastNestDamageTick, "lastNestDamageTick", -1);
            Scribe_Values.Look(ref NextBoomWaveTick, "nextBoomWaveTick", -1);
            Scribe_Values.Look(ref NextBirthTick, "nextBirthTick");
            Scribe_References.Look(ref LastAggressor, "lastAggressor");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Members = Members ?? new List<Pawn>();
                StorageCells = StorageCells ?? new List<IntVec3>();
                Intruders = new List<Thing>();
            }
        }
    }
}
