using LudeonTK;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //通过开发者工具生成敌对异常，不接入事件或墓葬遭遇。
    internal static class BladebearerDebugActions
    {
        //在可站立的鼠标位置生成独立刀兵，随后可从 Gizmo 打开编辑器。
        [DebugAction("NingshaRace", "生成震尼拥刀者（敌对异常）", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void Spawn()
        {
            Map map = Find.CurrentMap; IntVec3 cell = Verse.UI.MouseCell();
            if (!cell.InBounds(map) || !cell.Standable(map))
            { Messages.Message("请选择可站立的地图位置。", MessageTypeDefOf.RejectInput, false); return; }
            Pawn pawn = PawnGenerator.GeneratePawn(DefDatabase<PawnKindDef>.GetNamed("NingshaRace_Zhenni_Bladebearer_Kind"), Faction.OfEntities, map.Tile);
            GenSpawn.Spawn(pawn, cell, map, Rot4.South);
        }
    }
}
