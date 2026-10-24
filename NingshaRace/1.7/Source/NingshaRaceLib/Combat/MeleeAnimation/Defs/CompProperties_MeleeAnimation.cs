using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //任意使用原版主手近战装备的 Pawn 都可通过组件配置一套动画。
    public sealed class CompProperties_MeleeAnimation : CompProperties
    {
        public MeleeAnimationDef animation;
        public ThingDef weapon;
        //武器为空时接受当前主手近战武器，指定后只对该武器生效。
        public CompProperties_MeleeAnimation() { compClass = typeof(CompMeleeAnimation); }
        //缺失动画属于配置错误，不静默回退。
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef)) yield return error;
            if (animation == null) yield return parentDef.defName + "：近战组件缺少 animation。";
        }
    }
}
