using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace NingshaRaceLib.Storyteller.Quests.Scheduling
{
    //类职责：让索提斯在预定时间后持续尝试提供一次侵蚀追杀任务，直到事件成功出现。
    public sealed class StorytellerComp_NingshaErosionPursuit : StorytellerComp
    {
        //属性职责：取得 XML 中配置的固定侵蚀追杀事件。
        private StorytellerCompProperties_NingshaErosionPursuit Props =>
            (StorytellerCompProperties_NingshaErosionPursuit)props;

        //函数职责：在世界目标的叙事者周期中检查保证时间和事件可用条件并生成待执行事件。
        public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
        {
            if (!(target is World) || Props.incident == null)
            {
                yield break;
            }

            WorldComponent_NingshaErosionPursuit state =
                Find.World.GetComponent<WorldComponent_NingshaErosionPursuit>();
            if (state == null || state.OfferConsumed)
            {
                yield break;
            }

            state.EnsureSotisiSchedule();
            if (!state.SotisiGuaranteedOfferDue || !Props.incident.TargetAllowed(target))
            {
                yield break;
            }

            IncidentParms parms = GenerateParms(Props.incident.category, target);
            if (Props.incident.Worker.CanFireNow(parms))
            {
                yield return new FiringIncident(Props.incident, this, parms);
            }
        }
    }
}
