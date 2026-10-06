using LudeonTK;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Zhenni.Axebearer
{
    //通过开发者工具生成持斧异常，沿用实体阵营和自动近战行为。
    internal static class AxebearerDebugActions
    {
        //在可站立的鼠标位置生成持斧者，独立外观和动作由 Def 决定。
        [DebugAction("NingshaRace", "生成震尼持斧者（敌对异常）", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void Spawn()
        {
            Map map = Find.CurrentMap; IntVec3 cell = Verse.UI.MouseCell();
            if (!cell.InBounds(map) || !cell.Standable(map))
            { Messages.Message("请选择可站立的地图位置。", MessageTypeDefOf.RejectInput, false); return; }
            Pawn pawn = PawnGenerator.GeneratePawn(DefDatabase<PawnKindDef>.GetNamed("NingshaRace_Zhenni_Axebearer_Kind"), Faction.OfEntities, map.Tile);
            GenSpawn.Spawn(pawn, cell, map, Rot4.South);
        }
    }
}
