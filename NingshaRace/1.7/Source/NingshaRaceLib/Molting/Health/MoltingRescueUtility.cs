using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Molting.Components;

namespace NingshaRaceLib.Molting.Health
{
    //类职责：判定伤势型濒死状态并按五伤口规则执行一次不可递归的蜕皮保命治疗。
    public static class MoltingRescueUtility
    {
        //字段职责：阻止移除和调整Hediff时再次进入同一Pawn的健康结算。
        private static readonly HashSet<int> ResolvingPawnIds = new HashSet<int>();

        //字段职责：在不触发健康状态结算的情况下临时调整严重度，用于保命结果模拟。
        private static readonly AccessTools.FieldRef<Hediff, float> SeverityField =
            AccessTools.FieldRefAccess<Hediff, float>("severityInt");

        //函数职责：只对具有就绪组件、仍存活且由伤口造成倒地或致命伤害的凝砂族尝试治疗。
        public static void TryResolve(Pawn pawn)
        {
            CompNingshaMolting comp = pawn?.TryGetComp<CompNingshaMolting>();
            if (comp == null || !comp.RescueReady || pawn.Dead || !ResolvingPawnIds.Add(pawn.thingIDNumber))
            {
                return;
            }
            try
            {
                List<Hediff_Injury> injuries = pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>()
                    .Where(injury => injury.Severity > 0f).OrderBy(injury => injury.Severity).ToList();
                bool injuryEmergency = injuries.Count > 0
                    && (pawn.Downed || pawn.health.ShouldBeDeadFromLethalDamageThreshold());
                if (!injuryEmergency || HasFatalMissingPart(pawn))
                {
                    return;
                }
                if (!CanRecoverAfterTreatment(pawn, injuries))
                {
                    return;
                }
                ApplyInjuryTreatment(pawn, injuries);
                RemoveBloodLoss(pawn);
                pawn.health.hediffSet.DirtyCache();
                if (pawn.health.ShouldBeDeadFromRequiredCapacity() != null
                    || PawnCapacityUtility.CalculatePartEfficiency(pawn.health.hediffSet, pawn.RaceProps.body.corePart) <= 0.0001f
                    || pawn.health.ShouldBeDeadFromLethalDamageThreshold() || pawn.health.ShouldBeDowned())
                {
                    throw new System.InvalidOperationException("蜕皮保命治疗后Pawn仍处于必死状态，伤势模拟判定不一致。" + pawn);
                }
                comp.ConsumeRescueNutrition();
                Messages.Message(pawn.LabelShortCap + "消耗蜕皮营养，从致命伤势中恢复了意识。", pawn,
                    MessageTypeDefOf.PositiveEvent, false);
            }
            finally
            {
                ResolvingPawnIds.Remove(pawn.thingIDNumber);
            }
        }

        //函数职责：拒绝头部、躯干、大脑、心脏或肝脏等致命部位缺失的情况且不消耗营养。
        private static bool HasFatalMissingPart(Pawn pawn)
        {
            BodyPartRecord core = pawn.RaceProps.body.corePart;
            if (!pawn.health.hediffSet.HasBodyPart(core) || !pawn.health.hediffSet.HasHead
                || pawn.health.hediffSet.GetBrain() == null)
            {
                return true;
            }
            foreach (Hediff_MissingPart missing in pawn.health.hediffSet.hediffs.OfType<Hediff_MissingPart>())
            {
                string name = missing.Part?.def?.defName;
                if (name == "Heart" || name == "Liver" || name == "Torso" || name == "Head" || name == "Brain")
                {
                    return true;
                }
            }
            return false;
        }

        //函数职责：临时套用五伤口结果并清空失血，确认Pawn可恢复行动后完整还原健康数据。
        private static bool CanRecoverAfterTreatment(Pawn pawn, List<Hediff_Injury> injuries)
        {
            Hediff bloodLoss = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
            List<Hediff> simulated = new List<Hediff>(injuries.Count + (bloodLoss == null ? 0 : 1));
            simulated.AddRange(injuries);
            if (bloodLoss != null) simulated.Add(bloodLoss);
            float[] originalSeverities = simulated.Select(hediff => hediff.Severity).ToArray();
            int keepCount = injuries.Count < 5 ? 0 : 5;
            try
            {
                for (int i = 0; i < injuries.Count; i++) SeverityField(injuries[i]) = i < keepCount ? 1f : 0f;
                if (bloodLoss != null) SeverityField(bloodLoss) = 0f;
                pawn.health.hediffSet.DirtyCache();
                return pawn.health.ShouldBeDeadFromRequiredCapacity() == null
                    && PawnCapacityUtility.CalculatePartEfficiency(pawn.health.hediffSet, pawn.RaceProps.body.corePart) > 0.0001f
                    && !pawn.health.ShouldBeDeadFromLethalDamageThreshold()
                    && !pawn.health.ShouldBeDowned();
            }
            finally
            {
                for (int i = 0; i < simulated.Count; i++) SeverityField(simulated[i]) = originalSeverities[i];
                pawn.health.hediffSet.DirtyCache();
            }
        }

        //函数职责：少于五伤口时全部移除，否则保留五个最轻伤口并统一为一点严重度。
        private static void ApplyInjuryTreatment(Pawn pawn, List<Hediff_Injury> injuries)
        {
            int keepCount = injuries.Count < 5 ? 0 : 5;
            for (int i = injuries.Count - 1; i >= keepCount; i--)
            {
                pawn.health.RemoveHediff(injuries[i]);
            }
            for (int i = 0; i < keepCount; i++)
            {
                injuries[i].Severity = 1f;
                pawn.health.Notify_HediffChanged(injuries[i]);
            }
        }

        //函数职责：仅移除伤势关联的原版失血状态，不清除疾病或其他非伤害Hediff。
        private static void RemoveBloodLoss(Pawn pawn)
        {
            Hediff bloodLoss = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
            if (bloodLoss != null)
            {
                pawn.health.RemoveHediff(bloodLoss);
            }
        }
    }
}
