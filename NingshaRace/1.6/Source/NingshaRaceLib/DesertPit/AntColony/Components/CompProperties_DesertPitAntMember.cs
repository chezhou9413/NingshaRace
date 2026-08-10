using Verse;

using NingshaRaceLib.DesertPit.AntColony.Core;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //类职责：为蚂蚁 ThingDef 指定固定阶级并创建巢群成员组件。
    public class CompProperties_DesertPitAntMember : CompProperties
    {
        public AntCaste caste;

        //构造函数职责：把属性类型绑定到沙漠巨坑蚁群成员组件。
        public CompProperties_DesertPitAntMember()
        {
            compClass = typeof(Comp_DesertPitAntMember);
        }
    }
}
