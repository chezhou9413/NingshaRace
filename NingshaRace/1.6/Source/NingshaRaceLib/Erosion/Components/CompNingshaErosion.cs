using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Erosion.UI;
using NingshaRaceLib.Erosion.Utility;
using NingshaRaceLib.SandGolem.Tracking;

namespace NingshaRaceLib.Erosion.Components
{
    //类职责：持久化凝砂族侵蚀值，处理自然衰减并驱动满值后的实体化过程。
    public sealed class CompNingshaErosion : ThingComp
    {
        //字段职责：定义 RimWorld 一个游戏日包含的 Tick 数。
        private const float TicksPerDay = 60000f;

        //字段职责：保存当前侵蚀点数。
        private float currentErosion;

        //字段职责：记录 Pawn 是否正在执行侵蚀体转化。
        private bool transforming;

        //字段职责：保存侵蚀体转化完成的绝对 Tick。
        private int transformationEndTick = -1;

        //字段职责：保存不进入存档的原版起身粒子实例。
        private Effecter riseEffecter;

        //字段职责：保存不进入存档的原版起身持续音效。
        private Sustainer riseSustainer;

        //属性职责：返回侵蚀组件的配置参数。
        public CompProperties_NingshaErosion Props => (CompProperties_NingshaErosion)props;

        //属性职责：返回组件所属的凝砂族 Pawn。
        public Pawn Pawn => parent as Pawn;

        //属性职责：返回当前侵蚀点数。
        public float CurrentErosion => currentErosion;

        //属性职责：实时读取 Pawn 经基因、Hediff 和装备结算后的侵蚀上限。
        public float MaxErosion => Mathf.Max(1f, Pawn.GetStatValue(DefOfRefs.NingshaRace_ErosionLimit));

        //属性职责：返回钳制在零到一之间的当前侵蚀比例。
        public float ErosionRatio => Mathf.Clamp01(currentErosion / MaxErosion);

        //属性职责：返回是否已经进入不可移动的实体化阶段。
        public bool IsTransforming => transforming;

        //属性职责：返回距离实体化完成仍需等待的 Tick 数。
        public int TransformationTicksRemaining =>
            transforming ? Mathf.Max(0, transformationEndTick - Find.TickManager.TicksGame) : 0;

