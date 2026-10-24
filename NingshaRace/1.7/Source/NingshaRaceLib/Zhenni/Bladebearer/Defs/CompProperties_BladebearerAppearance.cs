using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //把震尼系列实体与分层外观及真实装备关联起来。
    public sealed class CompProperties_BladebearerAppearance : CompProperties
    {
        public BladebearerAppearanceDef appearance;
        public ThingDef weapon;
        public string weaponPart = "Dagger";

        //沿用系列的分层渲染与特效组件。
        public CompProperties_BladebearerAppearance() { compClass = typeof(CompBladebearerAppearance); }

        //实体刃段必须指向当前外观里的武器部件，供握点和轨迹共同采样。
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef)) yield return error;
            if (appearance == null) yield return "appearance：缺少震尼外观定义。";
            else if (!appearance.parts.Exists(p => p.id == weaponPart && p.attachment == "Weapon" && !p.flame && !p.particle))
                yield return "weaponPart：外观中不存在实体武器部件「" + weaponPart + "」。";
        }
    }
}
