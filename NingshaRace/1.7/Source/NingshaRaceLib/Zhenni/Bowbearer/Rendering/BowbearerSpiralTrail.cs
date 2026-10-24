using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//有限距离采样生成螺旋，每条尾迹持有自己的动态网格。
internal sealed class BowbearerSpiralTrail : IDisposable
{
    private const int Capacity = 128;
    private const int JoinSegments = 8;
    private readonly List<Sample> samples = new(Capacity);
    private readonly List<Vector3> vertices = new(Capacity * 8);
    private readonly List<Vector2> uvs = new(Capacity * 8);
    private readonly List<Color> colors = new(Capacity * 8);
    private readonly List<int> indices = new(Capacity * 12);
    private readonly MaterialPropertyBlock block = new();
    private readonly BowbearerArrowEffects effects;
    private readonly RibbonPoint[] ribbon = new RibbonPoint[Capacity + JoinSegments];
    private readonly Vector3[] join = new Vector3[JoinSegments + 1];
    private Mesh mesh;
    private Vector3 lastPosition, arrowPosition, joinOffset;
    private Quaternion arrowRotation;
    private float lastTime, distance;
    private bool initialized;
    private bool stopped;
    internal readonly BowbearerSettings Settings;
    private readonly record struct Sample(Vector3 Position, float Time, float Distance);
    private readonly record struct RibbonPoint(Vector3 Position, float Width, Color Color, bool Visible);

    //草稿与实际箭矢各自持有配置，不共享可变历史。
    internal BowbearerSpiralTrail(BowbearerSettings settings, int seed = 0)
    { Settings = settings; effects = new BowbearerArrowEffects(settings, seed); }

    //时间轴回溯时重建采样，继续复用预览网格。
    internal void Reset() { samples.Clear(); effects.Reset(); initialized = false; stopped = false; distance = 0; }

    //发射体离开地图后只保留余下的消散时间。
    internal void Stop() { stopped = true; }

    //命中后尾迹不再延长，爆焰和灰烬继续由本对象管理。
    internal void Impact(Vector3 position, Quaternion rotation, float time)
    { Record(position, rotation, time); effects.Impact(position, time); stopped = true; }

