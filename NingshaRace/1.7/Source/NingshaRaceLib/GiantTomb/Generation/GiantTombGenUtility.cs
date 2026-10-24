using System;
using Verse;

namespace NingshaRaceLib.GiantTomb.Generation
{
    //类职责：提供巨型墓葬生成步骤共享的运行数据和进度报告入口。
    internal static class GiantTombGenUtility
    {
        private const string LayoutKey = "NingshaRace_GiantTombLayout";

        //函数职责：取得当前地图生成流程已经建立的巨型墓葬布局数据。
        public static GiantTombLayoutData GetLayoutData()
        {
            GiantTombLayoutData data = MapGenerator.GetVar<GiantTombLayoutData>(LayoutKey);
            if (data == null)
            {
                throw new InvalidOperationException("巨型墓葬布局数据尚未建立");
            }
            return data;
        }

        //函数职责：把求解完成的布局数据登记到当前地图生成上下文。
        public static void SetLayoutData(GiantTombLayoutData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            MapGenerator.SetVar(LayoutKey, data);
        }
    }
}
