using Verse;
using Verse.AI.Group;

namespace NingshaRaceLib.DesertPit.AntColony.Lords
{
    //类职责：让一组兵蚁依次完成巢穴集结、热点调查、防御观察与共同返巢。
    public sealed class LordJob_DesertPitAntInvestigation : LordJob
    {
        //字段职责：记录调查队所属蚁巢编号，供地图组件查找和强制解散。
        public int ColonyId;

        //字段职责：记录集结与返程使用的蚁穴位置。
        private IntVec3 nestCell;

        //字段职责：记录调查队需要前往的死亡热点。
        private IntVec3 hotspotCell;

        //字段职责：记录集结阶段的最长持续时间。
        private int rallyTimeoutTicks;

        //字段职责：记录单程移动阶段的最长持续时间。
        private int travelTimeoutTicks;

        //字段职责：记录热点防御观察的持续时间。
        private int defendTicks;

        //属性职责：阻止原版为调查队附加逃跑状态，保持状态图完全受本任务控制。
        public override bool AddFleeToil => false;

        //函数职责：供存档系统通过无参构造函数恢复调查任务。
        public LordJob_DesertPitAntInvestigation()
        {
        }

        //函数职责：用蚁巢、热点和各阶段时限建立调查任务。
        public LordJob_DesertPitAntInvestigation(int colonyId, IntVec3 nestCell, IntVec3 hotspotCell, int rallyTimeoutTicks, int travelTimeoutTicks, int defendTicks)
        {
            ColonyId = colonyId;
            this.nestCell = nestCell;
            this.hotspotCell = hotspotCell;
            this.rallyTimeoutTicks = rallyTimeoutTicks;
            this.travelTimeoutTicks = travelTimeoutTicks;
            this.defendTicks = defendTicks;
        }

        //函数职责：构建集结、外出、调查、防御、返程和解散的完整 Lord 状态图。
        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();
            LordToil_Travel rally = new LordToil_Travel(nestCell) { maxDanger = Danger.Deadly };
            LordToil_Travel outbound = new LordToil_Travel(hotspotCell) { maxDanger = Danger.Deadly };
            LordToil_DefendPoint investigate = new LordToil_DefendPoint(hotspotCell, 10f, 8f);
            LordToil_Travel returning = new LordToil_Travel(nestCell) { maxDanger = Danger.Deadly };
            LordToil_End end = new LordToil_End();

            graph.AddToil(rally);
            graph.AddToil(outbound);
            graph.AddToil(investigate);
            graph.AddToil(returning);
            graph.AddToil(end);

            AddArrivalTransition(graph, rally, outbound);
            AddTimeoutTransition(graph, rally, end, rallyTimeoutTicks);
            AddArrivalTransition(graph, outbound, investigate);
            AddTimeoutTransition(graph, outbound, end, travelTimeoutTicks);
            AddTimeoutTransition(graph, investigate, returning, defendTicks);
            AddArrivalTransition(graph, returning, end);
            AddTimeoutTransition(graph, returning, end, travelTimeoutTicks);
            return graph;
        }

        //函数职责：保存调查队所属巢群、目的地和各阶段时限。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ColonyId, "colonyId");
            Scribe_Values.Look(ref nestCell, "nestCell");
            Scribe_Values.Look(ref hotspotCell, "hotspotCell");
            Scribe_Values.Look(ref rallyTimeoutTicks, "rallyTimeoutTicks", 2500);
            Scribe_Values.Look(ref travelTimeoutTicks, "travelTimeoutTicks", 10000);
            Scribe_Values.Look(ref defendTicks, "defendTicks", 2500);
        }

        //函数职责：添加原版 TravelToil 发出抵达备忘录后的阶段转换。
        private static void AddArrivalTransition(StateGraph graph, LordToil source, LordToil target)
        {
            Transition transition = new Transition(source, target);
            transition.AddTrigger(new Trigger_Memo("TravelArrived"));
            graph.AddTransition(transition);
        }

        //函数职责：添加阶段超过指定时间后的转换，用于调查持续时间和安全超时。
        private static void AddTimeoutTransition(StateGraph graph, LordToil source, LordToil target, int ticks)
        {
            Transition transition = new Transition(source, target);
            transition.AddTrigger(new Trigger_TicksPassed(ticks));
            graph.AddTransition(transition);
        }
    }
}
