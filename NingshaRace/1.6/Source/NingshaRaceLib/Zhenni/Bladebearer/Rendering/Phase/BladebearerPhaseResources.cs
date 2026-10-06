using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //所有瞬移和死亡效果共用一张火屑网格，不逐颗分配实体或材质。
    [StaticConstructorOnStartup]
    internal static class BladebearerPhaseResources
    {
        internal static readonly Mesh Embers;
        internal static readonly Material EmberMaterial;

        //均匀采样原画画布，Shader 仅保留真实轮廓内的细碎火屑。
        static BladebearerPhaseResources()
        {
            EmberMaterial = BladebearerResources.Create("NingshaRace/Zhenni/Bladebearer/PhaseEmbers");
            EmberMaterial.renderQueue = BladebearerResources.PhaseColor.renderQueue;
            const int columns = 24, rows = 32, count = columns * rows;
            var vertices = new Vector3[count * 4]; var uv = new Vector2[count * 4];
            var corners = new Vector2[count * 4]; var indices = new int[count * 6];
            var random = new System.Random(714029);
            for (int y = 0; y < rows; y++) for (int x = 0; x < columns; x++)
            {
                int index = y * columns + x, v = index * 4, t = index * 6;
                var center = new Vector2((x + 0.15f + (float)random.NextDouble() * 0.7f) / columns,
                    (y + 0.15f + (float)random.NextDouble() * 0.7f) / rows);
                for (int c = 0; c < 4; c++)
                {
                    vertices[v + c] = new Vector3(center.x - 0.5f, 0, center.y - 0.5f);
                    uv[v + c] = center;
                }
                corners[v] = new Vector2(0, 0); corners[v + 1] = new Vector2(0, 1);
                corners[v + 2] = new Vector2(1, 1); corners[v + 3] = new Vector2(1, 0);
                indices[t] = v; indices[t + 1] = v + 1; indices[t + 2] = v + 2;
                indices[t + 3] = v; indices[t + 4] = v + 2; indices[t + 5] = v + 3;
            }
            Embers = new Mesh { name = "拥刀者轮廓火屑", hideFlags = HideFlags.HideAndDontSave,
                vertices = vertices, uv = uv, uv2 = corners, triangles = indices,
                bounds = new Bounds(Vector3.zero, new Vector3(4, 0.2f, 4)) };
        }
    }
}
