using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Core.Effects;
using NingshaRaceLib.Petrification.Defs;
using NingshaRaceLib.Petrification.Patches;
using NingshaRaceLib.Petrification.Rendering;
using NingshaRaceLib.Petrification.Utility;
using NingshaRaceLib.SandGolem.Utility;

namespace NingshaRaceLib.Petrification.Health
{
    //类职责：保存石化严重度与持续时间，在满层期间登记材质替换状态。
    public class Hediff_Petrification : HediffWithComps
    {
        //字段职责：记录最近一次石化严重度实际增加的游戏 Tick。
        private int lastSeverityIncreaseTick = -1;

        //字段职责：记录完全石化持续阶段结束的游戏 Tick。
        private int fullPetrificationEndTick = -1;

        //字段职责：记录当前运行期是否已登记石化材质，避免每 Tick 重复安装补丁和刷新图形。
        private bool runtimePresentationInitialized;

        //属性职责：返回最近一次实际增加严重度的游戏 Tick。
        public int LastSeverityIncreaseTick => lastSeverityIncreaseTick;

        //属性职责：返回完全石化结束的游戏 Tick，未触发时为负数。
        public int FullPetrificationEndTick => fullPetrificationEndTick;

        //属性职责：判断完全石化是否仍在生效且 Pawn 仍然存活。
        public bool IsFullyPetrified => fullPetrificationEndTick > GenTicks.TicksGame && pawn != null && !pawn.Dead;

        //属性职责：判断一次完全石化是否已经触发但尚未完成清理。
        public bool HasTriggeredFullPetrification => fullPetrificationEndTick >= 0;

        //属性职责：返回 HediffDef 上配置的石化持续与消退参数。
        private PetrificationDefExtension Settings => def.GetModExtension<PetrificationDefExtension>();

        //属性职责：返回 HediffDef 声明的完全石化严重度阈值。
        private float FullSeverity => def.maxSeverity;

        //属性职责：以真实百分比显示石化严重度。
        public override string SeverityLabel => (Severity / FullSeverity).ToStringPercent();

        //属性职责：在满层持续时间结束或十二小时未累计时请求健康系统移除本状态。
        public override bool ShouldRemove
        {
            get
            {
                if (HasTriggeredFullPetrification)
                {
                    return GenTicks.TicksGame >= fullPetrificationEndTick;
                }
                if (base.ShouldRemove)
                {
                    return true;
                }
                return lastSeverityIncreaseTick >= 0
                    && GenTicks.TicksGame - lastSeverityIncreaseTick >= Settings.inactivityDurationTicks;
            }
        }

        //属性职责：拦截外部严重度写入，记录实际增量并在首次满层时进入完全石化。
        public override float Severity
        {
            get => base.Severity;
            set
            {
                if (HasTriggeredFullPetrification && value < FullSeverity)
                {
                    value = FullSeverity;
                }

                float previousSeverity = base.Severity;
                int previousStageIndex = CurStageIndex;
                base.Severity = value;
                float currentSeverity = base.Severity;
                bool severityChanged = !Mathf.Approximately(previousSeverity, currentSeverity);

                if (currentSeverity > previousSeverity)
                {
                    lastSeverityIncreaseTick = GenTicks.TicksGame;
                }

                bool attached = pawn != null
                    && pawn.health != null
                    && pawn.health.hediffSet.hediffs.Contains(this);
                if (attached && currentSeverity >= FullSeverity && !HasTriggeredFullPetrification)
                {
                    EnterFullPetrification();
                }
                if (attached && severityChanged && CurStageIndex == previousStageIndex)
                {
                    pawn.health.Notify_HediffChanged(this);
                }
            }
        }

        //函数职责：加入健康系统后初始化累计计时，并处理以满严重度直接创建的情况。
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (lastSeverityIncreaseTick < 0)
            {
                lastSeverityIncreaseTick = GenTicks.TicksGame;
            }
            if (base.Severity >= FullSeverity && !HasTriggeredFullPetrification)
            {
                EnterFullPetrification();
            }
        }

