using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Reproduction.Buildings;
using NingshaRaceLib.Reproduction.Utility;
using NingshaRaceLib.Molting.Components;

namespace NingshaRaceLib.Reproduction.Components
{
    //类职责：保存受精凝砂卵的阵营与孵化进度，并在适宜孵化巢内推进破壳。
    public class CompNingshaEmbryo : ThingComp
    {
        //字段职责：保存当前受精卵的归一化孵化进度。
        private float hatchProgress;

        //字段职责：保存子代破壳时继承的阵营。
        private Faction hatcheeFaction;

        //字段职责：保存受精卵创建瞬间母方蜕皮次数快照，父方和母方后续变化均不参与。
        private int inheritedMoltingCount;

        //属性职责：提供当前受精卵的孵化参数。
        public CompProperties_NingshaEmbryo Props => (CompProperties_NingshaEmbryo)props;

        //属性职责：提供当前孵化进度供孵化巢和开发者工具读取。
        public float HatchProgress => hatchProgress;

        //属性职责：提供破壳子代应当使用的阵营。
        public Faction HatcheeFaction => hatcheeFaction;

        //属性职责：提供子代破壳时应继承的母方蜕皮次数快照。
        public int InheritedMoltingCount => inheritedMoltingCount;

        //属性职责：判断受精卵当前是否处于凝砂孵化巢内。
        public bool IsInsideHatchNest => parent.ParentHolder is Building_NingshaHatchNest;

        //属性职责：从原版父母来源组件中取得凝砂母亲。
        public Pawn Mother => PawnSources.FirstOrDefault(pawn => pawn != null && pawn.gender == Gender.Female && pawn.def == DefOfRefs.NingshaRace);

        //属性职责：从原版父母来源组件中取得男性父亲。
        public Pawn Father => PawnSources.FirstOrDefault(pawn => pawn != null && pawn.gender == Gender.Male);

        //属性职责：取得原版父母来源组件保存的 Pawn 列表。
        private List<Pawn> PawnSources
        {
            get
            {
                CompHasPawnSources sourceComp = parent.TryGetComp<CompHasPawnSources>();
                return sourceComp?.pawnSources ?? new List<Pawn>();
            }
        }

        //函数职责：写入产卵时的父母来源、阵营和母方蜕皮次数快照。
        public void Initialize(Pawn mother, Pawn father, Faction faction)
        {
            CompHasPawnSources sourceComp = parent.TryGetComp<CompHasPawnSources>();
            sourceComp?.AddSource(mother);
            sourceComp?.AddSource(father);
            hatcheeFaction = faction ?? mother?.Faction ?? Faction.OfPlayer;
            inheritedMoltingCount = mother?.TryGetComp<CompNingshaMolting>()?.MoltingCount ?? 0;
        }

        //函数职责：保存孵化进度、破壳阵营和母方蜕皮次数快照。
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref hatchProgress, "ningshaHatchProgress", 0f);
            Scribe_References.Look(ref hatcheeFaction, "ningshaHatcheeFaction");
            Scribe_Values.Look(ref inheritedMoltingCount, "inheritedMoltingCount", 0);
        }

        //函数职责：仅在凝砂孵化巢与安全温度条件同时成立时推进孵化。
        public override void CompTick()
        {
            base.CompTick();
            if (parent.Destroyed || !IsInsideHatchNest || !TemperatureAllowsIncubation())
            {
                return;
            }

            hatchProgress += 1f / (Props.hatchDays * 60000f);
            if (hatchProgress >= 1f)
            {
                HatchNow();
            }
        }

        //函数职责：显示孵化百分比、剩余时间与当前暂停原因。
        public override string CompInspectStringExtra()
        {
            string text = "NingshaRace_HatchProgress".Translate(hatchProgress.ToStringPercent());
            int remainingTicks = Mathf.CeilToInt((1f - hatchProgress) * Props.hatchDays * 60000f);
            text += "\n" + "NingshaRace_HatchTimeRemaining".Translate(remainingTicks.ToStringTicksToPeriod());

            if (!IsInsideHatchNest)
            {
                text += "\n" + "NingshaRace_HatchPausedOutsideNest".Translate();
            }
            else if (!TemperatureAllowsIncubation())
            {
                text += "\n" + "NingshaRace_HatchPausedTemperature".Translate(parent.AmbientTemperature.ToStringTemperature());
            }
            return text;
        }

        //函数职责：仅在开发者模式且受精卵位于孵化巢时提供推进与立即破壳按钮。
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            if (!DebugSettings.godMode || !IsInsideHatchNest)
            {
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = "DEV：孵化进度 +10%",
                defaultDesc = "为巢内受精凝砂卵增加百分之十孵化进度。",
                action = delegate { AddHatchProgress(0.1f); }
            };
            yield return new Command_Action
            {
                defaultLabel = "DEV：立刻孵化",
                defaultDesc = "直接调用正式破壳逻辑。",
                action = HatchNow
            };
        }

        //函数职责：按开发者指定数值推进孵化，并在达到完成值时破壳。
        public void AddHatchProgress(float amount)
        {
            hatchProgress = Mathf.Clamp01(hatchProgress + amount);
            if (hatchProgress >= 1f && IsInsideHatchNest)
            {
                HatchNow();
            }
        }

        //函数职责：校验必须入巢的规则后调用统一破壳生成逻辑。
        public void HatchNow()
        {
            if (!IsInsideHatchNest)
            {
                Log.Error("[NingshaRace] 受精凝砂卵只能在凝砂孵化巢中破壳。");
                return;
            }
            NingshaReproductionUtility.HatchEgg(this);
        }

        //函数职责：判断当前环境温度是否处于 XML 配置的孵化范围内。
        public bool TemperatureAllowsIncubation()
        {
            float temperature = parent.AmbientTemperature;
            return temperature >= Props.minimumTemperature && temperature <= Props.maximumTemperature;
        }
    }
}
