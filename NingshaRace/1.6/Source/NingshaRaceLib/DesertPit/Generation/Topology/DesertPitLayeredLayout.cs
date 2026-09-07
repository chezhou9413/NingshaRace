using System;
using System.Collections;
using System.Collections.Generic;
using NingshaRaceLib.DesertPit.AntColony.Generation.Chambers;
using NingshaRaceLib.DesertPit.Generation.Config;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Hydrology;
using NingshaRaceLib.DesertPit.Generation.Progress;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Topology
{
    //类职责：按中央洞群、蚁巢、水系和次要洞群的顺序串行生成分层洞穴拓扑。
    internal static class DesertPitLayeredLayout
    {
        //函数职责：在各结构完成后交还生成帧，只在同一生成线程提交洞穴掩码。
        public static IEnumerable Generate(Map map, DesertPitLayoutData data, DefModExtension_DesertPitLayout settings)
        {
            Reset(data);
            data.RiverRunsNorthSouth = Rand.Bool;
            BuildCentralRooms(map, data, settings);
            DesertPitRiverPlanner.Prepare(map, data, settings);
            DesertPitGenerationProgress.SetStepFraction(0.18f);
            yield return null;
            AntChamberPlanner.Plan(map, data);
            DesertPitGenerationProgress.SetStepFraction(0.4f);
            yield return null;
            DesertPitRiverPlanner.Plan(map, data, settings);
            DesertPitGenerationProgress.SetStepFraction(0.6f);
            yield return null;
            int missing = DesertPitSecondaryRooms.Generate(map, data, settings, false);
            DesertPitGenerationProgress.SetStepFraction(0.75f);
            yield return null;
            missing += DesertPitSecondaryRooms.Generate(map, data, settings, true);
            if (missing > 0) Log.Warning($"[凝砂族] 河谷有 {missing} 处可选小洞室因岩层占用或重叠过密未放置，主次洞群与河流不受影响。");
            DesertPitGenerationProgress.SetStepFraction(0.9f);
            yield return null;
            Validate(map, data, settings);
        }

        //函数职责：清空仅用于本次布局的结构缓存，不触及其他地图和存档对象。
        private static void Reset(DesertPitLayoutData data)
        {
            data.Rooms.Clear();
            data.SecondaryRooms.Clear();
            data.CentralCoreCells.Clear();
            data.RiverCenterline.Clear();
            data.RiverWaterCells.Clear();
            data.RiverCorridorCells.Clear();
        }

        //函数职责：生成一个宽阔主洞和沿不同方位相接的侧室，保留出生核心与各洞室中心。
        private static void BuildCentralRooms(Map map, DesertPitLayoutData data, DefModExtension_DesertPitLayout settings)
        {
            float scale = settings.Scale(map);
            data.MainCenter = map.Center + new IntVec3(Rand.RangeInclusive(-2, 2), 0, Rand.RangeInclusive(-2, 2));
            data.MainRadiusX = settings.mainRadiusX.RandomInRange * scale;
            data.MainRadiusZ = settings.mainRadiusZ.RandomInRange * scale;
            float rotation = Rand.Range(-40f, 40f);
            DesertPitRoom main = DesertPitRoomBrush.Create(map, data.MainCenter, data.MainRadiusX, data.MainRadiusZ, rotation, null);
            //出生地保留完整连续干地，不能被洞壁纹理掏出夹角。
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(data.MainCenter, settings.mainDryRadius + 2f, true))
                if (cell.InBounds(map)) main.Floor.Add(cell);
            DesertPitRoomBrush.Apply(data, main, false);
            foreach (IntVec3 cell in main.Floor)
            {
                float distance = cell.DistanceTo(data.MainCenter);
                if (distance < Mathf.Min(data.MainRadiusX, data.MainRadiusZ) * 0.65f) data.CentralCoreCells.Add(cell);
                if (distance <= settings.mainDryRadius) data.ReservedSceneCells.Add(cell);
            }
            int count = settings.centralRoomCount.RandomInRange;
            float phase = Rand.Range(0f, 360f);
            for (int i = 0; i < count; i++)
            {
                float angle = (phase + i * 360f / count + Rand.Range(-18f, 18f)) * Mathf.Deg2Rad;
                float radius = settings.smallRoomRadius.RandomInRange * scale;
                float separation = Rand.Range(0.5f, 0.85f);
                Vector3 offset = new Vector3(Mathf.Cos(angle) * (data.MainRadiusX + radius * separation),
                    0f, Mathf.Sin(angle) * (data.MainRadiusZ + radius * separation));
                offset = Quaternion.AngleAxis(rotation, Vector3.up) * offset;
                IntVec3 center = (data.MainCenter.ToVector3Shifted() + offset).ToIntVec3();
                DesertPitRoom room = DesertPitRoomBrush.Create(map, center, radius, radius * Rand.Range(0.82f, 1f), Rand.Range(0f, 180f), null);
                DesertPitRoomBrush.Apply(data, room, true);
                DesertPitRoomBrush.Connect(map, data, data.MainCenter, center, 3.2f * scale, null);
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 4f, true))
                    if (cell.InBounds(map)) data.CentralCoreCells.Add(cell);
            }
        }

        //函数职责：检查必要洞群、两个单口蚁巢、河流端点和出生干地的实际掩码不变量。
        private static void Validate(Map map, DesertPitLayoutData data, DefModExtension_DesertPitLayout settings)
        {
            HashSet<IntVec3> reached = AntChamberConnector.CollectMainCave(map, data.MainCenter);
            foreach (DesertPitRoom room in data.Rooms)
                if (!reached.Contains(room.Center)) throw new InvalidOperationException("分层洞群存在未接入中央洞室的洞室。");
            if (data.AntChambers.Count != 2 || data.SecondaryRooms.Count != 2)
                throw new InvalidOperationException("分层巨坑缺少必要的蚁巢或次要洞室。");
            foreach (AntChamberLayout room in data.AntChambers)
            {
                AntChamberValidator.Validate(map, room);
                if (!reached.Contains(room.Nest)) throw new InvalidOperationException("中层蚁巢未与主洞群连接。");
            }
            int last = (data.RiverRunsNorthSouth ? map.Size.z : map.Size.x) - 1;
            if (data.RiverCenterline.Count == 0 || DesertPitRiverPlanner.Axis(data.RiverCenterline[0], data.RiverRunsNorthSouth) != 0
                || DesertPitRiverPlanner.Axis(data.RiverCenterline[data.RiverCenterline.Count - 1], data.RiverRunsNorthSouth) != last)
                throw new InvalidOperationException("浅河没有连接地图相对两边。");
            foreach (IntVec3 cell in data.RiverWaterCells)
                if (!reached.Contains(cell) || cell.DistanceTo(data.MainCenter) < settings.mainDryRadius)
                    throw new InvalidOperationException("浅河存在断流，或侵入了中央出生干地。");
        }
    }
}