        //函数职责：移除状态时解除完全石化缓存并重置 Pawn 的固定绘制状态。
        public override void PostRemoved()
        {
            base.PostRemoved();
            EndRuntimePresentation();
            fullPetrificationEndTick = -1;
            runtimePresentationInitialized = false;
        }

        //函数职责：Pawn 死亡时立即停止完全石化锁定和石化材质覆盖。
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            EndRuntimePresentation();
            fullPetrificationEndTick = -1;
            runtimePresentationInitialized = false;
        }

        //函数职责：在地图开始正常推进后恢复读档 Pawn 的石化材质，避免存档载入阶段安装 Harmony 补丁。
        public override void Tick()
        {
            base.Tick();
            if (IsFullyPetrified && !runtimePresentationInitialized)
            {
                InitializeRuntimePresentation();
            }
        }

        //函数职责：保存累计时间和完全石化结束时间，并让读档阶段只恢复纯数据状态。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastSeverityIncreaseTick, "lastSeverityIncreaseTick", -1);
            Scribe_Values.Look(ref fullPetrificationEndTick, "fullPetrificationEndTick", -1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                runtimePresentationInitialized = false;
                PetrificationUtility.UnregisterFullyPetrified(pawn);
            }
        }

        //函数职责：记录满层结束时间、登记石化材质状态并刷新渲染缓存。
        private void EnterFullPetrification()
        {
            fullPetrificationEndTick = GenTicks.TicksGame + Settings.fullPetrificationDurationTicks;
            if (Scribe.mode != LoadSaveMode.Inactive)
            {
                return;
            }

            InitializeRuntimePresentation();
        }

        //函数职责：在正常游戏阶段安装渲染补丁、登记 Pawn 并冻结全部行动与视觉动画。
        private void InitializeRuntimePresentation()
        {
            if (!UnityData.IsInMainThread)
            {
                throw new System.InvalidOperationException("完全石化状态只能在游戏主线程初始化。");
            }

            PetrificationRenderingPatchInstaller.EnsureInstalled();
            PetrificationUtility.RegisterFullyPetrified(pawn, this);
            StopCurrentActions();
            runtimePresentationInitialized = true;
            MarkGraphicsDirty();
        }

        //函数职责：在完全石化开始时终止 Job、路径、姿态、飞行和渲染动画。
        private void StopCurrentActions()
        {
            pawn?.jobs?.StopAll();
            pawn?.pather?.StopDead();
            pawn?.stances?.CancelBusyStanceHard();
            pawn?.flight?.ForceLand();
            pawn?.Drawer?.renderer?.SetAnimation(null);
            pawn?.Drawer?.tweener?.ResetTweenedPosToRoot();
        }

        //函数职责：解除石化登记、清理移动残留并立即请求 AI 选择新的工作。
        private void EndRuntimePresentation()
        {
            if (!UnityData.IsInMainThread)
            {
                throw new System.InvalidOperationException("完全石化状态只能在游戏主线程解除。");
            }
            Pawn affectedPawn = pawn;
            affectedPawn?.pather?.StopDead();
            if (affectedPawn?.Spawned == true)
            {
                affectedPawn.Map.pawnDestinationReservationManager.ReleaseAllClaimedBy(affectedPawn);
            }
            PetrificationUtility.UnregisterFullyPetrified(pawn);
            affectedPawn?.Drawer?.tweener?.ResetTweenedPosToRoot();
            MarkGraphicsDirty();
            if (affectedPawn != null
                && !affectedPawn.Destroyed
                && !affectedPawn.Dead
                && affectedPawn.Spawned
                && affectedPawn.mindState?.Active == true)
            {
                affectedPawn.jobs?.CheckForJobOverride();
            }
        }

        //函数职责：刷新地图绘制、肖像和渲染树使用的材质缓存。
        private void MarkGraphicsDirty()
        {
            pawn?.Drawer?.renderer?.SetAllGraphicsDirty();
        }
    }
}
