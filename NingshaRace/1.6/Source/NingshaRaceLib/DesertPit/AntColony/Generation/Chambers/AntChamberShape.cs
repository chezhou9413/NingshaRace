using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace NingshaRaceLib.DesertPit.AntColony.Generation.Chambers
{
    //类职责：在局部坐标中一次生成不对称洞体、遮挡弯道和随轮廓生长的厚岩层，供选址复用。
    internal sealed class AntChamberShape
    {
        public readonly HashSet<IntVec3> OpenCells = new HashSet<IntVec3>();
        public readonly HashSet<IntVec3> PassageCells = new HashSet<IntVec3>();
        public readonly HashSet<IntVec3> Footprint = new HashSet<IntVec3>();
        public readonly List<CellRect> OccupiedSpans = new List<CellRect>();
        public readonly IntVec3 Nest = new IntVec3(-3, 0, -2);
        public readonly IntVec3 Mound = new IntVec3(2, 0, 4);
        public readonly IntVec3 Mouth;
        public CellRect Bounds;

        //函数职责：只为实际需要的洞室采样形状参数，选址候选不重复抽样或构造地面集合。
        public AntChamberShape()
        {
            Mouth = new IntVec3(Rand.RangeInclusive(30, 33), 0, Rand.RangeInclusive(7, 9));
            ModuleBase noise = new Perlin(0.17, 2.0, 0.5, 2, Rand.Int, QualityMode.Medium);
            BuildRoom(noise);
            BuildPassage();
            OpenCells.UnionWith(PassageCells);
            OpenCells.RemoveWhere(cell => cell.x > Mouth.x);
            KeepConnectedFloor();
            PassageCells.IntersectWith(OpenCells);
            BuildRockShell(noise);
        }

        //函数职责：融合受缓变噪声扰动的旋转椭圆与两处生活空间，保留巢穴储藏环和菌床面积。
        private void BuildRoom(ModuleBase noise)
        {
            float radiusX = Rand.Range(10.2f, 11.8f);
            float radiusZ = Rand.Range(9.3f, 11.3f);
            float angle = Rand.Range(-0.3f, 0.3f);
            float phase = Rand.Range(0f, Mathf.PI * 2f);
            float cosine = Mathf.Cos(angle);
            float sine = Mathf.Sin(angle);
            for (int x = -17; x <= 14; x++)
                for (int z = -16; z <= 16; z++)
                {
                    float dx = (x + 1) * cosine - z * sine;
                    float dz = (x + 1) * sine + z * cosine;
                    float direction = Mathf.Atan2(dz / radiusZ, dx / radiusX);
                    float boundary = 1f + 0.11f * Mathf.Sin(direction * 3f + phase)
                        + 0.045f * Mathf.Sin(direction * 5f - phase) + 0.055f * (float)noise.GetValue(x, 0, z);
                    if (dx * dx / (radiusX * radiusX) + dz * dz / (radiusZ * radiusZ) <= boundary * boundary)
                        OpenCells.Add(new IntVec3(x, 0, z));
                }
            //四格巢穴及外沿两格储藏区完全落在圆形核心中；菌床也有独立的开放空间。
            AntCaveBrush.AddDisc(OpenCells, new Vector2(Nest.x, Nest.z), 6.1f);
            AntCaveBrush.AddDisc(OpenCells, new Vector2(Mound.x, Mound.z), 5.5f);
        }

        //函数职责：让入口先绕过洞体下侧再回转，使用连续切线形成遮挡巢穴的岩脊。
        private void BuildPassage()
        {
            float bendX = Rand.Range(18.5f, 20f);
            float lowZ = Rand.Range(-11.5f, -10f);
            float phase = Rand.Range(0f, Mathf.PI * 2f);
            Vector2 bend = new Vector2(bendX, -3f);
            AntCaveBrush.AddCurve(PassageCells, new Vector2(5f, -5f), new Vector2(11f, lowZ),
                new Vector2(bendX, lowZ), bend, phase);
            AntCaveBrush.AddCurve(PassageCells, bend, new Vector2(bendX, 6f),
                new Vector2(Mouth.x - 8f, Mouth.z), new Vector2(Mouth.x, Mouth.z), phase + 1.3f);
        }

        //函数职责：去掉轮廓离散化可能产生的孤立边缘格，仅保留与巢穴实际四邻接连通的地面。
        private void KeepConnectedFloor()
        {
            HashSet<IntVec3> connected = new HashSet<IntVec3> { Nest };
            Queue<IntVec3> queue = new Queue<IntVec3>();
            queue.Enqueue(Nest);
            while (queue.Count > 0)
            {
                IntVec3 current = queue.Dequeue();
                foreach (IntVec3 offset in GenAdj.CardinalDirections)
                {
                    IntVec3 next = current + offset;
                    if (OpenCells.Contains(next) && connected.Add(next)) queue.Enqueue(next);
                }
            }
            OpenCells.IntersectWith(connected);
        }

        //函数职责：沿真实地面轮廓包裹三至四格厚岩，只在洞口截面留下出口，不回填整个包围矩形。
        private void BuildRockShell(ModuleBase noise)
        {
            foreach (IntVec3 cell in OpenCells)
            {
                float radius = 3.6f + 0.35f * (float)noise.GetValue(cell.x * 0.7f, 19, cell.z * 0.7f);
                foreach (IntVec3 wall in GenRadial.RadialCellsAround(cell, radius, true))
                    if (wall.x <= Mouth.x) Footprint.Add(wall);
            }
            int minX = int.MaxValue, minZ = int.MaxValue, maxX = int.MinValue, maxZ = int.MinValue;
            foreach (IntVec3 cell in Footprint)
            {
                minX = Mathf.Min(minX, cell.x);
                minZ = Mathf.Min(minZ, cell.z);
                maxX = Mathf.Max(maxX, cell.x);
                maxZ = Mathf.Max(maxZ, cell.z);
            }
            Bounds = CellRect.FromLimits(new IntVec3(minX, 0, minZ), new IntVec3(maxX, 0, maxZ));
            FillEnclosedRock();
            BuildOccupiedSpans();
        }

        //函数职责：把厚岩轮廓包围的闭合缝隙归入岩层，避免保留无法绕路接回主洞的微型孤腔。
        private void FillEnclosedRock()
        {
            HashSet<IntVec3> exterior = new HashSet<IntVec3>();
            Queue<IntVec3> queue = new Queue<IntVec3>();
            foreach (IntVec3 cell in Bounds.EdgeCells)
                if (!Footprint.Contains(cell) && exterior.Add(cell)) queue.Enqueue(cell);
            while (queue.Count > 0)
            {
                IntVec3 cell = queue.Dequeue();
                foreach (IntVec3 offset in GenAdj.CardinalDirections)
                {
                    IntVec3 next = cell + offset;
                    if (Bounds.Contains(next) && !Footprint.Contains(next) && exterior.Add(next)) queue.Enqueue(next);
                }
            }
            foreach (IntVec3 cell in Bounds)
                if (!exterior.Contains(cell)) Footprint.Add(cell);
        }

        //函数职责：把真实占地压成互不重叠的逐行区间，选址时可用前缀和检查凹凸岩层而非空白包围框。
        private void BuildOccupiedSpans()
        {
            for (int z = Bounds.minZ; z <= Bounds.maxZ; z++)
            {
                int start = int.MinValue;
                for (int x = Bounds.minX; x <= Bounds.maxX + 1; x++)
                {
                    bool occupied = x <= Bounds.maxX && Footprint.Contains(new IntVec3(x, 0, z));
                    if (occupied && start == int.MinValue) start = x;
                    if (!occupied && start != int.MinValue)
                    {
                        OccupiedSpans.Add(CellRect.FromLimits(new IntVec3(start, 0, z), new IntVec3(x - 1, 0, z)));
                        start = int.MinValue;
                    }
                }
            }
        }
    }
}
