#if RW_1_7
using System;
using System.Collections.Generic;
using UnityEngine;
using Waylith.Rendering;
using Shader = UnityEngine.Shader;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //在主线程固定材质属性，渲染线程只读取引擎生成的不可变绘制项。
    internal static class BladebearerBloomSnapshot
    {
        private static readonly DrawPropertyArena arena = new DrawPropertyArena();

        //保留不发光图层的覆盖 alpha，使身体和头盔仍能遮挡后面的火焰。
        internal static MeshDrawFrame Capture(BladebearerBloomDraws sources, Camera camera, CameraView view, int frame)
        {
            var result = Empty(view, frame);
            try
            {
                sources.Sort(camera);
                arena.Clear();
                foreach (var draw in sources.Items)
                {
                    var material = draw.CaptureMaterial(false);
                    draw.Properties.CopyTo(arena.Allocate(draw.Properties.PropertyCount, out var range));
                    var command = new MeshDrawCommand(draw.Mesh.EngineMesh, material.engineMaterial,
                        range, Matrix4x4.ToNumerics(draw.Matrix), 0);
                    var item = MeshDrawSubmission.DrawItemMaker.TryMake(command, arena, Shader.GlobalKeywordKey,
                        result.Draws.Count, "Ningsha.Bloom");
                    if (item == null) throw new InvalidOperationException("刀兵辉光材质未能生成 Vulkan 绘制项。");
                    result.Draws.Add(item);
                }
                return result;
            }
            catch { result.Dispose(); throw; }
            finally { arena.Clear(); }
        }

        //预览沿用 Pawn 相机收集的原始网格及属性，不重新求值角色动画。
        internal static MeshDrawFrame CaptureScene(IReadOnlyList<MeshDrawCommand> commands, CameraView view, int frame)
        {
            var result = Empty(view, frame);
            try
            {
                for (int i = 0; i < commands.Count; i++)
                {
                    var item = MeshDrawSubmission.DrawItemMaker.TryMake(commands[i], MeshDrawSubmission.ActiveArena,
                        Shader.GlobalKeywordKey, i, "Ningsha.Preview");
                    if (item != null) result.Draws.Add(item);
                }
                return result;
            }
            catch { result.Dispose(); throw; }
        }

        //由 MeshDrawFrame 统一归还绘制项和列表。
        private static MeshDrawFrame Empty(CameraView view, int frame) => new MeshDrawFrame(frame, view,
            MeshDrawFrame.RentList(), MeshDrawFrame.RentList(), System.Numerics.Vector3.UnitY);
    }
}
#endif
