using LudeonTK;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//独立开发者生成入口，不改变随机遭遇。
internal static class BowbearerDebugActions
{
    //在合法鼠标位置生成敌对挽弓异常。
    [DebugAction("NingshaRace", "生成震尼挽弓者（敌对异常）", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void Spawn()
    {
        Map map = Find.CurrentMap; IntVec3 cell = Verse.UI.MouseCell();
        if (!cell.InBounds(map) || !cell.Standable(map))
        { Messages.Message("请选择可站立的地图位置。", MessageTypeDefOf.RejectInput, false); return; }
        Pawn pawn = PawnGenerator.GeneratePawn(DefDatabase<PawnKindDef>.GetNamed("NingshaRace_Zhenni_Bowbearer_Kind"), Faction.OfEntities, map.Tile);
        GenSpawn.Spawn(pawn, cell, map, Rot4.South);
    }
}
