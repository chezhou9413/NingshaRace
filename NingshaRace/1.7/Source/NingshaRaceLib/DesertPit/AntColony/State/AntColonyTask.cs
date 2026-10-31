using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.State
{
    //任务类型决定执行者、目标和结算方式。
    public enum AntTaskKind { Hatch, Build, Repair, Upgrade, Haul, Dig, Mine }

    //巢群持有任务和物资预留，Pawn 只引用自己正在执行的任务。
    public sealed class AntColonyTask : IExposable
    {
        public AntTaskKind Kind;
        public Pawn Worker;
        public Pawn PreferredWorker;
        public Thing Target;
        public Thing Destination;
        public IntVec3 Cell = IntVec3.Invalid;
        public ThingDef BuildingDef;
        public PawnKindDef HatchKind;
        public float Nutrition;
        public float Work;
        public float Progress;
        public int Count;
        public int Priority;
        public bool FreeHaul;
        public List<Thing> Materials = new List<Thing>();
        public List<int> MaterialCounts = new List<int>();

        //保存任务进度及实体引用，读档后恢复同一份预留。
        public void ExposeData()
        {
            Scribe_Values.Look(ref Kind, "kind");
            Scribe_References.Look(ref Worker, "worker");
            Scribe_References.Look(ref PreferredWorker, "preferredWorker");
            Scribe_References.Look(ref Target, "target");
            Scribe_References.Look(ref Destination, "destination");
            Scribe_Values.Look(ref Cell, "cell", IntVec3.Invalid);
            Scribe_Defs.Look(ref BuildingDef, "buildingDef");
            Scribe_Defs.Look(ref HatchKind, "hatchKind");
            Scribe_Values.Look(ref Nutrition, "nutrition");
            Scribe_Values.Look(ref Work, "work");
            Scribe_Values.Look(ref Progress, "progress");
            Scribe_Values.Look(ref Count, "count");
            Scribe_Values.Look(ref Priority, "priority");
            Scribe_Values.Look(ref FreeHaul, "freeHaul");
            Scribe_Collections.Look(ref Materials, "materials", LookMode.Reference);
            Scribe_Collections.Look(ref MaterialCounts, "materialCounts", LookMode.Value);
        }
    }
}
