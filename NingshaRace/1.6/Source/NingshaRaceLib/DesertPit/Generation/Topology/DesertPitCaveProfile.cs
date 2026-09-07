using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace NingshaRaceLib.DesertPit.Generation.Topology
{
    //类职责：保存一次采样的不对称洞壁轮廓，将大尺度凸湾、岩壁凹口与细部岩性组合成天然洞体。
    internal sealed class DesertPitCaveProfile
    {
        public readonly float RadiusX;
        public readonly float RadiusZ;
        public readonly float Rotation;
        public readonly List<IntVec3> FloorOffsets = new List<IntVec3>();
        private readonly ModuleBase noise;
        private readonly Vector2[] lobes;
        private readonly float[] lobeSizes;
        private readonly float phase;
        private readonly float sine;
        private readonly float cosine;

        //函数职责：仅在创建洞室时采样轮廓，附近候选位置复用同一形状，避免每个候选重新随机。
        public DesertPitCaveProfile(float rx, float rz, float rotation)
        {
            RadiusX = rx;
            RadiusZ = rz;
            Rotation = rotation;
            sine = Mathf.Sin(rotation * Mathf.Deg2Rad);
            cosine = Mathf.Cos(rotation * Mathf.Deg2Rad);
            phase = Rand.Range(0f, Mathf.PI * 2f);
            noise = new Perlin(0.085, 2.0, 0.52, 3, Rand.Int, QualityMode.Medium);
            int count = Rand.RangeInclusive(2, 4);
            lobes = new Vector2[count];
            lobeSizes = new float[count];
            for (int i = 0; i < count; i++)
            {
                float angle = phase + i * 2f * Mathf.PI / count + Rand.Range(-0.5f, 0.5f);
                lobes[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Rand.Range(0.72f, 0.94f);
                lobeSizes[i] = Rand.Range(0.26f, 0.43f);
            }
            Rasterize();
        }

        //函数职责：一次缓存局部地面，候选选址只平移格子，不重复执行整片噪声与三角函数采样。
        private void Rasterize()
        {
            int extent = Mathf.CeilToInt(Mathf.Max(RadiusX, RadiusZ) * 1.45f + 4f);
            for (int x = -extent; x <= extent; x++)
                for (int z = -extent; z <= extent; z++)
                    if (Contains(x, z)) FloorOffsets.Add(new IntVec3(x, 0, z));
        }

        //函数职责：在洞室局部坐标求连续洞壁边界，保留中心净空，产生大小不一且不等距的岩湾。
        private bool Contains(int x, int z)
        {
            float warpX = 2.6f * (float)noise.GetValue(x, 11, z);
            float warpZ = 2.6f * (float)noise.GetValue(x + 173, 37, z - 91);
            float u = ((x + warpX) * cosine + (z + warpZ) * sine) / RadiusX;
            float v = (-(x + warpX) * sine + (z + warpZ) * cosine) / RadiusZ;
            float angle = Mathf.Atan2(v, u);
            float boundary = 0.93f + 0.12f * Mathf.Sin(angle * 3f + phase)
                + 0.065f * Mathf.Sin(angle * 5f - phase * 1.7f)
                + 0.045f * (float)noise.GetValue(x * 1.8f, 0, z * 1.8f);
            if (u * u + v * v <= boundary * boundary) return true;
            for (int i = 0; i < lobes.Length; i++)
            {
                float dx = u - lobes[i].x;
                float dz = (v - lobes[i].y) * 1.25f;
                if (dx * dx + dz * dz < lobeSizes[i] * lobeSizes[i]) return true;
            }
            return false;
        }
    }
}
