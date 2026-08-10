using System;
using HarmonyLib;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Progress
{
    //类职责：在当前场景更新期间临时隐藏尚未初始化完成的巨坑地图，避免原版更新其空绘制分区。
    [HarmonyPatch(typeof(Game), nameof(Game.UpdatePlay))]
    internal static class DesertPitGameUpdatePatches
    {
        //字段职责：保存当前游戏更新期间临时从地图列表摘出的半成品地图。
        private static Map detachedGeneratingMap;

        //函数职责：在其他游戏更新逻辑运行前摘出半成品地图，同时保留原地图场景正常刷新。
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            Map generatingMap = MapGenerator.mapBeingGenerated;
            if (!DesertPitGenerationProgress.Active || generatingMap == null || !Current.Game.Maps.Contains(generatingMap))
            {
                detachedGeneratingMap = null;
                return;
            }

            Current.Game.Maps.Remove(generatingMap);
            detachedGeneratingMap = generatingMap;
        }

        //函数职责：无论本帧游戏更新是否异常都恢复生成地图，使下一批生成步骤维持原版地图上下文。
        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        private static Exception Finalizer(Exception __exception)
        {
            if (detachedGeneratingMap != null && !Current.Game.Maps.Contains(detachedGeneratingMap))
            {
                Current.Game.Maps.Add(detachedGeneratingMap);
            }

            detachedGeneratingMap = null;
            return __exception;
        }
    }
}
