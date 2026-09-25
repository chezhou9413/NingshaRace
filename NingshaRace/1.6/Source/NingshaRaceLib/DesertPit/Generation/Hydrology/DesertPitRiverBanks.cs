using NingshaRaceLib.DesertPit.Generation.Config;
using NingshaRaceLib.DesertPit.Generation.Data;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace NingshaRaceLib.DesertPit.Generation.Hydrology
{
    //类职责：围绕连续河心生成非对称水岸，并由实际水面向外保留可通行河岸。
    internal static class DesertPitRiverBanks
    {
        //函数职责：用缓变河宽和多尺度岸线噪声规划水面，在蚁巢选址前登记完整河谷占地。
        public static void Reserve(Map map, DesertPitLayoutData data, DefModExtension_DesertPitLayout settings)
        {
            ModuleBase widthNoise = new Perlin(0.055, 2.0, 0.5, 2, Rand.Int, QualityMode.Medium);
            ModuleBase edgeNoise = new Perlin(0.15, 2.0, 0.52, 3, Rand.Int, QualityMode.Medium);
            float coreRadius = settings.riverWidth.min * 0.5f;
            foreach (IntVec3 center in data.RiverCenterline)
            {
                float t = Mathf.Clamp01(0.5f + 0.5f * (float)widthNoise.GetValue(center.x, 0, center.z));
                float radius = Mathf.Lerp(settings.riverWidth.min, settings.riverWidth.max, t) * 0.5f;
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius + settings.riverEdgeNoise, true))
                {
                    if (!cell.InBounds(map) || data.RiverWaterCells.Contains(cell)
                        || cell.DistanceToSquared(data.MainCenter) < settings.mainDryRadius * settings.mainDryRadius) continue;
                    //按水岸格自身坐标采样，两岸不再共享同一个圆形宽度；连续河心保留最低净宽。
                    float edge = Mathf.Clamp((float)edgeNoise.GetValue(cell.x, 17, cell.z) * 1.65f, -1f, 1f);
                    float localRadius = Mathf.Max(coreRadius, radius + edge * settings.riverEdgeNoise);
                    if (cell.DistanceToSquared(center) <= localRadius * localRadius) data.RiverWaterCells.Add(cell);
                }
            }
            DesertPitWaterMask.KeepConnected(data.RiverWaterCells, data.RiverCenterline[0]);

            //从真实水面外扩岸带，水面鼓出形成浅湾时仍有完整干岸，而不是切薄原有通道。
            foreach (IntVec3 water in data.RiverWaterCells)
            {
                float extraBank = Mathf.Clamp01((float)edgeNoise.GetValue(water.x + 173, 41, water.z - 97));
                float bankRadius = settings.riverBankWidth + extraBank * settings.riverEdgeNoise * 0.75f;
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(water, bankRadius, true))
                {
                    if (!cell.InBounds(map)) continue;
                    data.RiverCorridorCells.Add(cell);
                    data.ReservedSceneCells.Add(cell);
                    data.ProtectedRouteCells.Add(cell);
                }
            }
        }
    }
}
