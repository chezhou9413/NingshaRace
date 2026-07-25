using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Race.Rendering.BodyAddons;

namespace NingshaRaceLib.Race.Components
{
    //类职责：声明凝砂族 Pawn 固有能力组件并绑定对应运行时组件。
    public sealed class CompProperties_NingshaInnateAbilities : CompProperties
    {
        //函数职责：初始化组件类型，使种族 Def 能直接创建固有能力组件。
        public CompProperties_NingshaInnateAbilities()
        {
            compClass = typeof(CompNingshaInnateAbilities);
        }
    }
}
