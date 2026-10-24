using System.Collections.Generic;
using NingshaRaceLib.Erosion.Rendering;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Erosion.Editor
{
    //把节点当前生效的数据展开为四向独立草稿。
    internal sealed class ErosionSnakeDraft
    {
        internal readonly PawnRenderNodeProperties_ErosionSnake Source;
        internal readonly PawnRenderNodeProperties_ErosionSnake Props;
        internal readonly DrawData.RotationalData[] Poses = new DrawData.RotationalData[4];
        internal readonly bool[] Visible = new bool[4];

        //保留原版东、西向偏移继承后的实际值，打开工具不会改变外观。
        internal ErosionSnakeDraft(PawnRenderNodeProperties_ErosionSnake source)
        {
            Source = source;
            Props = source.CopyForEditor();
            Props.swayByFacing = new List<ErosionSnakeSway>();
            for (int i = 0; i < 4; i++)
            {
                Rot4 facing = new Rot4(i);
                Poses[i] = new DrawData.RotationalData(facing, source.drawData.LayerForRot(facing, source.baseLayer))
                {
                    offset = source.drawData.OffsetForRot(facing), pivot = source.drawData.PivotForRot(facing),
                    rotationOffset = source.drawData.RotationOffsetForRot(facing), flip = source.drawData.FlipForRot(facing)
                };
                Visible[i] = source.visibleFacing == null || source.visibleFacing.Contains(facing);
                Props.swayByFacing.Add(source.SwayFor(facing)?.Copy() ?? new ErosionSnakeSway
                {
                    facing = facing, angle = source.swayAngle * (facing.IsHorizontal ? source.sideAngleFactor : 1),
                    period = source.swayPeriod, phase = source.swayPhase
                });
            }
            Apply();
        }

        //以原版 DrawData 保留层级和镜像行为，编辑器不另建渲染坐标系。
        internal void Apply()
        {
            Props.drawData = DrawData.NewWithData(Poses);
            Props.drawData.scale = Source.drawData.scale;
            Props.drawData.childScale = Source.drawData.childScale;
            Props.drawData.scaleOffsetByBodySize = Source.drawData.scaleOffsetByBodySize;
            Props.drawData.useBodyPartAnchor = Source.drawData.useBodyPartAnchor;
            Props.visibleFacing = new List<Rot4>();
            for (int i = 0; i < 4; i++) if (Visible[i]) Props.visibleFacing.Add(new Rot4(i));
        }

        //只恢复当前蛇头的一个朝向。
        internal void ResetFacing(int index)
        {
            var initial = new ErosionSnakeDraft(Source);
            Poses[index] = initial.Poses[index];
            Visible[index] = initial.Visible[index];
            Props.swayByFacing[index] = initial.Props.swayByFacing[index];
            Apply();
        }
    }
}
