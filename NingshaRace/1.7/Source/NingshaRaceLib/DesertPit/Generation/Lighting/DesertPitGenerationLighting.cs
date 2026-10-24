using System;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Lighting
{
    //类职责：在地下地图生成期间刷新真实光照，并限定尚未创建绘制分区时的通知范围。
    internal static class DesertPitGenerationLighting
    {
        [ThreadStatic]
        private static MapDrawer refreshingDrawer;

        //函数职责：同步完成原版光照计算，调用结束或异常时恢复当前线程的绘制通知上下文。
        public static void Refresh(Map map)
        {
            MapDrawer previous = refreshingDrawer;
            refreshingDrawer = map.mapDrawer;
            try
            {
                map.glowGrid.GlowGridUpdate_First();
            }
            finally
            {
                refreshingDrawer = previous;
            }
        }

        //函数职责：判断绘制通知是否来自当前地下生成步骤正在刷新的同一张地图。
        internal static bool IsRefreshing(MapDrawer drawer)
            => ReferenceEquals(refreshingDrawer, drawer);
    }
}
