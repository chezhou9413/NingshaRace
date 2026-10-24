using HarmonyLib;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Progress
{
    //类职责：阻止尚未初始化完成的凝砂口袋地图执行绘制更新，同时保持地图索引和持有物引用有效。
    [HarmonyPatch(typeof(Map), nameof(Map.MapUpdate))]
    internal static class DesertPitGameUpdatePatches
    {
        //函数职责：只跳过当前正在生成的半成品地图，其他已初始化地图继续正常更新。
        private static bool Prefix(Map __instance)
        {
            return !DesertPitGenerationProgress.Active || __instance != MapGenerator.mapBeingGenerated;
        }
    }

    //类职责：生成期间暂停全局Pawn纹理图集回收，避免扫描半成品地图中的尸体并减少无效工作。
    [HarmonyPatch(typeof(GlobalTextureAtlasManager), nameof(GlobalTextureAtlasManager.GlobalTextureAtlasManagerUpdate))]
    internal static class DesertPitTextureAtlasUpdatePatches
    {
        //函数职责：只在凝砂口袋地图生成流程结束后恢复原版纹理图集更新。
        private static bool Prefix()
        {
            return !DesertPitGenerationProgress.Active;
        }
    }
}
