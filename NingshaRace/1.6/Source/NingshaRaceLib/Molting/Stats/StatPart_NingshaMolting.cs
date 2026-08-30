using RimWorld;
using Verse;

using NingshaRaceLib.Molting.Components;

namespace NingshaRaceLib.Molting.Stats
{
    //类职责：按蜕皮次数线性计算治疗倍率、移动、痛觉休克阈值和侵蚀上限增量。
    public sealed class StatPart_NingshaMolting : StatPart
    {
        //字段职责：指定当前StatPart采用的四种线性计算模式之一。
        public string mode;

        //函数职责：仅对具有蜕皮组件的Pawn按层数变换最终属性值。
        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn = req.Thing as Pawn;
            int count = pawn?.TryGetComp<CompNingshaMolting>()?.MoltingCount ?? 0;
            if (count <= 0)
            {
                return;
            }
            if (mode == "HealingFactor") val *= 1f + 0.02f * count;
            else if (mode == "MoveSpeed") val += 0.015f * count;
            else if (mode == "PainShockThreshold") val += 0.005f * count;
            else if (mode == "ErosionLimit") val += 0.5f * count;
        }

        //函数职责：在属性说明中展示蜕皮层数与该属性的实际线性修正。
        public override string ExplanationPart(StatRequest req)
        {
            Pawn pawn = req.Thing as Pawn;
            int count = pawn?.TryGetComp<CompNingshaMolting>()?.MoltingCount ?? 0;
            if (count <= 0)
            {
                return null;
            }
            if (mode == "HealingFactor") return "蜕皮者（" + count + "次）：×" + (1f + 0.02f * count).ToString("0.##");
            float value = mode == "MoveSpeed" ? 0.015f * count : mode == "PainShockThreshold" ? 0.005f * count : 0.5f * count;
            return "蜕皮者（" + count + "次）：+" + value.ToString("0.###");
        }
    }
}
