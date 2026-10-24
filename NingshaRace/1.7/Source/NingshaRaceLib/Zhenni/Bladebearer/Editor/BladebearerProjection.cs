using UnityEngine;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //保存实际 Pawn 相机下的部件投影，手柄不依赖猜测的相机比例。
    internal struct BladebearerProjection
    {
        private Vector2 origin, right, up;

        //在 PawnCacheCamera 的临时相机状态恢复前采集三个基准点。
        internal static BladebearerProjection Capture(Matrix4x4 matrix, Camera camera)
        {
            Vector2 a = camera.WorldToViewportPoint(matrix.MultiplyPoint3x4(new Vector3(-0.5f, 0, -0.5f)));
            Vector2 b = camera.WorldToViewportPoint(matrix.MultiplyPoint3x4(new Vector3(0.5f, 0, -0.5f)));
            Vector2 c = camera.WorldToViewportPoint(matrix.MultiplyPoint3x4(new Vector3(-0.5f, 0, 0.5f)));
            return new BladebearerProjection { origin = a, right = b - a, up = c - a };
        }

        //把贴图 UV 投到编辑窗口像素。
        internal Vector2 Screen(Rect viewport, Vector2 uv)
        {
            Vector2 p = origin + right * uv.x + up * uv.y;
            return new Vector2(viewport.x + p.x * viewport.width, viewport.yMax - p.y * viewport.height);
        }

        //把鼠标反投影到 UV，供部件移动和根部、尾端拖动共用。
        internal Vector2 Uv(Rect viewport, Vector2 mouse)
        {
            Vector2 d = new Vector2((mouse.x - viewport.x) / viewport.width, (viewport.yMax - mouse.y) / viewport.height) - origin;
            float determinant = right.x * up.y - right.y * up.x;
            if (Mathf.Abs(determinant) < 0.000001f) return new Vector2(0.5f, 0.5f);
            return new Vector2((d.x * up.y - d.y * up.x) / determinant, (right.x * d.y - right.y * d.x) / determinant);
        }
    }
}
