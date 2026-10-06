using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //动作状态属于当前 Pawn，接触点触发原版结算，不接管装备生成和 AI。
    public sealed class CompMeleeAnimation : ThingComp
    {
        internal readonly MeleeAnimationPlayback Playback = new MeleeAnimationPlayback();
        internal readonly MeleeAnimationPlayback PreviewPlayback = new MeleeAnimationPlayback();
        internal readonly MeleeAnimationTrail Trail = new MeleeAnimationTrail();
        internal MeleeAnimationDef Draft;
        internal bool CapturePreview, Paused;
        internal float Clock;
        internal Verb ResolvingVerb;
        internal Pawn Pawn => (Pawn)parent;
        internal MeleeAnimationDef Definition => ((CompProperties_MeleeAnimation)props).animation;
        internal MeleeAnimationDef RenderDefinition => CapturePreview && Draft != null ? Draft : Definition;
        internal MeleeAnimationPlayback Current => CapturePreview ? PreviewPlayback : Playback;

        //非近战装备和明确未被配置选中的武器不进入动画管线。
        internal bool Accepts(Thing equipment) => equipment != null && equipment == Pawn.equipment?.Primary
            && equipment.def.IsMeleeWeapon && (((CompProperties_MeleeAnimation)props).weapon == null
                || ((CompProperties_MeleeAnimation)props).weapon == equipment.def);

        //出手期间由忙碌姿态管理整条时间轴，完成后继续推进残余刀光。
        public override void CompTick()
        {
            if (Pawn.stances.curStance is Stance_MeleeAnimation) return;
            if (Pawn.Dead || Pawn.Downed || Pawn.Rotation != Playback.Facing || !Accepts(Pawn.equipment?.Primary)) Playback.Clear();
            else Playback.Advance(Find.TickManager.TicksGame / 60f);
        }

        //所有已接入单位都有相同编辑入口。
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Prefs.DevMode) yield return new Command_Action
            {
                defaultLabel = "近战动画编辑器", defaultDesc = "编辑当前单位的四向握点、动作曲线和火焰刀光，导入或导出 XML。",
                icon = TexCommand.Attack, action = () => Find.WindowStack.Add(new Window_MeleeAnimationEditor(this))
            };
        }

        //离图后不保留瞬态动作，避免旧网格成为新地图的绘制来源。
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        { Playback.Clear(); PreviewPlayback.Clear(); Trail.Dispose(); base.PostDeSpawn(map, mode); }
        //销毁也释放当前组件拥有的资源。
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        { Trail.Dispose(); base.PostDestroy(mode, previousMap); }
    }
}