        //函数职责：保存当前侵蚀值、转化状态和完成 Tick。
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref currentErosion, "currentErosion", 0f);
            Scribe_Values.Look(ref transforming, "transforming", false);
            Scribe_Values.Look(ref transformationEndTick, "transformationEndTick", -1);
        }

        //函数职责：Pawn 进入地图时恢复尚未完成的移动锁定和起身动画。
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (transforming && Pawn != null && !Pawn.Dead && !Pawn.IsMutant)
            {
                ErosionTransformationUtility.LockPawn(Pawn, stopJobs: true);
                ErosionTransformationUtility.EnsureTransformationHediff(Pawn);
                MaintainTransformationEffects();
            }
        }

        //函数职责：按间隔推进侵蚀衰减或满值实体化流程。
        public override void CompTickInterval(int delta)
        {
            Pawn pawn = Pawn;
            if (pawn == null)
            {
                return;
            }
            if (transforming)
            {
                TickTransformation();
                return;
            }
            if (pawn.Dead || pawn.IsMutant)
            {
                return;
            }

            float maximum = MaxErosion;
            currentErosion = Mathf.Clamp(currentErosion, 0f, maximum);
            if (currentErosion >= maximum)
            {
                StartTransformation();
                return;
            }
            if (currentErosion > 0f)
            {
                currentErosion = Mathf.Max(0f, currentErosion - Props.dailyDecay / TicksPerDay * delta);
            }
        }

        //函数职责：为普通玩家凝砂族提供只读状态条，并在上帝模式下提供满侵蚀测试按钮。
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (ErosionPawnUtility.IsNormalPlayerNingsha(Pawn))
            {
                yield return new Gizmo_NingshaErosion
                {
                    erosion = this
                };

                if (DebugSettings.godMode && !Pawn.Dead && !transforming)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "DEV: 拉满侵蚀",
                        defaultDesc = "立即把当前侵蚀值提升到最终上限，并启动满值转化流程。",
                        action = () => AddErosion(MaxErosion)
                    };
                }
            }
        }

        //函数职责：增加指定侵蚀点数，并在达到最终上限时立即启动实体化动画。
        public void AddErosion(float amount)
        {
            Pawn pawn = Pawn;
            if (amount <= 0f || pawn == null || pawn.Dead || pawn.IsMutant || transforming)
            {
                return;
            }

            currentErosion = Mathf.Clamp(currentErosion + amount, 0f, MaxErosion);
            if (currentErosion >= MaxErosion)
            {
                StartTransformation();
            }
        }

        //函数职责：降低普通阶段侵蚀值并钳制到零，但绝不逆转已经开始的实体化流程。
        public void ReduceErosion(float amount)
        {
            if (amount <= 0f || transforming)
            {
                return;
            }
            currentErosion = Mathf.Max(0f, currentErosion - amount);
        }

        //函数职责：判断增加指定点数后是否会达到当前最终侵蚀上限。
        public bool WouldReachLimit(float amount)
        {
            return currentErosion + Mathf.Max(0f, amount) >= MaxErosion;
        }

        //函数职责：达到满值时停止工作、锁定移动并启动五至十秒的原版蹒跚怪起身表现。
        public void StartTransformation()
        {
            Pawn pawn = Pawn;
            if (transforming || pawn == null || pawn.Dead || pawn.IsMutant)
            {
                return;
            }

            currentErosion = MaxErosion;
            transforming = true;
            transformationEndTick = Find.TickManager.TicksGame + Props.transformationTicks.RandomInRange;
            ErosionTransformationUtility.LockPawn(pawn, stopJobs: true);
            ErosionTransformationUtility.EnsureTransformationHediff(pawn);
            MaintainTransformationEffects();
            Messages.Message(
                pawn.LabelShortCap + "已被侵蚀彻底吞没，正在转化为侵蚀体。",
                pawn,
                MessageTypeDefOf.NegativeEvent,
                historical: false);
        }

        //函数职责：因死亡取消未完成的转化，并清理移动锁定、Hediff、动画和运行时特效。
        public void CancelTransformation()
        {
            if (!transforming)
            {
                CleanupRuntimeEffects(clearAnimation: true);
                return;
            }

            transforming = false;
            transformationEndTick = -1;
            ErosionTransformationUtility.UnlockPawn(Pawn);
            ErosionTransformationUtility.RemoveTransformationHediff(Pawn);
            CleanupRuntimeEffects(clearAnimation: true);
        }

        //函数职责：Pawn 死亡时取消尚未完成的实体化过程。
        public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
        {
            CancelTransformation();
        }

        //函数职责：Pawn 离开地图时释放只对当前地图有效的特效与音效资源。
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            CleanupRuntimeEffects(clearAnimation: false);
            base.PostDeSpawn(map, mode);
        }

        //函数职责：Pawn 被销毁时释放全部实体化运行时资源。
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            CleanupRuntimeEffects(clearAnimation: true);
            base.PostDestroy(mode, previousMap);
        }

        //函数职责：维持转化锁定与起身表现，并在结束 Tick 到达后执行永久突变。
        private void TickTransformation()
        {
            Pawn pawn = Pawn;
            if (pawn == null || pawn.Dead || pawn.Destroyed)
            {
                CancelTransformation();
                return;
            }
            if (pawn.IsMutant)
            {
                transforming = false;
                transformationEndTick = -1;
                CleanupRuntimeEffects(clearAnimation: false);
                return;
            }
            if (Find.TickManager.TicksGame >= transformationEndTick)
            {
                FinishTransformation();
                return;
            }

            ErosionTransformationUtility.LockPawn(pawn, stopJobs: false);
            ErosionTransformationUtility.EnsureTransformationHediff(pawn);
            MaintainTransformationEffects();
        }

        //函数职责：收回召唤者的沙傀并通过原版 Mutant 流程永久安装侵蚀体状态。
        private void FinishTransformation()
        {
            Pawn pawn = Pawn;
            transforming = false;
            transformationEndTick = -1;
            CleanupRuntimeEffects(clearAnimation: true);
            ErosionTransformationUtility.UnlockPawn(pawn);
            ErosionTransformationUtility.RemoveTransformationHediff(pawn);
            ErosionTransformationUtility.DropFromCarrier(pawn);
            GameComponent_SandGolemTracker.Current?.RecallGolemForCaster(pawn);
            ErosionBodySpawnUtility.TurnIntoErosionBody(pawn);
        }

        //函数职责：维持原版蹒跚怪起身动画、起身音效和扬尘粒子。
        private void MaintainTransformationEffects()
        {
            Pawn pawn = Pawn;
            if (pawn == null || !pawn.Spawned)
            {
                return;
            }

            if (pawn.Drawer.renderer.CurAnimation != AnimationDefOf.ShamblerRise)
            {
                pawn.Drawer.renderer.SetAnimation(AnimationDefOf.ShamblerRise);
            }

            int remaining = transformationEndTick - Find.TickManager.TicksGame;
            if (remaining <= 15)
            {
                riseSustainer?.End();
                return;
            }
            if (riseSustainer == null || riseSustainer.Ended)
            {
                SoundInfo info = SoundInfo.InMap(pawn, MaintenanceType.PerTick);
                riseSustainer = SoundDefOf.Pawn_Shambler_Rise.TrySpawnSustainer(info);
            }
            if (riseEffecter == null)
            {
                riseEffecter = EffecterDefOf.ShamblerRaise.Spawn(pawn, pawn.Map);
            }

            riseSustainer.Maintain();
            riseEffecter.EffectTick(pawn, TargetInfo.Invalid);
        }

        //函数职责：结束持续音效与粒子，并按需要清除 Pawn 当前动画。
        private void CleanupRuntimeEffects(bool clearAnimation)
        {
            riseSustainer?.End();
            riseSustainer = null;
            riseEffecter?.Cleanup();
            riseEffecter = null;
            if (clearAnimation && Pawn?.Drawer?.renderer != null)
            {
                Pawn.Drawer.renderer.SetAnimation(null);
            }
        }

    }
}
