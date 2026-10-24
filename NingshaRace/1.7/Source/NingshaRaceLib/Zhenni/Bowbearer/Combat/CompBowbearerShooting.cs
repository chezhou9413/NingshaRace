using System.Collections.Generic;
using NingshaRaceLib.Zhenni.Bladebearer;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//记录真实放箭事件，蓄力直接读取原版姿态，预览不推进战斗。
public sealed class CompBowbearerShooting : ThingComp
{
    private int releasedTick = -10000;
    private int normalShots;
    internal BowbearerSettings Draft;
    internal bool PreviewActive;
    internal bool PreviewEmpowered;
    internal BowbearerSettings PreviewVisuals;
    internal float PreviewProgress = -1, PreviewReleased = -1;
    internal int ForcedFrame = -1;
    internal System.Action<Vector3> PreviewDraw;
    internal readonly MaterialPropertyBlock NockedArrowProperties = new();
    internal readonly MaterialPropertyBlock FlashProperties = new();
    internal Pawn Pawn => (Pawn)parent;
    internal BowbearerShootingDef Definition => ((CompProperties_BowbearerShooting)props).shooting;
    internal BowbearerSettings Settings => Draft ?? Definition.settings;
    internal bool NextEmpowered => normalShots >= 3;
    internal bool ChargingEmpowered => PreviewActive ? PreviewEmpowered : NextEmpowered;
    internal BowbearerSettings VisualSettings => PreviewActive ? PreviewVisuals : NextEmpowered ? Definition.EmpoweredVisuals : Settings;
    internal float WarmupRemaining => PreviewActive ? PreviewWarmupRemaining : Progress < 0 ? -1 : ((Stance_Warmup)Pawn.stances.curStance).ticksLeft / 60f;
    internal float PreviewWarmupRemaining;
    internal bool Ready => Pawn.Spawned && !Pawn.Dead && !Pawn.Downed && !Pawn.IsOnHoldingPlatform
        && Pawn.equipment.Primary?.def == Pawn.GetComp<CompBladebearerAppearance>().WeaponDef;

    //只有本单位的射击预热才驱动拉弓。
    internal float Progress
    {
        get
        {
            if (PreviewActive) return PreviewProgress;
            if (!Ready || Pawn.stances.curStance is not Stance_Warmup warm || warm.verb is not Verb_BowbearerShoot) return -1;
            int elapsed = Find.TickManager.TicksGame - warm.startedTick;
            return Mathf.Clamp01(elapsed / (float)Mathf.Max(1, elapsed + warm.ticksLeft));
        }
    }

    //回位按游戏时间计算，暂停不前进。
    internal float SinceRelease => PreviewActive ? PreviewReleased : !Ready ? -1 : (Find.TickManager.TicksGame - releasedTick) / 60f;
    internal int Frame => ForcedFrame >= 0 ? ForcedFrame : Settings.Frame(Progress, SinceRelease);

    //只由成功发射的 Verb 通知，不再次创建弹丸。
    internal void Released(bool empowered)
    {
        releasedTick = Find.TickManager.TicksGame;
        normalShots = empowered ? 0 : normalShots + 1;
    }

    //短暂释放姿态平滑回到瞄准基准。
    internal float Recoil()
    {
        float age = SinceRelease;
        if (age < 0 || Progress >= 0) return 0;
        if (age < Settings.releaseSeconds) return Settings.releaseAngle * Mathf.Sin(age / Settings.releaseSeconds * Mathf.PI * .5f);
        return Settings.releaseAngle * (1 - Mathf.SmoothStep(0, 1, (age - Settings.releaseSeconds) / Settings.recoverySeconds));
    }

    //实际攻击时间需要随存档保存，尾迹和编辑草稿不写入。
    public override void PostExposeData()
    {
        Scribe_Values.Look(ref releasedTick, "bowbearerReleasedTick", -10000);
        Scribe_Values.Look(ref normalShots, "bowbearerNormalShots");
    }

    //开发者入口与已有外观编辑器并列。
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (Prefs.DevMode) yield return new Command_Action
        {
            defaultLabel = "挽弓者射击特效编辑器", defaultDesc = "预览普通箭、强化预警与爆焰，编辑并导出参数。",
            icon = TexCommand.DesirePower, action = () => Find.WindowStack.Add(new Window_BowbearerShooting(this))
        };
    }
}

