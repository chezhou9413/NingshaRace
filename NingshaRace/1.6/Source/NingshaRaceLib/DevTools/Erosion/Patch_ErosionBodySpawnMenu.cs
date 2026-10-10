using System.Collections.Generic;
using HarmonyLib;
using LudeonTK;
using Verse;

namespace NingshaRaceLib.DevTools.Erosion
{
    //把两种侵蚀体生成工具放入原版 Spawn pawn 的实体分类。
    [HarmonyPatch(typeof(DebugToolsSpawning), "SpawnPawn")]
    internal static class Patch_ErosionBodySpawnMenu
    {
        //仅在启用异象时提供入口，点击回调负责生成和转化。
        [HarmonyPostfix]
        private static void Postfix(List<DebugActionNode> __result)
        {
            if (!ModsConfig.AnomalyActive) return;
            __result.Add(new DebugActionNode("凝砂族侵蚀体", DebugActionType.ToolMap,
                NingshaErosionDebugActions.SpawnErosionBody) { category = "Entity" });
            __result.Add(new DebugActionNode("侵蚀体", DebugActionType.ToolMap,
                NingshaErosionDebugActions.SpawnRandomErosionBody) { category = "Entity" });
        }
    }
}
