using Verse;

namespace NingshaRaceLib.Combat
{
    //类职责：识别由坠岳斩 PawnFlyer 持有的飞行中武器，避免原版装备绘制与自定义挥刀重叠。
    public static class FallingMountainSlashRenderUtility
    {
        //函数职责：沿装备持有链判断武器所属 Pawn 是否正在执行坠岳斩飞行。
        public static bool ShouldHideOriginalWeapon(Thing equipment)
        {
            Pawn_EquipmentTracker tracker = equipment?.ParentHolder as Pawn_EquipmentTracker;
            Pawn pawn = tracker?.pawn;
            return pawn?.ParentHolder is PawnFlyer_FallingMountainSlash;
        }
    }
}
