using RimWorld;
using Verse;

using NingshaRaceLib.Molting.Components;

namespace NingshaRaceLib.Molting.Stats
{
    //类职责：应用蜕皮的愈合、承伤、闪避、寿命及侵蚀上限修正。
    public sealed class StatPart_NingshaMolting : StatPart
    {
        //字段职责：指定当前属性的计算模式。
        public string mode;

        //函数职责：仅对具有蜕皮组件的Pawn按层数变换最终属性值。
        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn = req.Thing as Pawn;
            CompNingshaMolting molting = pawn?.TryGetComp<CompNingshaMolting>();
            if (molting == null)
            {
                return;
            }
            float modifier = MoltingEffects.StatModifier(mode, molting.MoltingCount);
            if (MoltingEffects.IsOffset(mode)) val += modifier;
            else val *= modifier;
        }

        //函数职责：在属性说明中展示蜕皮层数与该属性的实际线性修正。
        public override string ExplanationPart(StatRequest req)
        {
            Pawn pawn = req.Thing as Pawn;
            CompNingshaMolting molting = pawn?.TryGetComp<CompNingshaMolting>();
            if (molting == null)
            {
                return null;
            }
            int count = molting.MoltingCount;
            float modifier = MoltingEffects.StatModifier(mode, count);
            string value = mode == "MeleeDodgeChance" ? (modifier * 100f).ToString("0.##") + " 个百分点" : modifier.ToString("0.###");
            return "蜕皮者（" + count + "层）：" + (MoltingEffects.IsOffset(mode) ? "+" : "×") + value;
        }
    }
}
