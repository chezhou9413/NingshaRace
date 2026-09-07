using Verse;

namespace NingshaRaceLib.Core.Defs
{
    //类职责：集中登记驱蚁、菌巢、吐酸蚁和食用菌采收使用的定义。
    public static partial class DefOfRefs
    {
        public static ThingDef NingshaRace_AntRepellent;
        public static ThingDef NingshaRace_FungalMound;
        public static PawnKindDef NingshaRace_DesertPitAcidAntKind;
        public static HediffDef NingshaRace_AntAcidSlow;
        //字段职责：提供酸液飞行尾迹使用的柔边酸雾和散落液滴定义。
        public static FleckDef NingshaRace_AntAcidTrailMist;
        public static FleckDef NingshaRace_AntAcidTrailDroplet;
        public static JobDef NingshaRace_Job_DesertPitAntHarvest;
        public static ThingDef RawFungus;
    }
}
