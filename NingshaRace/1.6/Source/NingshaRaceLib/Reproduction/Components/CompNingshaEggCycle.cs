using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Reproduction.Utility;

using NingshaRaceLib.UI.Gizmos;

namespace NingshaRaceLib.Reproduction.Components
{
    //类职责：保存雌性凝砂的排卵进度，并提供正式产卵与开发者控制入口。
    public class CompNingshaEggCycle : ThingComp
    {
        //字段职责：保存当前未受精卵周期的归一化进度。
        private float eggProgress;

        //字段职责：标记新生成 Pawn 的随机初始进度是否已经建立。
        private bool cycleInitialized;

        //属性职责：提供当前组件所属的凝砂 Pawn。
        private Pawn Pawn => (Pawn)parent;

        //属性职责：提供当前组件的排卵参数。
        private CompProperties_NingshaEggCycle Props => (CompProperties_NingshaEggCycle)props;

        //函数职责：保存排卵进度并在读取旧实例时补建周期状态。
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref eggProgress, "ningshaEggProgress", 0f);
            Scribe_Values.Look(ref cycleInitialized, "ningshaEggCycleInitialized", false);
        }

        //函数职责：按固定间隔推进排卵周期，避免每个游戏 Tick 执行完整条件检查。
        public override void CompTick()
        {
            base.CompTick();
            EnsureCycleInitialized();
            if (!parent.IsHashIntervalTick(2500))
            {
                return;
            }

            if (!CanAdvanceCycle(out _))
            {
                return;
            }

            float intervalTicks = Props.eggLayingIntervalDays * 60000f;
            eggProgress = Mathf.Clamp01(eggProgress + 2500f / intervalTicks);
            if (eggProgress >= 1f)
            {
                TryLayUnfertilizedEgg();
            }
        }

        //函数职责：显示排卵百分比与当前暂停原因。
        public override string CompInspectStringExtra()
        {
            if (Pawn.gender != Gender.Female || Pawn.Dead)
            {
                return null;
            }

            EnsureCycleInitialized();
            string text = "NingshaRace_EggCycleProgress".Translate(eggProgress.ToStringPercent());
            if (!CanAdvanceCycle(out string pauseReason))
            {
                text += "\n" + "NingshaRace_EggCyclePaused".Translate(pauseReason);
            }
            return text;
        }

        //函数职责：仅在开发者模式下提供产卵、孕期推进与必定受孕控制。
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            if (!DebugSettings.godMode || Pawn.gender != Gender.Female)
            {
                yield break;
            }

            yield return new Command_NingshaAction
            {
                defaultLabel = "DEV：立刻排出未受精卵",
                defaultDesc = "直接调用正式排卵逻辑，并重新开始七天周期。",
                action = delegate { TryLayUnfertilizedEgg(); }
            };

            Command_Action finishPregnancy = new Command_NingshaAction
            {
                defaultLabel = "DEV：强制完成妊娠",
                defaultDesc = "把当前原版怀孕推进到生产阶段，随后继续走正式产卵流程。",
                action = delegate { NingshaReproductionUtility.ForcePregnancyToLabor(Pawn); }
            };
            if (!NingshaReproductionUtility.HasHumanPregnancy(Pawn))
            {
                finishPregnancy.Disable("当前没有凝砂怀孕。");
            }
            yield return finishPregnancy;

            Command_Action layFertilizedEgg = new Command_NingshaAction
            {
                defaultLabel = "DEV：立刻排出受精卵",
                defaultDesc = "使用当前怀孕记录的父亲立即产下一枚受精凝砂卵。",
                action = delegate { NingshaReproductionUtility.CompleteCurrentPregnancyAsEggImmediately(Pawn, preventLetter: false); }
            };
            if (!NingshaReproductionUtility.HasHumanPregnancy(Pawn))
            {
                layFertilizedEgg.Disable("当前没有凝砂怀孕。");
            }
            yield return layFertilizedEgg;

        }

        //函数职责：直接尝试排出未受精卵，并只在成功放置后重置周期。
        public bool TryLayUnfertilizedEgg()
        {
            EnsureCycleInitialized();
            if (!NingshaReproductionUtility.TryCreateAndPlaceEgg(Pawn, Props.unfertilizedEggDef, out Thing egg))
            {
                return false;
            }

            eggProgress = 0f;
            if (PawnUtility.ShouldSendNotificationAbout(Pawn))
            {
                Messages.Message("NingshaRace_MessageLaidUnfertilizedEgg".Translate(Pawn.Named("PAWN")), egg, MessageTypeDefOf.NeutralEvent);
            }
            return true;
        }

        //函数职责：为新生成的成年凝砂随机分散首轮排卵进度，避免同批 Pawn 同时产卵。
        private void EnsureCycleInitialized()
        {
            if (cycleInitialized)
            {
                return;
            }

            eggProgress = Rand.Value;
            cycleInitialized = true;
        }

        //函数职责：检查性别、年龄、生育能力和孕期状态是否允许推进排卵周期。
        private bool CanAdvanceCycle(out string pauseReason)
        {
            if (Pawn.Dead)
            {
                pauseReason = "NingshaRace_EggCyclePauseDead".Translate();
                return false;
            }
            if (Pawn.gender != Gender.Female)
            {
                pauseReason = "NingshaRace_EggCyclePauseGender".Translate();
                return false;
            }
            if (!Pawn.ageTracker.Adult || Pawn.ageTracker.CurLifeStage.reproductive != true)
            {
                pauseReason = "NingshaRace_EggCyclePauseAge".Translate();
                return false;
            }
            if (NingshaReproductionUtility.HasPregnancyOrLabor(Pawn))
            {
                pauseReason = "NingshaRace_EggCyclePausePregnancy".Translate();
                return false;
            }
            if (Pawn.Sterile())
            {
                pauseReason = "NingshaRace_EggCyclePauseSterile".Translate();
                return false;
            }

            pauseReason = null;
            return true;
        }
    }
}
