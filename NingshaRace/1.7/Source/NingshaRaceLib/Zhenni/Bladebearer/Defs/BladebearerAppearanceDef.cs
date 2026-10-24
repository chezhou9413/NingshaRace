using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //定义刀兵全部图层的默认外观，不向存档写入编辑器草稿。
    public sealed class BladebearerAppearanceDef : Def
    {
        public List<BladebearerPart> parts = new List<BladebearerPart>();

        //在加载 Def 时报告非法参数。
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors()) yield return error;
            foreach (string error in BladebearerValidation.Errors(parts)) yield return error;
        }

        //为编辑窗口创建独立草稿。
        public List<BladebearerPart> CopyParts() => parts.Select(p => p.Copy()).ToList();
    }
}
