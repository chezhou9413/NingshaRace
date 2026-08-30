using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Molting.Components
{
    //类职责：保存凝砂族进食累计营养与蜕皮次数，并维护防死亡就绪状态。
    public sealed class CompNingshaMolting : ThingComp
    {
        //字段职责：保存通过实际摄入营养累计的蜕皮资源。
        private float moltingNutrition;

        //字段职责：保存已经完成并参与属性计算的蜕皮次数。
        private int moltingCount;

        //属性职责：提供蜕皮系统配置。
        public CompProperties_NingshaMolting Props => (CompProperties_NingshaMolting)props;

        //属性职责：取得组件所属凝砂族Pawn。
        public Pawn Pawn => (Pawn)parent;

        //属性职责：提供当前蜕皮营养。
        public float MoltingNutrition => moltingNutrition;

        //属性职责：提供钳制在零至二十的蜕皮层数。
        public int MoltingCount => moltingCount;

        //属性职责：判断当前营养是否足以启用伤势保命。
        public bool RescueReady => moltingNutrition >= Props.rescueNutritionCost;

        //函数职责：保存蜕皮营养与次数并在读档后校正范围和就绪状态。
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref moltingNutrition, "moltingNutrition", 0f);
            Scribe_Values.Look(ref moltingCount, "moltingCount", 0);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                moltingNutrition = Mathf.Clamp(moltingNutrition, 0f, Props.nutritionCapacity);
                moltingCount = Mathf.Clamp(moltingCount, 0, 20);
                SynchronizeHediffs();
            }
        }

        //函数职责：生成或读档后立即确保防死亡就绪Hediff与营养状态一致。
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            SynchronizeHediffs();
        }

        //函数职责：每六十Tick校正一次Hediff，覆盖世界Pawn和异常外部营养修改。
        public override void CompTick()
        {
            base.CompTick();
            if (Pawn.IsHashIntervalTick(60))
            {
                SynchronizeHediffs();
            }
        }

        //函数职责：按Thing.Ingested返回的实际营养累计蜕皮资源。
        public void AddIngestedNutrition(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }
            moltingNutrition = Mathf.Min(Props.nutritionCapacity, moltingNutrition + amount);
            SynchronizeHediffs();
        }

        //函数职责：在未满二十层且营养充足时消耗一百并立即增加一层蜕皮。
        public void PerformMolting()
        {
            if (moltingCount >= 20 || moltingNutrition < Props.nutritionCapacity)
            {
                return;
            }
            moltingNutrition -= Props.nutritionCapacity;
            moltingCount++;
            SynchronizeHediffs();
            Messages.Message(Pawn.LabelShortCap + "完成了第" + moltingCount + "次蜕皮。", Pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        //函数职责：由保命结算在成功治疗后扣除六十营养并撤销就绪状态。
        public void ConsumeRescueNutrition()
        {
            moltingNutrition = Mathf.Max(0f, moltingNutrition - Props.rescueNutritionCost);
            SynchronizeHediffs();
        }

        //函数职责：在孵化继承时直接应用母方产卵时快照的蜕皮次数。
        public void ApplyInheritedCount(int count)
        {
            moltingCount = Mathf.Clamp(count, 0, 20);
            SynchronizeHediffs();
        }

        //函数职责：为未满二十层且营养达到一百的玩家凝砂族显示即时蜕皮命令。
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            if (Pawn.Faction != Faction.OfPlayer || moltingCount >= 20 || moltingNutrition < Props.nutritionCapacity)
            {
                yield break;
            }
            yield return new Command_Action
            {
                defaultLabel = "蜕皮",
                defaultDesc = "消耗100点蜕皮营养，永久增加一层蜕皮者状态。",
                action = PerformMolting
            };
        }

        //函数职责：显示蜕皮次数、营养和当前保命就绪状态。
        public override string CompInspectStringExtra()
        {
            return "蜕皮者：" + moltingCount + " / 20\n蜕皮营养：" + moltingNutrition.ToString("0.##")
                + " / " + Props.nutritionCapacity.ToString("0.##")
                + (RescueReady ? "\n伤势保命：就绪" : string.Empty);
        }

        //函数职责：同步可见层数Hediff和隐藏防死亡就绪Hediff，不触碰其他健康状态。
        private void SynchronizeHediffs()
        {
            if (Pawn.health == null)
            {
                return;
            }
            Hediff layers = Pawn.health.hediffSet.GetFirstHediffOfDef(DefOfRefs.NingshaRace_MoltingLayers);
            if (moltingCount > 0)
            {
                if (layers == null)
                {
                    layers = HediffMaker.MakeHediff(DefOfRefs.NingshaRace_MoltingLayers, Pawn);
                    Pawn.health.AddHediff(layers);
                }
                layers.Severity = moltingCount;
            }
            else if (layers != null)
            {
                Pawn.health.RemoveHediff(layers);
            }
            Hediff ready = Pawn.health.hediffSet.GetFirstHediffOfDef(DefOfRefs.NingshaRace_MoltingRescueReady);
            if (RescueReady && ready == null)
            {
                Pawn.health.AddHediff(DefOfRefs.NingshaRace_MoltingRescueReady);
            }
            else if (!RescueReady && ready != null)
            {
                Pawn.health.RemoveHediff(ready);
            }
        }
    }
}
