using System;
using System.Collections.Generic;
using NingshaRaceLib.DesertPit.AntColony.Components;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Buildings
{
    //蚁巢只保存原 Pawn 实例，内部成员不运行需求、健康或孵化。
    public partial class Building_DesertPitAntNest
    {
        private ThingOwner<Pawn> residents;
        public bool ShouldTickContents => false;
        public bool IsContentsSuspended => true;
        public ThingOwner<Pawn> Residents => residents;

        //允许蚁后与多只爆浆蚁同时入巢。
        public Building_DesertPitAntNest() => residents = new ThingOwner<Pawn>(this);
        public ThingOwner GetDirectlyHeldThings() => residents;
        public void GetChildHolders(List<IThingHolder> children) => ThingOwnerUtility.AppendThingHoldersFromThings(children, residents);

        //结束当前工作并转移唯一所有权，任务预留在离图前释放。
        public void Enter(Pawn pawn)
        {
            if (!pawn.Spawned || pawn.Dead || pawn.Downed || pawn.Map != Map) return;
            Map.GetComponent<MapComponent_DesertPitAntColonies>().ReleaseTask(pawn);
            if (pawn.CurJob != null) pawn.jobs.EndCurrentJob(JobCondition.Succeeded, startNewJob: false);
            pawn.DeSpawn();
            if (!residents.TryAddOrTransfer(pawn)) throw new InvalidOperationException("蚁巢不能接收成员：" + pawn);
        }

        //出巢使用可站立格，地表和容器之间始终只有一份 Pawn。
        public bool Release(Pawn pawn)
        {
            if (!residents.Contains(pawn)) return false;
            if (!CellFinder.TryFindRandomCellNear(Position, Map, 7,
                cell => cell.Standable(Map) && cell.GetFirstPawn(Map) == null, out IntVec3 cell, 150)) return false;
            if (!residents.TryDrop(pawn, cell, Map, ThingPlaceMode.Direct, out Pawn released))
                throw new InvalidOperationException("蚁巢无法释放成员：" + pawn);
            return true;
        }

        //巢毁时允许共享落点，保证拥挤场景也不会丢失内部成员。
        public void ReleaseAll(Map map, IntVec3 cell)
        {
            if (!residents.TryDropAll(cell, map, ThingPlaceMode.Near, nearPlaceValidator: candidate => candidate.Standable(map)))
                throw new InvalidOperationException("蚁巢摧毁后仍有无法释放的成员：" + this);
        }
    }
}
