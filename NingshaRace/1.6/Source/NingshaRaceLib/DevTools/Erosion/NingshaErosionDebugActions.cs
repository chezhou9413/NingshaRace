using LudeonTK;
using Verse;

using NingshaRaceLib.Erosion.Utility;

namespace NingshaRaceLib.DevTools.Erosion
{
    //类职责：向开发者菜单注册可在鼠标位置快速生成侵蚀体的地图工具。
    public static class NingshaErosionDebugActions
    {
        //函数职责：在当前鼠标空格调用统一生成工具放置一名侵蚀体。
        [DebugAction("NingshaRace", "生成侵蚀体", actionType = DebugActionType.ToolMap,
            allowedGameStates = AllowedGameStates.PlayingOnMap, requiresAnomaly = true)]
        public static void SpawnErosionBody()
        {
            ErosionBodySpawnUtility.Spawn(Find.CurrentMap, Verse.UI.MouseCell());
        }
    }
}
