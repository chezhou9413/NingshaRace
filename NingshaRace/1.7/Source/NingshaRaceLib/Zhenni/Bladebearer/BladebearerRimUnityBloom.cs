using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer;

//发光层交给 RimUnity 的统一 Bloom，凝砂不持有后处理缓冲或模糊 Shader。
internal static class BladebearerRimUnityBloom
{
    internal const int Layer = 28;

    //深度与普通身体仍走原层，火焰、火星和加色部件进入专用层。
    internal static void Draw(Mesh mesh, Matrix4x4 matrix, Material material, bool bloom, bool immediate, MaterialPropertyBlock block)
    {
        if (immediate)
        {
            RimUnity.Assets.HostAssets.DrawMeshNow(mesh, matrix, material, bloom ? Layer : 0, block);
            return;
        }
        RimUnity.RimUnityMod.Bloom.SetLayerEnabled(Layer, true);
        Graphics.DrawMesh(mesh, matrix, material, bloom ? Layer : 0, null, 0, block);
    }
}

//地图加载后接管凝砂专用层，离开地图时关闭。
public sealed class GameComponent_NingshaBloom : GameComponent
{
    public GameComponent_NingshaBloom(Game game) { }
    public override void GameComponentUpdate()
    {
        RimUnity.RimUnityMod.Bloom.SetLayerEnabled(BladebearerRimUnityBloom.Layer, Find.CurrentMap != null);
    }
}