    //按经过距离插值，出生时间也插值，跳步不会生成一整段同龄尾迹。
    internal void Record(Vector3 position, Quaternion rotation, float time)
    {
        arrowPosition = position; arrowRotation = rotation;
        effects.Record(position, time);
        position = BowbearerArrowDraws.TrailOrigin(Settings, position, rotation);
        if (!initialized)
        {
            initialized = true; lastPosition = position; lastTime = time;
            samples.Add(new Sample(position, time, 0)); return;
        }
        float length = Vector3.Distance(lastPosition, position);
        if (length < .0001f) return;
        float spacing = Mathf.Max(.025f, Settings.length / (Capacity - 2));
        int steps = Mathf.Clamp(Mathf.CeilToInt(length / spacing), 1, Capacity);
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            if (samples.Count == Capacity) samples.RemoveAt(0);
            samples.Add(new Sample(Vector3.Lerp(lastPosition, position, t), Mathf.Lerp(lastTime, time, t), distance + length * t));
        }
        distance += length; lastPosition = position; lastTime = time;
    }

    //命中后仍可用原有历史淡出，不再依赖弹丸实体。
    internal bool Expired(float time) => stopped && initialized && time - lastTime > Settings.lifetime && effects.Expired(time);

    //普通双股与强化三股共用连续衔接，保留场景深度测试。
    internal void Draw(float time, bool immediate, Map map = null)
    {
        effects.Draw(time, immediate, map);
        while (samples.Count > 1 && (time - samples[0].Time > Settings.lifetime || distance - samples[0].Distance > Settings.length)) samples.RemoveAt(0);
        if (samples.Count < 2) return;
        BowbearerArrowDraws.TrailJoin(Settings, arrowPosition, arrowRotation, stopped ? lastTime : time, join);
        joinOffset = join[0] - lastPosition;
        vertices.Clear(); uvs.Clear(); colors.Clear(); indices.Clear();
        int count = samples.Count + JoinSegments;
        int strands = Settings.Empowered ? 3 : 2;
        for (int strand = 0; strand < strands; strand++)
        {
            //先求同一条螺旋的连续中心，再用相邻切线生成共享边，接缝不重叠也不张口。
            for (int i = 0; i < count; i++)
            {
                Sample sample = SampleAt(i);
                Vector3 axis = (SampleAt(Math.Min(i + 1, count - 1)).Position - SampleAt(Math.Max(0, i - 1)).Position).Yto0().normalized;
                Vector3 side = new(axis.z, 0, -axis.x);
                Point(sample, side, time, strand, out Vector3 position, out float width, out Color color);
                IntVec3 cell = position.ToIntVec3();
                ribbon[i] = new(position, width, color, map == null || cell.InBounds(map) && !cell.Fogged(map));
            }
            int start = vertices.Count;
            for (int i = 0; i < count; i++)
            {
                RibbonPoint point = ribbon[i];
                Vector3 tangent = (ribbon[Math.Min(i + 1, count - 1)].Position - ribbon[Math.Max(0, i - 1)].Position).Yto0().normalized;
                Vector3 across = new Vector3(tangent.z, 0, -tangent.x) * point.Width;
                vertices.Add(point.Position - across); vertices.Add(point.Position + across);
                uvs.Add(new Vector2(0, SampleAt(i).Distance)); uvs.Add(new Vector2(1, SampleAt(i).Distance));
                colors.Add(point.Color); colors.Add(point.Color);
                if (i == 0 || !point.Visible || !ribbon[i - 1].Visible) continue;
                int n = start + (i - 1) * 2;
                indices.Add(n); indices.Add(n + 1); indices.Add(n + 3);
                indices.Add(n); indices.Add(n + 3); indices.Add(n + 2);
            }
        }
        if (indices.Count == 0) return;
        if (mesh == null) { mesh = new Mesh { name = "挽弓者螺旋" }; mesh.MarkDynamic(); }
        mesh.Clear(); mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetColors(colors); mesh.SetTriangles(indices, 0);
        mesh.RecalculateBounds();
        Material material = BowbearerWeaponMaterials.Trail(Settings, time, block, immediate);
        BowbearerWeaponMaterials.Draw(mesh, Matrix4x4.identity, material, immediate, block);
        block.Clear();
    }

    //历史弹道保持稳定，靠近箭尾才平滑接向当前火焰；重叠段跟随实际箭尾。
    private Sample SampleAt(int index)
    {
        if (index < samples.Count)
        {
            Sample sample = samples[index];
            float weight = 1 - Mathf.SmoothStep(0, 1, (distance - sample.Distance) / Settings.joinBlend);
            return sample with { Position = sample.Position + joinOffset * weight };
        }
        int step = index - samples.Count + 1;
        float overlap = Settings.joinOverlap * step / JoinSegments;
        return new Sample(join[step], lastTime, distance + overlap);
    }

    //绕轴相位控制左右偏移和前后深度，尾端按寿命收细。
    private void Point(Sample sample, Vector3 side, float time, int strand, out Vector3 position, out float width, out Color color)
    {
        float age = Mathf.Clamp01((time - sample.Time) / Settings.lifetime);
        float behind = distance - sample.Distance;
        //容量上限也会截去旧采样，末端必须按实际留下的长度渐隐。
        float visibleLength = Mathf.Min(Settings.length, distance - samples[0].Distance);
        float fade = Mathf.Pow(1 - age, Settings.taper)
            * Mathf.SmoothStep(0, 1, (visibleLength - behind) / Settings.joinBlend);
        float phase = (sample.Distance / Settings.pitch - time * Settings.rotationsPerSecond) * Mathf.PI * 2
            + strand * Mathf.PI * 2 / (Settings.Empowered ? 3 : 2);
        float front = Mathf.Cos(phase);
        float opening = Mathf.SmoothStep(0, 1, behind / Settings.joinBlend);
        position = sample.Position + side * (Mathf.Sin(phase) * Settings.radius * fade * opening)
            + Vector3.up * (front * .008f * opening);
        //宽网格只承载外围柔光，中心亮芯由材质收窄，避免网格边成为纸条边。
        float cap = Mathf.SmoothStep(0, 1, (behind + Settings.joinOverlap) / (Settings.joinOverlap * .7f));
        width = Settings.width * 2.2f * fade * (.85f + .15f * front) * Mathf.Sqrt(cap);
        float brightness = .78f + .22f * front;
        //合拢后按股数分摊亮度，避免两股或三股叠成遮住箭尾的白色硬接头。
        brightness *= Mathf.Lerp(1f / (Settings.Empowered ? 3 : 2), 1, opening);
        color = new Color(brightness, brightness, brightness, fade * cap);
    }

    //离开地图或关闭预览时释放 GPU 资源。
    public void Dispose() { if (mesh != null) UnityEngine.Object.Destroy(mesh); mesh = null; samples.Clear(); effects.Dispose(); }
}
