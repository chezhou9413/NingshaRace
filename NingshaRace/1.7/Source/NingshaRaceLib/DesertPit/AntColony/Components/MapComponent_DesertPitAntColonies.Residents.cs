using NingshaRaceLib.Core.Defs;
using System.Linq;
using NingshaRaceLib.DesertPit.AntColony.Core;
using NingshaRaceLib.DesertPit.AntColony.State;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //统一警报护卫、入巢休眠与失巢行为。
    public partial class MapComponent_DesertPitAntColonies
    {
        //全部战斗蚁共用目标选择，爆浆蚁另有自爆流程。
        private static bool IsCombatCaste(AntCaste caste) => caste == AntCaste.Soldier || caste == AntCaste.Acid
            || caste == AntCaste.Breaker || caste == AntCaste.Flying;

        //警报释放爆浆蚁；平静后释放同一蚁后。墓葬唤醒后解除旧休眠领主。
        private void UpdateResidents(AntColonyState state, int now)
        {
            if (ColonyDormant(state)) return;
            Lord dormant = state.Queen?.GetLord();
            if (dormant?.LordJob is LordJob_SleepThenAssaultColony) map.lordManager.RemoveLord(dormant);
            bool alarm = IsFullAlarm(state, now);
            for (int i = state.Nest.Residents.Count - 1; i >= 0; i--)
            {
                Pawn pawn = state.Nest.Residents[i];
                AntCaste caste = pawn.GetComp<Comp_DesertPitAntMember>().Caste;
                if (caste == AntCaste.Queen ? !alarm : alarm || state.Intruders.Count > 0) state.Nest.Release(pawn);
            }
        }

        //蚁后入巢前按成员编号分组，入巢后所有成员守巢。
        private Job CreateGuardJob(Pawn pawn, AntColonyState state)
        {
            IntVec3 center = GuardCenter(pawn, state);
            if (CellFinder.TryFindRandomCellNear(center, map, 5,
                cell => cell.Standable(map) && pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly), out IntVec3 cell))
                return JobMaker.MakeJob(JobDefOf.Goto, cell);
            return JobMaker.MakeJob(JobDefOf.Wait, 120);
        }

        //护卫分组与交战范围使用同一锚点。
        private static IntVec3 GuardCenter(Pawn pawn, AntColonyState state) =>
            IsCombatCaste(pawn.GetComp<Comp_DesertPitAntMember>().Caste)
            && state.Queen?.Spawned == true && pawn.thingIDNumber % 2 == 0 ? state.Queen.Position : state.NestPosition;

        //失巢不再生成成员，全部蚁种使用共同猎杀人类流程。
        private Job TryCreateOrphanJob(Pawn pawn)
        {
            if (pawn.GetComp<Comp_DesertPitAntMember>() == null) return null;
            Pawn nearest = null;
            float distance = float.MaxValue;
            foreach (Pawn target in map.mapPawns.AllPawnsSpawned)
            {
                if (!target.RaceProps.Humanlike || target.Dead || target.Downed || target.Faction == pawn.Faction) continue;
                float next = pawn.Position.DistanceToSquared(target.Position);
                if (next < distance && CanEngageIntruder(pawn, target)) { nearest = target; distance = next; }
            }
            if (nearest != null) return pawn.GetComp<Comp_DesertPitAntMember>().Caste == AntCaste.Boom
                ? CreateBoomAttackJob(nearest) : CreateCombatJob(pawn, nearest);
            if (pawn.needs?.food?.CurLevelPercentage < 0.3f)
                foreach (Thing food in map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree))
                    if (IsStoredFood(food) && food.IngestibleNow && pawn.RaceProps.CanEverEat(food)
                        && pawn.CanReserveAndReach(food, PathEndMode.Touch, Danger.Deadly))
                    {
                        Job eat = JobMaker.MakeJob(JobDefOf.Ingest, food);
                        eat.count = food is Corpse ? 1 : System.Math.Min(food.stackCount,
                            FoodUtility.WillIngestStackCountOf(pawn, food.def, FoodUtility.NutritionForEater(pawn, food)));
                        return eat;
                    }
            if (pawn.needs?.rest?.CurLevelPercentage < 0.2f) return JobMaker.MakeJob(JobDefOf.LayDown, pawn.Position);
            return JobMaker.MakeJob(JobDefOf.Wait_Wander, 180);
        }
    }
}
