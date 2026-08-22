using RimWorld;
using Verse;

namespace NingshaRaceLib.GiantTomb.Discovery.Components
{
    //类职责：在玩家殖民者首次看见附近破损石棺时发送一次可持久化的提示信件。
    public sealed class Comp_GiantTombCoffinProximity : ThingComp
    {
        //字段职责：记录接近提示是否已经发送或因玩家主动调查而提前处理。
        private bool notified;

        //属性职责：读取破损石棺 Def 上配置的提示参数。
        private CompProperties_GiantTombCoffinProximity Settings => (CompProperties_GiantTombCoffinProximity)props;

        //函数职责：每60 Tick 检查八格内清醒、可视且与石棺存在视线的玩家殖民者。
        public override void CompTick()
        {
            base.CompTick();
            if (notified || !parent.Spawned || !parent.IsHashIntervalTick(60))
            {
                return;
            }

            Map map = parent.Map;
            int cellCount = GenRadial.NumCellsInRadius(Settings.radius);
            for (int i = 0; i < cellCount; i++)
            {
                IntVec3 cell = parent.Position + GenRadial.RadialPattern[i];
                if (!cell.InBounds(map))
                {
                    continue;
                }

                var things = cell.GetThingList(map);
                for (int j = 0; j < things.Count; j++)
                {
                    Pawn pawn = things[j] as Pawn;
                    if (pawn != null && pawn.IsColonistPlayerControlled && pawn.Awake() && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight) && GenSight.LineOfSightToThing(pawn.Position, parent, map))
                    {
                        SendNotice(pawn);
                        return;
                    }
                }
            }
        }

        //函数职责：保存接近提示状态，保证读档后不会重复发送同一封信件。
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref notified, "ningshaGiantTombCoffinNotified", false);
        }

        //函数职责：在玩家主动下达调查工作时提前消费接近提示。
        public void MarkNotified()
        {
            notified = true;
        }

        //函数职责：向玩家发送一次指向破损石棺的中性信件并记录触发者。
        private void SendNotice(Pawn pawn)
        {
            Find.LetterStack.ReceiveLetter(
                Settings.letterLabel.Formatted(pawn.Named("PAWN")),
                Settings.letterText.Formatted(pawn.Named("PAWN")),
                Settings.letterDef ?? LetterDefOf.NeutralEvent,
                parent);
            notified = true;
        }
    }
}
