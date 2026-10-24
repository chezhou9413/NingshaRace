using System;
using NingshaRaceLib.DesertPit.Generation.Config;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Hydrology;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace NingshaRaceLib.DesertPit.Generation.Habitats
{
    //类职责：在生态洞室中规划具有不等长轴、浅湾和连续岸线噪声的单片水潭。
    internal static class DesertPitPondPlanner
    {
        //函数职责：按实际洞壁限制基准半径，生成不对称水面并保留干岸、入口和中心连通区域。
        public static void Plan(DesertPitLayoutData data, DesertPitHabitat habitat, DefModExtension_DesertPitLayout settings)
        {
            float clearance = Mathf.Min(habitat.room.RadiusX, habitat.room.RadiusZ);
            foreach (IntVec3 cell in habitat.floor)
                foreach (IntVec3 offset in GenAdj.CardinalDirections)
                    if (!habitat.floor.Contains(cell + offset)) clearance = Mathf.Min(clearance, (cell + offset).DistanceTo(habitat.room.Center));
            float radius = Mathf.Min(settings.habitatPondRadius.RandomInRange, clearance - 1f);
            if (radius < settings.habitatPondRadius.min)
                throw new InvalidOperationException("生态洞室无法容纳水潭与岸带，请增大生态洞室半径。");

            float rotation = Rand.Range(0f, 2f * Mathf.PI);
            float phase = Rand.Range(0f, 2f * Mathf.PI);
            float cosine = Mathf.Cos(rotation);
            float sine = Mathf.Sin(rotation);
            float longRadius = radius * Rand.Range(1.05f, 1.18f);
            float shortRadius = radius * Rand.Range(0.78f, 0.96f);
            ModuleBase noise = new Perlin(0.65f / radius, 2.0, 0.52, 3, Rand.Int, QualityMode.Medium);
            foreach (IntVec3 cell in habitat.floor)
            {
                if (data.ProtectedRouteCells.Contains(cell) || !HasDryRim(habitat, cell)) continue;
                IntVec3 delta = cell - habitat.room.Center;
                float u = (delta.x * cosine + delta.z * sine) / longRadius;
                float v = (-delta.x * sine + delta.z * cosine) / shortRadius;
                float angle = Mathf.Atan2(v, u);
                //同一方向只使用一个边界半径，避免对潭内逐格扰动而打出空洞；环形采样在角度接缝处连续。
                float sampleX = habitat.room.Center.x + Mathf.Cos(angle) * radius;
                float sampleZ = habitat.room.Center.z + Mathf.Sin(angle) * radius;
                float detail = Mathf.Clamp((float)noise.GetValue(sampleX, 23, sampleZ) * 1.65f, -1f, 1f);
                float edge = 0.45f * Mathf.Sin(angle * 3f + phase) + 0.55f * detail;
                float boundary = 1f + settings.habitatPondEdgeNoise * edge;
                if (u * u + v * v <= boundary * boundary) habitat.water.Add(cell);
            }
            DesertPitWaterMask.KeepConnected(habitat.water, habitat.room.Center);
        }

        //函数职责：要求水格四周仍有洞室地面，防止外凸水岸直接贴住岩墙而吃掉最后一圈干岸。
        private static bool HasDryRim(DesertPitHabitat habitat, IntVec3 cell)
        {
            foreach (IntVec3 offset in GenAdj.AdjacentCells)
                if (!habitat.floor.Contains(cell + offset)) return false;
            return true;
        }
    }
}
