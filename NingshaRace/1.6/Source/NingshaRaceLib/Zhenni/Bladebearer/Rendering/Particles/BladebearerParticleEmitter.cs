using System;
using System.Collections.Generic;
using UnityEngine;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //逐颗保存出生位置、速度和寿命，用刀兵时钟计算粒子运动并复用场景深度。
    internal sealed class BladebearerParticleEmitter : IDisposable
    {
        private const int Capacity = 256;
        private readonly System.Random random;
        private readonly Particle[] particles = new Particle[Capacity];
        private readonly List<Vector3> vertices = new List<Vector3>(Capacity * 4);
        private readonly List<Vector2> uvs = new List<Vector2>(Capacity * 4);
        private readonly List<Color> colors = new List<Color>(Capacity * 4);
        private readonly List<int> triangles = new List<int>(Capacity * 6);
        internal readonly Mesh Mesh;
        internal readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();
        private float lastTime;
        private double nextEmissionTime;
        private int count;

        //每颗粒子独立运动和消失，不依赖隐藏场景对象的播放状态。
        private struct Particle
        {
            internal Vector3 Origin, Velocity, Direction;
            internal Vector2 NoiseSeed, NoiseOrigin;
            internal float Born, Lifetime, Size, Stretch, BirthFlow, SwayScale, Brightness;
        }

        //首次显示补齐一小段连续流；网格和各通道列表始终复用。
        internal BladebearerParticleEmitter(int seed, float time)
        {
            lastTime = time; nextEmissionTime = time - 0.25; random = new System.Random(seed);
            Mesh = new Mesh { name = "刀兵粒子网格", hideFlags = HideFlags.HideAndDontSave };
            Mesh.MarkDynamic();
        }

        //按出生时刻补齐仍存活的粒子，低帧率不会反复清空粒子，也不会把积压发射堆在根部。
        internal void Update(BladebearerPart part, BladebearerPart flame, float time)
        {
            var flow = new BladebearerParticleFlow(part, flame);
            Vector2 currentFlow = flow.Sample(time);
            float flowRate = (flow.Sample(time + 0.02f).x - flow.Sample(time - 0.02f).x) / 0.04f;
            if (time < lastTime) { count = 0; nextEmissionTime = time; }
            lastTime = time;
            for (int i = count - 1; i >= 0; i--)
                if (time - particles[i].Born >= particles[i].Lifetime) particles[i] = particles[--count];
            if (part.particleRate > 0)
            {
                nextEmissionTime = Math.Max(nextEmissionTime, time - part.particleLifetime);
                while (nextEmissionTime <= time)
                {
                    Vector2 birthFlow = flow.Sample((float)nextEmissionTime);
                    Emit(part, (float)nextEmissionTime, time, birthFlow);
                    nextEmissionTime += 1.0 / (part.particleRate * (1 + birthFlow.y * 0.2f));
                }
            }
            else nextEmissionTime = time;
            BuildMesh(part, time, currentFlow.x, flowRate, flame.speed);
        }

        //在指定根部周围生成独立火星，方向在美工设定的扇形内随机分布。
        private void Emit(BladebearerPart part, float born, float time, Vector2 flow)
        {
            if (count == Capacity) return;
            Vector2 axis = part.tip - part.root;
            float angle = Mathf.Atan2(axis.y, axis.x) + (part.flowAngle + (Next() - 0.5f) * part.particleSpread) * Mathf.Deg2Rad;
            float radius = Mathf.Sqrt(Next()) * part.particleRadius, around = Next() * Mathf.PI * 2;
            float speed = part.particleSpeed * Mathf.Lerp(0.8f, 1.15f, Next()) * (1 + flow.y * 0.2f);
            var particle = new Particle
            {
                Origin = new Vector3(part.root.x - 0.5f + Mathf.Cos(around) * radius, 0,
                    part.root.y - 0.5f + Mathf.Sin(around) * radius),
                Velocity = new Vector3(Mathf.Cos(angle) * speed, 0, Mathf.Sin(angle) * speed),
                Direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)),
                Born = born, Lifetime = part.particleLifetime * Mathf.Lerp(0.75f, 1, Next()),
                Size = part.particleSize * Mathf.Lerp(0.5f, 1.1f, Next() * Next()),
                Stretch = Mathf.Lerp(1.1f, 2f, Next()), BirthFlow = flow.x,
                SwayScale = Mathf.Lerp(0.7f, 1.3f, Next()),
                Brightness = Next() < 0.3f ? Mathf.Lerp(1.8f, 2.8f, Next()) : Mathf.Lerp(0.75f, 1.15f, Next())
            };
            particle.NoiseSeed = new Vector2(8 + Next() * 128, 8 + Next() * 128);
            particle.NoiseOrigin = PathNoise(particle.NoiseSeed, 0);
            if (time - born < particle.Lifetime) particles[count++] = particle;
        }

        //火星沿轨迹切线变成细小火屑，逐渐减速、收细和熄灭，保持部件的 Y 深度。
        private void BuildMesh(BladebearerPart part, float time, float flow, float flowRate, float flameSpeed)
        {
            vertices.Clear(); uvs.Clear(); colors.Clear(); triangles.Clear();
            for (int i = 0; i < count; i++)
            {
                Particle particle = particles[i];
                float age = time - particle.Born, life = Mathf.Clamp01(age / particle.Lifetime);
                Vector3 side = new Vector3(-particle.Direction.z, 0, particle.Direction.x);
                float wave = flow - particle.BirthFlow;
                float sway = part.particleSway * particle.SwayScale;
                float envelope = Mathf.Sin(life * Mathf.PI);
                //逐颗连续取样，离开火焰后仍沿噪声路径前进，不在寿命末端拉回原直线。
                float noiseSpeed = flameSpeed * 5 * particle.SwayScale;
                float noiseTime = age * noiseSpeed;
                Vector2 wander = PathNoise(particle.NoiseSeed, noiseTime) - particle.NoiseOrigin;
                Vector2 wanderRate = (PathNoise(particle.NoiseSeed, noiseTime + 0.025f)
                    - PathNoise(particle.NoiseSeed, noiseTime - 0.025f)) * (noiseSpeed / 0.05f);
                Vector3 position = particle.Origin + particle.Velocity * (age * (1 - 0.3f * life))
                    + (side * wander.x + particle.Direction * wander.y) * sway
                    + side * wave * sway * envelope * 0.35f;
                float swaySpeed = sway * 0.35f * (flowRate * envelope
                    + wave * Mathf.Cos(life * Mathf.PI) * Mathf.PI / particle.Lifetime);
                Vector3 tangent = particle.Velocity * (1 - 0.6f * life) + side * swaySpeed
                    + (side * wanderRate.x + particle.Direction * wanderRate.y) * sway;
                Vector3 forward = tangent.sqrMagnitude > 0.000001f ? tangent.normalized : particle.Direction;
                float shrink = Mathf.Lerp(1, 0.12f, Mathf.SmoothStep(0, 1, (life - 0.4f) / 0.6f));
                Vector3 along = forward * (particle.Size * particle.Stretch * shrink * 0.5f);
                Vector3 across = new Vector3(forward.z, 0, -forward.x) * (particle.Size * 0.38f * shrink * 0.5f);
                float alpha = Mathf.SmoothStep(0, 1, life / 0.06f) * (1 - Mathf.SmoothStep(0, 1, (life - 0.6f) / 0.4f));
                //少量高亮火星保留 HDR 亮芯，后半段才冷却，不让整团一起闪烁。
                float heat = particle.Brightness * part.lightIntensity * Mathf.Lerp(1, 0.55f, Mathf.SmoothStep(0, 1, (life - 0.5f) / 0.5f));
                Color color = new Color(heat, heat, heat, alpha);
                int start = vertices.Count;
                vertices.Add(position - across - along); uvs.Add(Vector2.zero); colors.Add(color);
                vertices.Add(position - across + along); uvs.Add(Vector2.up); colors.Add(color);
                vertices.Add(position + across + along); uvs.Add(Vector2.one); colors.Add(color);
                vertices.Add(position + across - along); uvs.Add(Vector2.right); colors.Add(color);
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
            }
            Mesh.Clear(); Mesh.SetVertices(vertices); Mesh.SetUVs(0, uvs); Mesh.SetColors(colors);
            Mesh.SetTriangles(triangles, 0, true);
        }

        //横向弯曲叠加轻微纵向回旋，使用固定噪声种子，暂停和低帧率下路径仍连续。
        private static Vector2 PathNoise(Vector2 seed, float time)
        {
            return new Vector2(
                (Mathf.PerlinNoise(seed.x + time * 0.31f, seed.y + time) - 0.5f) * 2,
                (Mathf.PerlinNoise(seed.x + 31.7f + time * 0.83f, seed.y + 17.3f + time * 0.23f) - 0.5f) * 0.6f);
        }

        //不使用游戏随机数，避免视觉效果改变游戏逻辑的随机序列。
        private float Next() => (float)random.NextDouble();

        //与所属 Pawn 或编辑会话一起释放网格。
        public void Dispose() { UnityEngine.Object.Destroy(Mesh); }
    }
}
