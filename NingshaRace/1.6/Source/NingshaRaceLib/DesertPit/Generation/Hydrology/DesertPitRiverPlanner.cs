using System;
using NingshaRaceLib.DesertPit.Generation.Config;
using NingshaRaceLib.DesertPit.Generation.Data;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace NingshaRaceLib.DesertPit.Generation.Hydrology
{
    //类职责：先规划斜穿地图的连续曲流和岸线，再将避开蚁巢的整条河谷雕刻到洞穴中。
    internal static class DesertPitRiverPlanner
    {
        //函数职责：固定两端的斜向展开位置，以连续曲线绕过出生干地，提前保留完整水系占地。
        public static void Prepare(Map map, DesertPitLayoutData data, DefModExtension_DesertPitLayout settings)
        {
            int last = (data.RiverRunsNorthSouth ? map.Size.z : map.Size.x) - 1;
            int lateralLast = (data.RiverRunsNorthSouth ? map.Size.x : map.Size.z) - 1;
            float middle = Axis(data.MainCenter, data.RiverRunsNorthSouth);
            float lateralMiddle = Lateral(data.MainCenter, data.RiverRunsNorthSouth);
            float sign = Rand.Bool ? 1f : -1f;
            data.RiverLateralSlope = sign * settings.riverDiagonalReach * lateralLast / last;
            float bendSign = Rand.Bool ? 1f : -1f;
            float safeRadius = settings.mainDryRadius + settings.riverWidth.max * 0.5f + 2f;
            float bump = (safeRadius + 5f) * Mathf.Sqrt(1f + data.RiverLateralSlope * data.RiverLateralSlope);
            float phase = Rand.Range(0f, Mathf.PI * 2f);
            for (int axis = 0; axis <= last; axis++)
            {
                float u = axis < middle ? (axis - middle) / middle : (axis - middle) / (last - middle);
                float envelope = 1f - u * u;
                float lateral = lateralMiddle + data.RiverLateralSlope * (axis - middle)
                    + bendSign * bump * envelope * envelope * envelope
                    + settings.riverMeander * settings.Scale(map) * Mathf.Sin(u * 6f + phase) * envelope * Mathf.Abs(u);
                //干地由真实圆形距离约束，单调主轴保证河流不会折返、自交或共用支干。
                float axialDistance = axis - middle;
                if (Mathf.Abs(axialDistance) < safeRadius)
                {
                    float limit = Mathf.Sqrt(safeRadius * safeRadius - axialDistance * axialDistance);
                    if (bendSign > 0f) lateral = Mathf.Max(lateral, lateralMiddle + limit);
                    else lateral = Mathf.Min(lateral, lateralMiddle - limit);
                }
                lateral = Mathf.Clamp(lateral, 4f, lateralLast - 4f);
                IntVec3 next = data.RiverRunsNorthSouth ? new IntVec3(Mathf.RoundToInt(lateral), 0, axis)
                    : new IntVec3(axis, 0, Mathf.RoundToInt(lateral));
                if (data.RiverCenterline.Count == 0) data.RiverCenterline.Add(next);
                else
                    foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(data.RiverCenterline[data.RiverCenterline.Count - 1], next))
                        if (cell != data.RiverCenterline[data.RiverCenterline.Count - 1]) data.RiverCenterline.Add(cell);
            }
            ReserveCorridor(map, data, settings);
        }

        //函数职责：确认完整蚁巢没有覆盖预留河谷后开挖水道，保持所有地图写入在生成线程完成。
        public static void Plan(Map map, DesertPitLayoutData data, DefModExtension_DesertPitLayout settings)
        {
            foreach (var room in data.AntChambers)
                foreach (IntVec3 cell in room.Footprint)
                    if (data.RiverCorridorCells.Contains(cell))
                        throw new InvalidOperationException("蚁巢岩层侵入了预留斜向河谷，请检查中层布局参数。");
            foreach (IntVec3 cell in data.RiverCorridorCells) MapGenerator.Caves[cell] = 1f;
        }

        //函数职责：沿真实河心记录变宽水面与两侧岸线，供蚁巢选址和后续场景生成共同避让。
        private static void ReserveCorridor(Map map, DesertPitLayoutData data, DefModExtension_DesertPitLayout settings)
        {
            ModuleBase noise = new Perlin(0.055, 2.0, 0.5, 2, Rand.Int, QualityMode.Medium);
            foreach (IntVec3 center in data.RiverCenterline)
            {
                float t = Mathf.Clamp01(0.5f + 0.5f * (float)noise.GetValue(center.x, 0, center.z));
                float radius = Mathf.Lerp(settings.riverWidth.min, settings.riverWidth.max, t) * 0.5f;
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius + settings.riverBankWidth, true))
                {
                    if (!cell.InBounds(map)) continue;
                    data.RiverCorridorCells.Add(cell);
                    data.ReservedSceneCells.Add(cell);
                    data.ProtectedRouteCells.Add(cell);
                    if (cell.DistanceToSquared(center) <= radius * radius) data.RiverWaterCells.Add(cell);
                }
            }
        }

        //函数职责：取得河道主方向坐标，统一处理从两组相对边缘进入地图的河流。
        internal static int Axis(IntVec3 cell, bool northSouth) => northSouth ? cell.z : cell.x;

        //函数职责：取得垂直于地图主轴的坐标，配合斜率计算河谷两侧位置。
        internal static int Lateral(IntVec3 cell, bool northSouth) => northSouth ? cell.x : cell.z;

        //函数职责：把地图格子投影到斜向河谷的法线，供中层蚁巢分居两侧。
        internal static float Across(DesertPitLayoutData data, IntVec3 cell)
        {
            IntVec3 delta = cell - data.MainCenter;
            return (Lateral(delta, data.RiverRunsNorthSouth) - Axis(delta, data.RiverRunsNorthSouth) * data.RiverLateralSlope)
                / Mathf.Sqrt(1f + data.RiverLateralSlope * data.RiverLateralSlope);
        }

        //函数职责：取得斜向河谷的单位法向，供洞室沿河道两岸交错选址。
        internal static Vector3 Side(DesertPitLayoutData data)
            => (data.RiverRunsNorthSouth ? new Vector3(1f, 0f, -data.RiverLateralSlope)
                : new Vector3(-data.RiverLateralSlope, 0f, 1f)).normalized;
    }
}
