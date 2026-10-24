using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.GiantTomb.Discovery.Components;

namespace NingshaRaceLib.GiantTomb.Discovery.Buildings
{
    //类职责：作为沙漠巨坑中的未调查墓葬线索，并在调查完成后原地揭示真正入口。
    public sealed class Building_GiantTombBrokenCoffin : Building
    {
        //函数职责：为玩家控制的殖民者提供带路径、预约和操作能力检查的右键调查选项。
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.GetFloatMenuOptions(selPawn))
            {
                yield return option;
            }

            if (selPawn == null || !selPawn.IsColonistPlayerControlled)
            {
                yield break;
            }

            const string label = "调查破损砂岩石棺";
            if (!selPawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                yield return new FloatMenuOption(label + "：无法操作", null);
                yield break;
            }

            if (!selPawn.CanReach(this, PathEndMode.Touch, Danger.Deadly))
            {
                yield return new FloatMenuOption(label + "：没有路径", null);
                yield break;
            }

            if (!selPawn.CanReserve(this))
            {
                yield return new FloatMenuOption(label + "：目标已被占用", null);
                yield break;
            }

            yield return new FloatMenuOption(label, delegate
            {
                Job job = JobMaker.MakeJob(DefOfRefs.NingshaRace_Job_InvestigateGiantTombCoffin, this);
                if (selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc))
                {
                    GetComp<Comp_GiantTombCoffinProximity>()?.MarkNotified();
                }
            });
        }

        //函数职责：把不可摧毁的未调查石棺安全移出地图，在同一位置生成开启入口并提示玩家。
        public void RevealEntrance(Pawn investigator)
        {
            if (!Spawned || Destroyed)
            {
                return;
            }

            Map map = Map;
            IntVec3 position = Position;
            Rot4 rotation = Rotation;
            DeSpawn(DestroyMode.WillReplace);
            Thing entrance = GenSpawn.Spawn(ThingMaker.MakeThing(DefOfRefs.NingshaRace_GiantTombCoffinEntrance), position, map, rotation);
            FleckMaker.ThrowDustPuff(entrance.DrawPos, map, 2f);
            Messages.Message(investigator.LabelShortCap + "发现石棺下方连接着一片庞大的古代墓葬群。", entrance, MessageTypeDefOf.PositiveEvent);
        }
    }
}
