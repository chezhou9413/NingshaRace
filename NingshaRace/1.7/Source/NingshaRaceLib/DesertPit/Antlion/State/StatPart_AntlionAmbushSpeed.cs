using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Antlion.Components;
using RimWorld;

namespace NingshaRaceLib.DesertPit.Antlion.State
{
    //类职责：仅对蚁狮应用组件给出的短时移动倍率，其他种族的移速不受影响。
    public sealed class StatPart_AntlionAmbushSpeed : StatPart
    {
        //函数职责：从当前实体状态读取倍率，不缓存绝对到期时间之外的加速结果。
        public override void TransformValue(StatRequest req, ref float val)
        {
            val *= Factor(req);
        }

        //函数职责：在生物属性页使用普通中文说明当前生效的突袭移速。
        public override string ExplanationPart(StatRequest req)
        {
            float factor = Factor(req);
            return factor > 1f ? "出土突袭：×" + factor.ToString("0.##") : null;
        }

        //函数职责：先按种族快速排除其他实体，再读取蚁狮组件倍率。
        private static float Factor(StatRequest req)
        {
            if (!req.HasThing || req.Thing.def != DefOfRefs.NingshaRace_Antlion) return 1f;
            return ((Verse.Pawn)req.Thing).GetComp<CompAntlionAmbush>().SpeedFactor;
        }
    }
}
