using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //根据真实刃段轨迹生成紫焰带和少量火屑，每个 Pawn 复用自己的网格。
    internal sealed class MeleeAnimationTrail : IDisposable
    {
        private Buffer world, previousWorld, preview;
        private readonly MeleeAnimationPlayback fading = new MeleeAnimationPlayback();

        //接下一招时保留上一招已经飞出的火屑，用两个缓冲交替复用。
        internal void PreserveTail(MeleeAnimationPlayback play)
        {
            if (!play.Active || world == null) return;
            fading.CopyVisualFrom(play);
            Buffer spare = previousWorld; previousWorld = world; world = spare;
        }

        //地图与相机预览各有一份网格，避免即时预览覆写已经提交的地图请求。
        internal void Draw(CompMeleeAnimation owner, PawnDrawParms parms, Matrix4x4 basis,
            Matrix4x4 blade, MeleeAnimationPlayback playback)
        {
            if (!playback.Active) return;
            if (!parms.DrawNow && previousWorld != null && fading.Move != null)
            {
                fading.Advance(Find.TickManager.TicksGame / 60f);
                if (fading.Active) previousWorld.Draw(owner, parms, previousWorld.Basis, previousWorld.Blade, fading);
            }
            Buffer buffer;
            if (parms.DrawNow) buffer = preview ?? (preview = new Buffer(owner));
            else buffer = world ?? (world = new Buffer(owner));
            buffer.Draw(owner, parms, basis, blade, playback);
        }

        //关闭编辑器只释放预览资源，地图上的动作仍正常完成。
        internal void DisposePreview() { preview?.Dispose(); preview = null; }

        //离图或销毁时释放这个 Pawn 拥有的全部网格。
        public void Dispose()
        { DisposePreview(); world?.Dispose(); previousWorld?.Dispose(); world = null; previousWorld = null; fading.Clear(); }

        //刀光与火屑共用网格，不额外加入 Pawn 渲染树。
        private sealed class Buffer : IDisposable
        {
            private readonly Mesh mesh = new Mesh { name = "近战火焰刀光" };
            private readonly List<Vector3> vertices = new List<Vector3>(400);
            private readonly List<Vector2> uv = new List<Vector2>(400);
            private readonly List<Color> colors = new List<Color>(400);
            private readonly List<int> triangles = new List<int>(600);
            private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
            private readonly MaterialPropertyBlock flashProperties = new MaterialPropertyBlock();
            private Vector3 anchor;
            internal Matrix4x4 Basis, Blade;

            //分配一次动态网格与属性块。
            internal Buffer(CompMeleeAnimation owner)
            {
                mesh.MarkDynamic();
            }

            //固定数量的时间采样保证不同帧率下轨迹一致，停顿时不会凭空延长刀光。
            internal void Draw(CompMeleeAnimation owner, PawnDrawParms parms, Matrix4x4 basis,
                Matrix4x4 blade, MeleeAnimationPlayback play)
            {
                vertices.Clear(); uv.Clear(); colors.Clear(); triangles.Clear();
                Basis = basis; Blade = blade;
                anchor = basis.GetColumn(3);
                MeleeAnimationMove move = play.Move;
                const int samples = 28;
                for (int i = 0; i < samples; i++)
                {
                    float a = play.VisualTime - move.trailLifetime + move.trailLifetime * i / samples;
                    float b = play.VisualTime - move.trailLifetime + move.trailLifetime * (i + 1) / samples;
                    if (b <= 0 || a > play.Duration) continue;
                    a = Mathf.Max(0, a);
                    Segment(owner.RenderDefinition, basis, blade, play, a, out Vector3 a0, out Vector3 a1, out float alphaA);
                    Segment(owner.RenderDefinition, basis, blade, play, b, out Vector3 b0, out Vector3 b1, out float alphaB);
                    if (alphaA <= 0 && alphaB <= 0) continue;
                    Color ca = Color.white * move.trailIntensity, cb = ca;
                    ca.a = alphaA; cb.a = alphaB;
                    Quad(a0, a1, b1, b0, new Vector2(2, a / play.Duration), new Vector2(3, a / play.Duration),
                        new Vector2(3, b / play.Duration), new Vector2(2, b / play.Duration), ca, cb);
                }
                Sparks(owner.RenderDefinition, basis, blade, play);
                Charge(owner, parms, basis, blade, play);
                if (vertices.Count == 0) return;
                mesh.Clear(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetColors(colors);
                mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
                Material material = MeleeAnimationResources.Apply(move, play.VisualTime * move.effectSpeed, parms.tint, properties, parms.DrawNow);
                Matrix4x4 matrix = Matrix4x4.Translate(anchor);
                GenDraw.DrawMeshNowOrLater(mesh, matrix, material, parms.DrawNow, properties);
            }

            //整张柔光星芒跟随最终刀刃位置，渐隐只降低透明度，沿用主体紫焰色相。
            private void Charge(CompMeleeAnimation owner, PawnDrawParms parms, Matrix4x4 basis,
                Matrix4x4 blade, MeleeAnimationPlayback play)
            {
                MeleeAnimationMove move = play.Move;
                if (move.chargeIntensity <= 0 || move.chargeFlash <= 0) return;
                float time = play.VisualTime, ready = move.chargeReady * play.Duration;
                float flashDuration = Mathf.Min(0.36f, (move.contact - move.chargeReady) * play.Duration);
                if (time <= ready || time >= ready + flashDuration) return;
                float progress = (time - ready) / flashDuration;
                float flash = Mathf.SmoothStep(0, 1, progress / 0.1f)
                    * (1 - Mathf.SmoothStep(0, 1, (progress - 0.36f) / 0.64f));
                MeleeAnimationDef def = owner.RenderDefinition;
                Matrix4x4 weapon = MeleeAnimationTransform.Weapon(basis, play, def, blade, play.MotionTime) * blade;
                Vector3 root = weapon.MultiplyPoint3x4(MeleeAnimationTransform.Uv(def.bladeStart));
                Vector3 tip = weapon.MultiplyPoint3x4(MeleeAnimationTransform.Uv(def.bladeEnd));
                Vector3 focus = Vector3.Lerp(root, tip, 0.58f);
                float size = (tip - root).magnitude * move.chargeScale * 2.6f;
                size *= Mathf.Lerp(0.86f, 1, flash);

                //素材烘焙了白色亮芯与默认紫焰，用相对色倍率保留亮芯并支持动作配色。
                Color color = new Color(move.color.r / 0.59f, move.color.g / 0.12f, move.color.b, move.color.a) * parms.tint;
                float brightness = 1 + move.chargeFlash * 0.6f;
                color.r *= brightness; color.g *= brightness; color.b *= brightness;
                color.a *= flash * Mathf.Clamp01(move.chargeIntensity * move.chargeFlash * 0.8f);
                Material material = MeleeAnimationResources.Flash(color, flashProperties, parms.DrawNow);
                //参考素材已经具有横竖尖芒，整张贴图一次绘制。
                Matrix4x4 matrix = Matrix4x4.TRS(focus, Quaternion.identity, new Vector3(size, 1, size));
                GenDraw.DrawMeshNowOrLater(MeshPool.plane10, matrix, material, parms.DrawNow, flashProperties);
            }

            //采样实际刀刃两个端点，颜色随轨迹年龄衰减，保留高速挥斩的亮芯。
            private static void Segment(MeleeAnimationDef def, Matrix4x4 basis, Matrix4x4 blade,
                MeleeAnimationPlayback play, float visualTime, out Vector3 start, out Vector3 end, out float alpha)
            {
                float motion = Mathf.Clamp(visualTime, 0, play.Duration);
                Matrix4x4 matrix = MeleeAnimationTransform.Weapon(basis, play, def, blade, motion) * blade;
                Vector3 tip = MeleeAnimationTransform.Uv(def.bladeEnd);
                Vector3 root = MeleeAnimationTransform.Uv(def.bladeStart);
                start = matrix.MultiplyPoint3x4(Vector3.LerpUnclamped(tip, root, play.Move.trailWidth));
                end = matrix.MultiplyPoint3x4(Vector3.LerpUnclamped(root, tip, 1.06f));
                float age = Mathf.Clamp01((play.VisualTime - visualTime) / play.Move.trailLifetime);
                alpha = play.Direction.Evaluate(motion / play.Duration).Trail * Mathf.Pow(1 - age, 1.6f);
            }

            //火屑从挥斩轨迹脱离，沿切线散开并通过连续噪声扭动，寿命结束时逐渐消失。
            private void Sparks(MeleeAnimationDef def, Matrix4x4 basis, Matrix4x4 blade, MeleeAnimationPlayback play)
            {
                MeleeAnimationMove move = play.Move;
                for (int i = 0; i < move.sparks; i++)
                {
                    float seed = i * 2.399963f + play.Sequence * 1.731f;
                    //连击把现有火屑数量分给各刺，三次分别爆发，蓄力期间不提前喷出。
                    int strike = i % move.strikeCount;
                    int count = (move.sparks - 1 - strike) / move.strikeCount + 1;
                    float contact = move.ContactAt(strike);
                    float before = move.strikeCount > 1 ? 0.025f : 0.12f, after = move.strikeCount > 1 ? 0.025f : 0.13f;
                    float birth = Mathf.Lerp(Mathf.Max(0, contact - before), contact + after,
                        (i / move.strikeCount + 0.5f) / count) * play.Duration;
                    float age = play.VisualTime - birth;
                    if (age < 0 || age > move.sparkLifetime) continue;
                    Segment(def, basis, blade, play, birth, out Vector3 root, out Vector3 tip, out _);
                    Segment(def, basis, blade, play, birth - 0.012f, out _, out Vector3 previous, out _);
                    Vector3 direction = tip - previous; direction.y = 0;
                    if (direction.sqrMagnitude < 0.000001f) direction = tip - root;
                    direction.Normalize();
                    Vector3 side = new Vector3(-direction.z, 0, direction.x);
                    Vector3 center = Vector3.Lerp(root, tip, 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(seed)))
                        + direction * (move.sparkSpeed * age * (0.65f + 0.35f * Mathf.Cos(seed)))
                        + side * ((Mathf.Sin(age * 13 * move.effectSpeed + seed) - Mathf.Sin(seed)) * move.sparkSpeed * age * 0.22f)
                        + Vector3.forward * (age * age * 0.12f * move.effectSpeed);
                    float life = age / move.sparkLifetime;
                    float size = move.sparkSize * (0.7f + 0.6f * Mathf.Abs(Mathf.Sin(seed * 3))) * (1 - life * 0.5f);
                    Vector3 tail = direction * size * 2.4f, width = side * size * 0.55f;
                    Color c = Color.white * (0.8f + 0.7f * Mathf.Abs(Mathf.Sin(seed)));
                    c.a = Mathf.Clamp01(age / 0.035f) * Mathf.Pow(1 - life, 1.3f);
                    Quad(center - tail - width, center - tail + width, center + tail + width, center + tail - width,
                        new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1), c, c);
                }
            }

            //写入独立四边形，刀光与火屑通过 UV 区间选择同一 Shader 中的轮廓。
            private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Color first, Color last)
            {
                int n = vertices.Count;
                vertices.Add(a - anchor); vertices.Add(b - anchor); vertices.Add(c - anchor); vertices.Add(d - anchor);
                uv.Add(ua); uv.Add(ub); uv.Add(uc); uv.Add(ud);
                colors.Add(first); colors.Add(first); colors.Add(last); colors.Add(last);
                triangles.Add(n); triangles.Add(n + 1); triangles.Add(n + 2);
                triangles.Add(n); triangles.Add(n + 2); triangles.Add(n + 3);
            }

            //Unity 网格只能在主线程释放。
            public void Dispose() { UnityEngine.Object.Destroy(mesh); }
        }
    }
}
