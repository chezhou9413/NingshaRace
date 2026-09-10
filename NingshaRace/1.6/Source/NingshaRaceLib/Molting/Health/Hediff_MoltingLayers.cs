using System.Collections.Generic;
using RimWorld;
using Verse;
using NingshaRaceLib.Molting.Stats;

namespace NingshaRaceLib.Molting.Health
{
    //类职责：按蜕皮层数提供独立的意识与移动能力倍率，交由原版健康系统结算。
    public sealed class Hediff_MoltingLayers : HediffWithComps
    {
        private int cachedCount = -1;
        private HediffStage cachedStage;

        //属性职责：仅在层数变化时重建当前个体的能力修正，不修改共享定义。
        public override HediffStage CurStage
        {
            get
            {
                int count = (int)Severity;
                if (cachedCount != count)
                {
                    cachedStage = new HediffStage
                    {
                        label = "蜕皮者",
                        capMods = new List<PawnCapacityModifier>
                        {
                            new PawnCapacityModifier { capacity = PawnCapacityDefOf.Consciousness, postFactor = MoltingEffects.ConsciousnessFactor(count) },
                            new PawnCapacityModifier { capacity = PawnCapacityDefOf.Moving, postFactor = MoltingEffects.MovingFactor(count) }
                        }
                    };
                    cachedCount = count;
                }
                return cachedStage;
            }
        }
    }
}
