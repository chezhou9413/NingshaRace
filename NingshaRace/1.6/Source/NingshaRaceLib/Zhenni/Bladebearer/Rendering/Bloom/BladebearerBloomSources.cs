using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //只收集当前地图本帧已由渲染树绘制的刀兵，隐藏与离图单位不会成为辉光来源。
    internal static class BladebearerBloomSources
    {
        private static readonly BladebearerBloomDraws draws = new BladebearerBloomDraws();
        private static Map map;
        private static int frame = -1;
        internal static BladebearerBloomDraws Current => frame == Time.frameCount && map == Find.CurrentMap ? draws : null;

        //在原版主线程 PostDraw 中接收最终网格与矩阵。
        internal static void Record(PawnRenderNode_BladebearerPart node, Mesh mesh, Matrix4x4 matrix, Color tint)
        {
            Map sourceMap = node.Owner.Pawn.MapHeld;
            if (sourceMap == null || sourceMap != Find.CurrentMap) return;
            if (frame != Time.frameCount || map != sourceMap)
            {
                draws.Clear(map != sourceMap); map = sourceMap; frame = Time.frameCount;
            }
            draws.Add(node, mesh, matrix, tint);
        }

        //无实体残影也使用现有辉光管线，颜色捕获与轮廓消散保持同步。
        internal static void Record(Map sourceMap, BladebearerPart part, Mesh mesh, Matrix4x4 matrix, Color tint, float time, float dissolve)
        {
            if (sourceMap != Find.CurrentMap) return;
            if (frame != Time.frameCount || map != sourceMap)
            {
                draws.Clear(map != sourceMap); map = sourceMap; frame = Time.frameCount;
            }
            draws.Add(part, mesh, matrix, tint, time, dissolve);
        }

        //离开地图时清除帧标记和引用，防止旧地图在相机切换后残留。
        internal static void Clear() { draws.Clear(true); map = null; frame = -1; }
    }
}
