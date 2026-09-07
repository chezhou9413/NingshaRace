using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.DesertPit.Antlion.Generation
{
    //类职责：通过地下生态 XML 配置蚁狮数量、场景避让距离和生成扫描批次。
    public sealed class DefModExtension_DesertPitAntlion : DefModExtension
    {
        public IntRange countRange = new IntRange(2, 3);
        public float entranceAvoidRadius = 25f;
        public float antNestAvoidRadius = 24f;
        public float spacing = 18f;
        public int scanBatchSize = 512;

        //函数职责：拒绝无效数量、距离和批次，允许将数量设为零以关闭自然生成。
        public override IEnumerable<string> ConfigErrors()
        {
            if (countRange.min < 0 || countRange.max < countRange.min)
                yield return "蚁狮生成数量必须为非负且有序的范围。";
            if (!ValidDistance(entranceAvoidRadius) || !ValidDistance(antNestAvoidRadius) || !ValidDistance(spacing))
                yield return "蚁狮生成避让距离必须为非负有限数值。";
            if (scanBatchSize <= 0) yield return "蚁狮生成扫描批次必须大于零。";
        }

        //函数职责：验证避让参数不会因无穷值或非数值破坏候选筛选。
        private static bool ValidDistance(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
        }
    }
}
