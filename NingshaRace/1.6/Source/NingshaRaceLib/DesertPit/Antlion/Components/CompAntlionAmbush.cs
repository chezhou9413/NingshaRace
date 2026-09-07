using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Antlion.Effects;
using NingshaRaceLib.DesertPit.Antlion.Generation;
using NingshaRaceLib.DesertPit.Antlion.Rendering;
using NingshaRaceLib.DesertPit.Antlion.State;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.Antlion.Components
{
    //类职责：保存蚁狮阶段与绝对时间，在游戏更新线程协调伏击、追猎和下潜。
    public sealed partial class CompAntlionAmbush : ThingComp
    {
        private AntlionPhase phase = AntlionPhase.SeekingSand;
        private Pawn target;
        private Pawn abandonedTarget;
        private IntVec3 anchor = IntVec3.Invalid;
        private IntVec3 sandDestination = IntVec3.Invalid;
        private int phaseStartTick;
        private int burstUntilTick;
        private int nextScanTick;
        private int nextSandSearchTick;
        private int unreachableSince = -1;
        private IntVec3 pathFailurePosition = IntVec3.Invalid;
        private bool targetReachable;

        //属性职责：读取所属 Pawn、配置和阶段，供任务及渲染使用。
        public Pawn Pawn => (Pawn)parent;
        public CompProperties_AntlionAmbush Props => (CompProperties_AntlionAmbush)props;
        public AntlionPhase Phase => phase;
        public int PhaseStartTick => phaseStartTick;
        public bool Transitioning => phase == AntlionPhase.Emerging || phase == AntlionPhase.Submerging;
        public bool AbleToAct => Pawn.Spawned && !Pawn.Dead && !Pawn.Downed && !Pawn.IsBurning();

        //属性职责：只在有行动能力的出土追击阶段提供暂时移速倍率。
        public float SpeedFactor => AbleToAct && phase == AntlionPhase.Hunting
            && Find.TickManager.TicksGame < burstUntilTick ? Props.burstMultiplier : 1f;

        //属性职责：把绝对阶段时间映射成身体露出比例，读档时不重播完整动画。
        public float SurfaceFraction
        {
            get
            {
                float elapsed = Find.TickManager.TicksGame - phaseStartTick;
                if (phase == AntlionPhase.Emerging) return Mathf.Clamp01(elapsed / Props.emergeTicks);
                if (phase == AntlionPhase.Submerging) return 1f - Mathf.Clamp01(elapsed / Props.submergeTicks);
                return 1f;
            }
        }

        //函数职责：为直接生成且尚无派系的蚁狮登记隐藏敌对派系，不在生成线程加载视觉资源。
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (Pawn.Faction == null) Pawn.SetFaction(AntlionFactionUtility.GetOrCreate());
        }

        //函数职责：序列化状态、猎物和期限，同一 Pawn 跨地下容器与地图保留全部伤势。
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref phase, "antlionPhase", AntlionPhase.SeekingSand);
            Scribe_References.Look(ref target, "antlionTarget");
            Scribe_References.Look(ref abandonedTarget, "antlionAbandonedTarget");
            Scribe_Values.Look(ref anchor, "antlionAnchor", IntVec3.Invalid);
            Scribe_Values.Look(ref sandDestination, "antlionSandDestination", IntVec3.Invalid);
            Scribe_Values.Look(ref phaseStartTick, "antlionPhaseStartTick");
            Scribe_Values.Look(ref burstUntilTick, "antlionBurstUntilTick");
            Scribe_Values.Look(ref nextScanTick, "antlionNextScanTick");
            Scribe_Values.Look(ref nextSandSearchTick, "antlionNextSandSearchTick");
            Scribe_Values.Look(ref unreachableSince, "antlionUnreachableSince", -1);
            Scribe_Values.Look(ref pathFailurePosition, "antlionPathFailurePosition", IntVec3.Invalid);
            Scribe_Values.Look(ref targetReachable, "antlionTargetReachable");
        }

        //函数职责：逐帧处理短动画，仅在错峰检查时查询猎物与沙地，不启动后台工作线程。
        public override void CompTick()
        {
            base.CompTick();
            if (!Pawn.Spawned || Pawn.Dead) return;
            int now = Find.TickManager.TicksGame;
            if (!AbleToAct)
            {
                if (phase != AntlionPhase.SeekingSand) BeginSeekingSand(now);
                AntlionAnimationUtility.Synchronize(this);
                return;
            }

            if (phase == AntlionPhase.Buried) BeginSeekingSand(now);
            if (!anchor.IsValid) anchor = Pawn.Position;
            if (burstUntilTick != 0 && now >= burstUntilTick)
            {
                burstUntilTick = 0;
                StatDefOf.MoveSpeed.Worker.ClearCacheForThing(Pawn);
            }

            if (now >= nextScanTick)
            {
                nextScanTick = now + Props.scanIntervalTicks
                    - (now + parent.thingIDNumber) % Props.scanIntervalTicks;
                if (phase == AntlionPhase.Hunting) UpdateHunt(now);
                else if (phase == AntlionPhase.SeekingSand || phase == AntlionPhase.Submerging) UpdateReturn(now);
            }
            if (!Pawn.Spawned) return;
            if (phase == AntlionPhase.Emerging && now - phaseStartTick >= Props.emergeTicks)
            {
                phase = AntlionPhase.Hunting;
                burstUntilTick = now + Props.burstTicks;
                StatDefOf.MoveSpeed.Worker.ClearCacheForThing(Pawn);
                UpdateHunt(now);
                StopOwnJob();
            }
            if (phase == AntlionPhase.Submerging && now - phaseStartTick >= Props.submergeTicks)
            {
                FinishSubmerging(now);
                if (!Pawn.Spawned) return;
            }
            AntlionAnimationUtility.Synchronize(this);
            if (Transitioning) AntlionSandEffects.TickTransition(this);
        }

        //函数职责：在地下容器持有前清除地表目标与加速，不重建或治疗 Pawn。
        public void PrepareBuried()
        {
            phase = AntlionPhase.Buried;
            target = null;
            abandonedTarget = null;
            anchor = IntVec3.Invalid;
            sandDestination = IntVec3.Invalid;
            burstUntilTick = 0;
            unreachableSince = -1;
            targetReachable = false;
            pathFailurePosition = IntVec3.Invalid;
            StatDefOf.MoveSpeed.Worker.ClearCacheForThing(Pawn);
        }

        //函数职责：从容器出土后记录唯一追击锚点并开始可受伤的出土阶段。
        public void BeginEmerging(Pawn prey)
        {
            target = prey;
            anchor = Pawn.Position;
            phase = AntlionPhase.Emerging;
            phaseStartTick = Find.TickManager.TicksGame;
            nextScanTick = phaseStartTick;
            unreachableSince = -1;
            pathFailurePosition = IntVec3.Invalid;
            Pawn.Rotation = Rot4.FromAngleFlat((prey.Position - Pawn.Position).ToVector3().AngleFlat());
            AntlionSandEffects.Erupt(Pawn.Map, Pawn.Position, parent.thingIDNumber);
            AntlionAnimationUtility.Synchronize(this);
        }

        //函数职责：结束本模块派发的任务，保留原版倒地和着火应急任务。
        private void StopOwnJob()
        {
            JobDef def = Pawn.CurJobDef;
            if (def != DefOfRefs.NingshaRace_AntlionAttack && def != DefOfRefs.NingshaRace_AntlionSeekSand
                && def != DefOfRefs.NingshaRace_AntlionWait) return;
            Pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false);
            Pawn.pather.StopDead();
        }
    }
}
