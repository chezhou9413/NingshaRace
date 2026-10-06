using System.Collections.Generic;
using UnityEngine;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //保存一组刀兵实际绘制请求，地图与编辑器共用参数捕获和透明排序。
    internal sealed class BladebearerBloomDraws
    {
        //一条已提交的网格及其独立动画参数。
        internal sealed class Draw
        {
            internal Mesh Mesh;
            internal Matrix4x4 Matrix;
            internal BladebearerPart Part;
            internal Color Tint;
            internal float Time, Distance, Dissolve;
            internal int Order;
            internal readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();
            internal int CapturePass => 0;

            //动态粒子读取自己的顶点颜色，火焰仍从发生置换的原图提取亮部。
            internal Material CaptureMaterial(bool immediate) => Part.particle
                ? BladebearerResources.Material(Part, false, immediate)
                : immediate ? BladebearerResources.PreviewBloomSource : BladebearerResources.BloomSource;

            //即时预览必须直接写专用材质，地图命令则使用独立属性块。
            internal void Apply(Material immediate = null)
            {
                if (Part.particle)
                {
                    Color tint = Tint; tint.a *= 1 - Dissolve;
                    BladebearerShaderParameters.ApplyParticles(Part, tint, Properties, immediate);
                    Properties.SetFloat("_BloomCapture", 1);
                    if (immediate != null) immediate.SetFloat("_BloomCapture", 1);
                }
                else
                {
                    BladebearerShaderParameters.Apply(Part, Time, Tint, Properties, immediate);
                    BladebearerShaderParameters.ApplyPhase(Dissolve, Properties);
                    if (immediate != null) immediate.SetFloat("_PhaseDissolve", Dissolve);
                }
                float strength = Dissolve > 0 ? Mathf.Max(0.8f, Part.bloomIntensity) : Part.HasBloom ? Part.bloomIntensity : 0;
                Properties.SetFloat("_BloomIntensity", strength);
                Properties.SetFloat("_BloomThreshold", Part.bloomThreshold);
                if (immediate == null) return;
                immediate.SetFloat("_BloomIntensity", strength);
                immediate.SetFloat("_BloomThreshold", Part.bloomThreshold);
            }
        }

        private readonly List<Draw> pool = new List<Draw>();
        internal readonly List<Draw> Items = new List<Draw>();
        internal bool HasEmission { get; private set; }

        //复用属性块槽位；同一次捕获期间不会覆写之前提交的部件参数。
        internal void Add(PawnRenderNode_BladebearerPart node, Mesh mesh, Matrix4x4 matrix, Color tint)
            => Add(node.Part, mesh, matrix, tint, node.Owner.AnimationTime, node.Owner.CapturePreview ? 0 : node.Owner.Phase?.Arrival ?? 0);

        //普通部件与事件残影共用同一套排序和属性块缓存。
        internal void Add(BladebearerPart part, Mesh mesh, Matrix4x4 matrix, Color tint, float time, float dissolve)
        {
            int index = Items.Count;
            if (pool.Count == index) pool.Add(new Draw());
            Draw draw = pool[index];
            draw.Mesh = mesh; draw.Matrix = matrix; draw.Part = part;
            draw.Tint = tint; draw.Time = time; draw.Order = index; draw.Dissolve = dissolve;
            draw.Properties.Clear(); draw.Apply();
            Items.Add(draw);
            HasEmission |= (dissolve > 0 || part.HasBloom && part.bloomIntensity > 0) && part.color.a * tint.a > 0;
        }

        //透明来源从远到近绘制，深度相同时沿用原版提交顺序。
        internal void Sort(Camera camera)
        {
            foreach (Draw draw in Items)
                draw.Distance = Vector3.Dot((Vector3)draw.Matrix.GetColumn(3) - camera.transform.position, camera.transform.forward);
            Items.Sort(Compare);
        }

        //避免排序闭包和每帧分配委托。
        private static int Compare(Draw a, Draw b)
        {
            int order = b.Distance.CompareTo(a.Distance);
            return order != 0 ? order : a.Order.CompareTo(b.Order);
        }

        //下一帧仅复用槽位，离开地图或关闭窗口时释放对草稿和贴图的引用。
        internal void Clear(bool release = false)
        {
            Items.Clear(); HasEmission = false;
            if (release) pool.Clear();
        }
    }
}
