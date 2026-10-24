using System;
using System.Collections.Generic;
using NingshaRaceLib.Zhenni.Bladebearer;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//箭体脱落的灰烬留在出生位置，命中后由地图继续绘制爆焰和余烬。
internal sealed class BowbearerArrowEffects : IDisposable
{
    private const int Capacity = 512;
    private readonly BowbearerSettings settings;
    private readonly int seed;
    private readonly List<Particle> particles = new(Capacity);
    private readonly List<Vector3> vertices = new(Capacity * 4);
    private readonly List<Vector2> uvs = new(Capacity * 4);
    private readonly List<Color> colors = new(Capacity * 4);
    private readonly List<int> indices = new(Capacity * 6);
    private readonly MaterialPropertyBlock properties = new();
    private readonly BladebearerPart particleStyle = new() { particle = true, color = Color.white, additive = .1f };
    private Mesh mesh;
    private Vector3 previous, impactPosition;
    private float previousTime, impactTime = -1, keepUntil;
    private double nextEmission;
    private bool initialized;
    private readonly record struct Particle(Vector3 Origin, Vector3 Velocity, float Born, float Life, float Size, float Phase);

    //每支箭和每个预览各自持有网格；视觉随机数不影响战斗随机序列。
    internal BowbearerArrowEffects(BowbearerSettings settings, int seed)
    {
        this.settings = settings; this.seed = seed;
    }

    //拖动时间轴时重建同一组确定性粒子，网格继续复用。
    internal void Reset() { particles.Clear(); initialized = false; impactTime = -1; keepUntil = 0; }

    //按出生时间插值飞行位置，快速弹丸也不会把灰烬挤在单个更新点。
    internal void Record(Vector3 position, float time)
    {
        if (!initialized)
        {
            initialized = true; previous = position; previousTime = time; nextEmission = time;
            return;
        }
        if (settings.arrowAshRate > 0 && time > previousTime && impactTime < 0)
        {
            Vector3 direction = (position - previous).Yto0().normalized;
            Vector3 side = new(direction.z, 0, -direction.x);
            nextEmission = Math.Max(nextEmission, time - settings.arrowAshLifetime);
            while (nextEmission <= time)
            {
                float born = (float)nextEmission;
                int key = Mathf.RoundToInt(born * 12000);
                Vector3 origin = Vector3.Lerp(previous, position, (born - previousTime) / (time - previousTime));
                origin -= direction * (.08f + Noise(key, 1) * .55f);
                Vector3 velocity = (-direction * .25f + side * (Noise(key, 2) * 2 - 1) + Vector3.forward * .2f)
                    * settings.arrowAshSpeed;
                Emit(origin, velocity, born, key);
                nextEmission += 1.0 / settings.arrowAshRate;
            }
        }
        previous = position; previousTime = time;
    }

    //命中或护盾拦截只触发一次视觉爆开，不创建爆炸伤害。
    internal void Impact(Vector3 position, float time)
    {
        if (impactTime >= 0) return;
        impactPosition = position; impactTime = time;
        keepUntil = Mathf.Max(keepUntil, time + settings.impactDuration);
        for (int i = 0; i < settings.impactAshCount; i++)
        {
            float angle = (i + Noise(i, 4)) / Mathf.Max(1, settings.impactAshCount) * Mathf.PI * 2;
            Vector3 direction = new(Mathf.Sin(angle), 0, Mathf.Cos(angle));
            float speed = settings.impactRadius / settings.arrowAshLifetime * (1.5f + Noise(i, 5));
            Emit(position + direction * settings.impactRadius * .06f, direction * speed, time, i + 100000);
        }
    }

    //尾迹消失后仍保留尚未熄灭的灰烬和爆焰。
    internal bool Expired(float time) => time > keepUntil;

