using Verse;

namespace NingshaRaceLib.GiantTomb.Pawns.Combat
{
    //类职责：保存木乃伊毒砂利爪每次有效命中的石化累计量并绑定专用近战动作。
    public sealed class VerbProperties_GiantTombMummyClaw : VerbProperties
    {
        //字段职责：定义一次造成实际伤害的命中所增加的石化严重度。
        public float petrificationSeverity = 0.2f;

        //构造函数职责：让使用本参数类型的近战动作固定执行木乃伊利爪结算。
        public VerbProperties_GiantTombMummyClaw()
        {
            verbClass = typeof(Verb_GiantTombMummyClaw);
        }
    }
}
