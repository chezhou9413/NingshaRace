using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //逐层记录实际绘制矩阵，残影包含当前朝向、握刀姿态和深度。
    internal sealed class BladebearerPhaseSnapshot
    {
        private readonly Dictionary<string, Layer> layers = new Dictionary<string, Layer>();
        private int frame = -1;
        private float recordedAt = -100;
        internal Vector3 Origin;
        internal bool Fresh => layers.Count > 0 && Time.realtimeSinceStartup - recordedAt <= 0.5f;

        //快照只持有静态部件资源，不引用即将销毁的 Pawn、动态火星网格或原版属性块。
        internal sealed class Layer
        {
            internal BladebearerPart Part;
            internal Matrix4x4 Matrix;
            internal Color Tint;
            internal float Time, Dissolve;
            internal int Frame;
            internal readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();
        }

        //每个部件复用记录槽，仅在主线程绘制完成时更新。
        internal void Record(PawnRenderNode_BladebearerPart node, Matrix4x4 matrix, Color tint)
        {
            if (node.Part.particle) return;
            frame = Time.frameCount; recordedAt = Time.realtimeSinceStartup; Origin = node.Owner.Pawn.DrawPos;
            if (!layers.TryGetValue(node.Part.id, out Layer layer)) layers.Add(node.Part.id, layer = new Layer());
            layer.Part = node.Part; layer.Matrix = matrix; layer.Tint = tint;
            layer.Time = node.Owner.AnimationTime; layer.Frame = frame; layer.Dissolve = node.Owner.Phase.Arrival;
        }

        //效果创建时复制参数与矩阵；之后编辑外观或销毁 Pawn 都不会改写残影。
        internal List<Layer> Capture(Vector3 origin)
        {
            var result = new List<Layer>();
            if (!Fresh) return result;
            Matrix4x4 shift = Matrix4x4.Translate(origin - Origin);
            foreach (Layer layer in layers.Values)
                if (layer.Frame == frame) result.Add(new Layer
                {
                    Part = layer.Part.Copy(), Matrix = shift * layer.Matrix, Tint = layer.Tint,
                    Time = layer.Time, Dissolve = layer.Dissolve
                });
            return result;
        }

        //瞬移后等待新位置的真实绘制结果，避免旧朝向混入聚合。
        internal void Clear() { layers.Clear(); frame = -1; recordedAt = -100; }
    }
}
