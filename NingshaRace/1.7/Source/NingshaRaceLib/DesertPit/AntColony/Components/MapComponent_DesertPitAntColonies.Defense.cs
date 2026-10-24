using NingshaRaceLib.DesertPit.AntColony.Core;
using NingshaRaceLib.DesertPit.AntColony.State;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //处理成员身边的可见敌人，并持续约束撤退阶段的攻击范围。
    public partial class MapComponent_DesertPitAntColonies
    {
        //战斗成员遇敌时临时打断移动职责，敌人消失后继续原有领主任务。
        public Job TryCreateEncounterJob(Pawn pawn)
        {
            if (!TryGetColony(pawn, out AntColonyState state)) return null;
            AntCaste caste = pawn.GetComp<Comp_DesertPitAntMember>().Caste;
            if (caste != AntCaste.Soldier && caste != AntCaste.Acid && caste != AntCaste.Boom) return null;
            Thing nearest = null;
            float bestDistance = Settings.alertRadius * Settings.alertRadius;
            foreach (Pawn target in map.mapPawns.AllPawnsSpawned)
            {
                if (target.Dead || target.Downed || IsColonyMember(target, state)) continue;
                float distance = pawn.Position.DistanceToSquared(target.Position);
                if (distance > bestDistance || !GenSight.LineOfSight(pawn.Position, target.Position, map, skipFirstCell: true)
                    || !CanContinueColonyAttack(pawn, target) || !CanEngageIntruder(pawn, target)) continue;
                nearest = target;
                bestDistance = distance;
            }
            if (nearest == null) return null;
            return caste == AntCaste.Boom ? CreateBoomAttackJob(nearest) : CreateCombatJob(pawn, nearest);
        }

        //撤退防御以蚁穴为中心限制十五格，并在敌人被墙遮挡时停止追击。
        public bool CanContinueColonyAttack(Pawn member, Thing target)
        {
            if (target == null || !target.Spawned || target.Map != map || target.Destroyed
                || target is Pawn victim && (victim.Dead || victim.Downed)) return false;
            if (!TryGetColony(member, out AntColonyState state)) return false;
            if (!IsRetreating(state, Find.TickManager.TicksGame)) return true;
            return target.Position.DistanceToSquared(state.NestPosition)
                    <= Settings.retreatDefenseRadius * Settings.retreatDefenseRadius
                && GenSight.LineOfSight(member.Position, target.Position, map, skipFirstCell: true);
        }
    }
}
