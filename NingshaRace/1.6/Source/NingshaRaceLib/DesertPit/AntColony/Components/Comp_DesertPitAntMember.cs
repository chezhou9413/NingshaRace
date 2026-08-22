using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

using NingshaRaceLib.DesertPit.AntColony.Core;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //类职责：保存单只蚂蚁的巢群编号，并把生成和销毁事件转交地图组件。
    public class Comp_DesertPitAntMember : ThingComp
    {
        //字段职责：记录成员所属的地图内唯一蚁巢编号。
        private int colonyId;

        //字段职责：记录当前采集批次已经完成的搬运次数。
        private int completedHaulsThisCycle;

        //字段职责：记录工蚁下一次允许外出采集的游戏 Tick。
        private int nextForageTick;

        //属性职责：向蚁群管理器和调试界面提供成员所属蚁巢编号。
        public int ColonyId => colonyId;

        //属性职责：从组件配置读取成员固定阶级。
        public AntCaste Caste => ((CompProperties_DesertPitAntMember)props).caste;

        //函数职责：设置成员所属巢群编号，供生成和补员流程登记成员。
        public void AssignColony(int id)
        {
            colonyId = id;
        }

        //函数职责：判断工蚁是否已经结束搬运冷却并允许申请下一趟采集工作。
        public bool CanStartForage(int currentTick, int haulLimit)
        {
            if (Caste != AntCaste.Worker)
            {
                return false;
            }

            if (nextForageTick > currentTick)
            {
                return false;
            }

            if (nextForageTick > 0)
            {
                nextForageTick = 0;
                completedHaulsThisCycle = 0;
            }

            return completedHaulsThisCycle < System.Math.Max(1, haulLimit);
        }

        //函数职责：记录一趟已经成功落货的搬运，并在达到批次上限时开始冷却。
        public void NotifySuccessfulHaul(int currentTick, int haulLimit, int cooldownTicks)
        {
            if (Caste != AntCaste.Worker)
            {
                return;
            }

            completedHaulsThisCycle++;
            if (completedHaulsThisCycle >= System.Math.Max(1, haulLimit))
            {
                nextForageTick = currentTick + System.Math.Max(0, cooldownTicks);
            }
        }

        //函数职责：保存成员所属巢群编号。
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref colonyId, "colonyId");
            Scribe_Values.Look(ref completedHaulsThisCycle, "completedHaulsThisCycle");
            Scribe_Values.Look(ref nextForageTick, "nextForageTick");
        }

        //函数职责：成员进入地图时向对应地图组件恢复运行时索引。
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (colonyId > 0 && parent.Map != null)
            {
                parent.Map.GetComponent<MapComponent_DesertPitAntColonies>().NotifyMemberSpawned(parent as Pawn, colonyId);
            }
        }

        //函数职责：在工蚁持有旧逻辑误选的活植物时立即中止搬运并将植物安全放回地图。
        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = parent as Pawn;
            if (Caste != AntCaste.Worker || pawn == null || !pawn.Spawned || !(pawn.carryTracker?.CarriedThing is Plant))
            {
                return;
            }

            Map map = pawn.Map;
            map.GetComponent<MapComponent_DesertPitAntColonies>().ReleaseForageAssignments(pawn);
            pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out Thing _);
            if (pawn.CurJobDef != null)
            {
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
            }
        }

        //函数职责：选中蚂蚁时用白线连接其所属且仍存活的蚁穴。
        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            Pawn pawn = parent as Pawn;
            if (pawn == null || !pawn.Spawned || colonyId <= 0)
            {
                return;
            }

            State.AntColonyState state;
            if (pawn.Map.GetComponent<MapComponent_DesertPitAntColonies>().TryGetColony(pawn, out state) && state.Nest != null && state.Nest.Spawned && !state.Nest.Destroyed)
            {
                GenDraw.DrawLineBetween(pawn.DrawPos, state.Nest.DrawPos, SimpleColor.White, 0.2f);
            }
        }

        //函数职责：成员被销毁或死亡时通知地图组件释放成员和搬运分配。
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (previousMap != null && colonyId > 0)
            {
                previousMap.GetComponent<MapComponent_DesertPitAntColonies>().NotifyMemberDestroyed(parent as Pawn, colonyId);
            }
        }
    }
}