    //灰烬先飞散再冷却，细长火屑逐渐变成暗淡碎片。
    internal void Draw(float time, bool immediate, Map map)
    {
        vertices.Clear(); uvs.Clear(); colors.Clear(); indices.Clear();
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            Particle p = particles[i];
            float age = time - p.Born;
            if (age >= p.Life) { particles.RemoveAt(i); continue; }
            if (age < 0) continue;
            float life = age / p.Life;
            Vector3 position = p.Origin + p.Velocity * (age * (1 - .35f * life));
            position += new Vector3(Mathf.Sin(p.Phase + age * 7) - Mathf.Sin(p.Phase), 0, age * .4f) * .06f;
            if (!Visible(position, map)) continue;
            Vector3 direction = p.Velocity.sqrMagnitude > .00001f ? p.Velocity.normalized : Vector3.forward;
            Vector3 along = direction * (p.Size * (1 - life * .65f));
            Vector3 across = new Vector3(direction.z, 0, -direction.x) * (p.Size * .3f);
            Color color = Color.Lerp(settings.ashColor, new Color(.22f, .19f, .25f, settings.ashColor.a), life);
            float heat = Mathf.Lerp(1.7f, .6f, life);
            color.r *= heat; color.g *= heat; color.b *= heat;
            color.a *= Mathf.SmoothStep(0, 1, age / .025f) * (1 - Mathf.SmoothStep(.5f, 1, life));
            int n = vertices.Count;
            vertices.Add(position - across - along); vertices.Add(position - across + along);
            vertices.Add(position + across + along); vertices.Add(position + across - along);
            uvs.Add(Vector2.zero); uvs.Add(Vector2.up); uvs.Add(Vector2.one); uvs.Add(Vector2.right);
            for (int j = 0; j < 4; j++) colors.Add(color);
            indices.Add(n); indices.Add(n + 1); indices.Add(n + 2);
            indices.Add(n); indices.Add(n + 2); indices.Add(n + 3);
        }
        if (vertices.Count > 0)
        {
            if (mesh == null) { mesh = new Mesh { name = "挽弓者箭矢灰烬" }; mesh.MarkDynamic(); }
            mesh.Clear(); mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetColors(colors); mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            Material material = BladebearerResources.Material(particleStyle, false, immediate);
            BladebearerShaderParameters.ApplyParticles(particleStyle, Color.white, properties, immediate ? material : null);
            BowbearerWeaponMaterials.Draw(mesh, Matrix4x4.identity, material, immediate, properties);
            properties.Clear();
        }
        DrawImpact(time, immediate, map);
    }

    //柔软亮芯与翻卷焰团在材质内连续膨胀，命中半径直接对应世界尺寸。
    private void DrawImpact(float time, bool immediate, Map map)
    {
        float age = time - impactTime;
        if (impactTime < 0 || age < 0 || age >= settings.impactDuration || !Visible(impactPosition, map)) return;
        float life = age / settings.impactDuration;
        float diameter = settings.impactRadius * 2;
        Material material = BowbearerWeaponMaterials.Impact(settings, time, life, Noise(seed, 6) * 17, properties, immediate);
        Matrix4x4 matrix = Matrix4x4.TRS(impactPosition, Quaternion.identity, new Vector3(diameter, 1, diameter));
        BowbearerWeaponMaterials.Draw(BladebearerResources.Mesh, matrix, material, immediate, properties);
        properties.Clear();
    }

    //限制每支箭的活跃碎片数量，满额时替换最早的灰烬。
    private void Emit(Vector3 origin, Vector3 velocity, float born, int key)
    {
        if (particles.Count == Capacity) particles.RemoveAt(0);
        float life = settings.arrowAshLifetime * Mathf.Lerp(.65f, 1, Noise(key, 7));
        particles.Add(new(origin, velocity, born, life, settings.arrowAshSize * Mathf.Lerp(.7f, 1.3f, Noise(key, 8)), Noise(key, 9) * 6.28f));
        keepUntil = Mathf.Max(keepUntil, born + life);
    }

    //固定哈希让暂停、单步和时间轴拖动保持同一条粒子轨迹。
    private float Noise(int key, int channel)
    {
        uint value = unchecked((uint)(key * 374761393 + channel * 668265263 + seed));
        value = unchecked((value ^ (value >> 13)) * 1274126177);
        return (value & 0x00ffffff) / 16777215f;
    }

    //地图特效遵循迷雾，预览无需查询地图。
    private static bool Visible(Vector3 position, Map map)
    {
        if (map == null) return true;
        IntVec3 cell = position.ToIntVec3();
        return cell.InBounds(map) && !cell.Fogged(map);
    }

    //地图移除或编辑窗口关闭时释放自有网格。
    public void Dispose() { if (mesh != null) UnityEngine.Object.Destroy(mesh); mesh = null; particles.Clear(); }
}
