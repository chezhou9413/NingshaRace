using RimWorld;
using Verse;

namespace NingshaRaceLib.GiantTomb.Discovery.Components
{
    //类职责：声明破损石棺接近提示使用的侦测半径、信件文字和信件类型。
    public sealed class CompProperties_GiantTombCoffinProximity : CompProperties
    {
        //字段职责：规定玩家殖民者触发破损石棺提示的最大距离。
        public int radius = 8;

        //字段职责：保存接近破损石棺时发送的信件标题。
        [MustTranslate]
        public string letterLabel;

        //字段职责：保存接近破损石棺时发送的信件正文。
        [MustTranslate]
        public string letterText;

        //字段职责：指定接近提示使用的原版信件类型。
        public LetterDef letterDef;

        //构造函数职责：把配置绑定到破损石棺接近提示组件。
        public CompProperties_GiantTombCoffinProximity()
        {
            compClass = typeof(Comp_GiantTombCoffinProximity);
        }
    }
}
