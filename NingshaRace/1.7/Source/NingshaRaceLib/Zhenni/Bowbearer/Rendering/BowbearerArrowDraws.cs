using NingshaRaceLib.Zhenni.Bladebearer;
using UnityEngine;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//地图和编辑器共用箭体的摆放、燃烧与辉光。
internal static class BowbearerArrowDraws
{
    //原稿朝左，先转到弹丸局部前方，再应用弹道朝向。
    internal static void Draw(BowbearerSettings settings, Vector3 position, Quaternion rotation, float time,
        bool immediate, MaterialPropertyBlock block)
    {
        Matrix4x4 basis = Basis(position, rotation);
        DrawPart(settings.arrowTail, basis, time, immediate, block);
        DrawPart(settings.arrowHead, basis, time, immediate, block);
    }

    //衔接点位于箭尾贴图内，随箭尾的位置、缩放和旋转一起变换。
    internal static Vector3 TrailOrigin(BowbearerSettings settings, Vector3 position, Quaternion rotation)
        => (Basis(position, rotation) * PartMatrix(settings.arrowTail)).MultiplyPoint3x4(
            new Vector3(settings.trailAnchor.x - .5f, 0, settings.trailAnchor.y - .5f));

    //重叠段沿箭尾本身的火焰形变延伸，四向和强化缩放共用箭体矩阵。
    internal static void TrailJoin(BowbearerSettings settings, Vector3 position, Quaternion rotation,
        float time, Vector3[] points)
    {
        var part = settings.arrowTail;
        var flame = new BowbearerFlameAnchor(part, time);
        Matrix4x4 matrix = Basis(position, rotation) * PartMatrix(part);
        Vector2 towardRoot = (part.root - settings.trailAnchor).normalized;
        float scale = Vector2.Scale(towardRoot, part.size).magnitude;
        Vector2 extent = scale > .0001f ? towardRoot * (settings.joinOverlap / scale) : Vector2.zero;
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 uv = flame.Position(settings.trailAnchor + extent * (i / (float)(points.Length - 1)));
            points[i] = matrix.MultiplyPoint3x4(new Vector3(uv.x - .5f, 0, uv.y - .5f));
        }
    }

    //箭体和衔接点共用原稿朝向校正。
    private static Matrix4x4 Basis(Vector3 position, Quaternion rotation)
        => Matrix4x4.TRS(position, rotation * Quaternion.AngleAxis(90, Vector3.up), Vector3.one);

    //图层的局部变换也用于定位箭尾衔接点。
    private static Matrix4x4 PartMatrix(BladebearerPart p)
        => Matrix4x4.TRS(new Vector3(p.position.x, p.layer * .0001f, p.position.y),
            Quaternion.AngleAxis(p.angle, Vector3.up), new Vector3(p.size.x, 1, p.size.y));

    //每次完整设置属性，防止不同箭矢和预览相互串色。
    internal static void DrawPart(BladebearerPart p, Matrix4x4 basis, float time, bool immediate, MaterialPropertyBlock block)
    {
        if (!p.enabled) return;
        Material material = BladebearerResources.Material(p, false, immediate);
        BladebearerShaderParameters.Apply(p, time, Color.white, block, immediate ? material : null);
        Matrix4x4 matrix = basis * PartMatrix(p);
        BowbearerWeaponMaterials.Draw(BladebearerResources.Mesh, matrix, material, immediate, block);
        block.Clear();
    }
}

