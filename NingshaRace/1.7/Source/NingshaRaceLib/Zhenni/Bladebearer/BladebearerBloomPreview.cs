using System;
using UnityEngine;

namespace NingshaRaceLib.Zhenni.Bladebearer;

//编辑器只管理预览会话，选择性辉光由 RimUnity 与地图共用的管线完成。
internal sealed class BladebearerBloomPreview : IDisposable
{
    private readonly RimUnity.Rendering.BloomPreview preview = new();

    //对本次 Pawn 相机目标启用凝砂发光层。
    internal void Begin(RenderTexture target) => preview.Begin(target, 1 << BladebearerRimUnityBloom.Layer);
    internal void CheckError() => preview.CheckError();
    internal void End() => preview.End();
    public void Dispose() => preview.Dispose();
}
