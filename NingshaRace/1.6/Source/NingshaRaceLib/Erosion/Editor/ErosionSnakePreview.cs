using System;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Erosion.Editor
{
    //通过原版角色相机显示真实贴图、遮挡和蛇头动画。
    internal sealed class ErosionSnakePreview
    {
        private readonly Window_ErosionSnakeEditor editor;
        private RenderTexture texture;
        private float zoom = 1.3f, nextRender;
        private bool dirty = true, lightBackground;
        private string error;
        private readonly Vector2[] outline = new Vector2[4];
        private Vector2 pivotPoint;
        private bool hasOutline;

        //绑定窗口草稿和朝向。
        internal ErosionSnakePreview(Window_ErosionSnakeEditor editor) { this.editor = editor; }

        //在 GUI 控件分配之前渲染，避免相机回调中断滑条拖动。
        internal void BeforeWindowGUI()
        {
            if (Event.current.type != EventType.Repaint || texture == null || error != null) return;
            if ((!dirty && editor.Session.Paused) || Time.realtimeSinceStartup < nextRender) return;
            Camera camera = Find.PawnCacheCamera;
            RenderTexture previous = RenderTexture.active, target = camera.targetTexture;
            Color background = camera.backgroundColor;
            CameraClearFlags flags = camera.clearFlags;
            Vector3 position = camera.transform.position;
            float size = camera.orthographicSize;
            try
            {
                camera.backgroundColor = lightBackground ? new Color(0.6f, 0.62f, 0.65f) : new Color(0.08f, 0.09f, 0.12f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                editor.Session.RenderingPreview = true;
                hasOutline = false;
                //肖像模式固定角色姿态，蛇头使用独立时钟，头部地图粒子不会跟进预览相机。
                Find.PawnCacheRenderer.RenderPawn(editor.Session.Pawn, texture, new Vector3(0, 0, 0.35f), zoom, 0,
                    editor.Facing, renderHead: true, renderHeadgear: true, renderClothes: true, portrait: true);
                dirty = false; nextRender = Time.realtimeSinceStartup + 1f / 30;
            }
            catch (Exception exception)
            {
                error = "预览失败：" + exception.Message;
                Log.Error("[侵蚀体蛇头编辑器] " + exception);
            }
            finally
            {
                editor.Session.RenderingPreview = false;
                camera.backgroundColor = background; camera.clearFlags = flags;
                camera.transform.position = position; camera.orthographicSize = size;
                camera.targetTexture = target; RenderTexture.active = previous;
            }
        }

        //预览操作始终留在顶部，视口只处理缩放。
        internal void Draw(Rect area)
        {
            float row = Window_ErosionSnakeEditor.Row, width = (area.width - 12) / 3;
            if (Widgets.ButtonText(new Rect(area.x, area.y, width, row), editor.Session.Paused ? "播放动画" : "暂停动画"))
            { editor.Session.Paused = !editor.Session.Paused; Invalidate(); }
            if (Widgets.ButtonText(new Rect(area.x + width + 6, area.y, width, row), "从头播放"))
            { editor.Session.Clock = 0; editor.Session.Paused = false; Invalidate(); }
            if (Widgets.ButtonText(new Rect(area.x + 2 * (width + 6), area.y, width, row), lightBackground ? "深色背景" : "浅色背景"))
            { lightBackground = !lightBackground; Invalidate(); }
            string hint = "蓝框标记当前蛇头，绿点是蛇尾连接点。滚轮缩放；图层越大越靠前。";
            float hintHeight = Text.CalcHeight(hint, area.width) + 8;
            Rect viewport = new Rect(area.x, area.y + row + 8, area.width, Mathf.Max(8, area.height - row - hintHeight - 16));
            EnsureTexture(viewport);
            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(viewport, texture);
                if (hasOutline) DrawOutline(viewport);
            }
            if (error != null) Widgets.Label(viewport.ContractedBy(12), error);
            if (Event.current.type == EventType.ScrollWheel && viewport.Contains(Event.current.mousePosition))
            {
                zoom = Mathf.Clamp(zoom * Mathf.Exp(-Event.current.delta.y * 0.08f), 0.5f, 3);
                Invalidate(); Event.current.Use();
            }
            Widgets.Label(new Rect(area.x, viewport.yMax + 8, area.width, hintHeight), hint);
        }

        //在角色相机状态还有效时记录画布边界与实际连接点。
        internal void Record(PawnRenderNode node, PawnDrawParms parms, Matrix4x4 matrix)
        {
            if (node.Props != editor.Selected.Props) return;
            Camera camera = Find.PawnCacheCamera;
            outline[0] = camera.WorldToViewportPoint(matrix.MultiplyPoint3x4(new Vector3(-0.5f, 0, -0.5f)));
            outline[1] = camera.WorldToViewportPoint(matrix.MultiplyPoint3x4(new Vector3(0.5f, 0, -0.5f)));
            outline[2] = camera.WorldToViewportPoint(matrix.MultiplyPoint3x4(new Vector3(0.5f, 0, 0.5f)));
            outline[3] = camera.WorldToViewportPoint(matrix.MultiplyPoint3x4(new Vector3(-0.5f, 0, 0.5f)));
            Rot4 facing = parms.flipHead ? parms.facing.Opposite : parms.facing;
            Vector2 pivot = node.Props.drawData.PivotForRot(facing);
            Vector3 point = new Vector3(pivot.x - 0.5f, 0, pivot.y - 0.5f);
            parms.facing = facing;
            if ((facing == Rot4.West) != node.FlipGraphic(parms)) point.x = -point.x;
            pivotPoint = camera.WorldToViewportPoint(matrix.MultiplyPoint3x4(point));
            hasOutline = true;
        }

        //边框裁剪在预览内，不压住右侧参数控件。
        private void DrawOutline(Rect viewport)
        {
            GUI.BeginGroup(viewport);
            try
            {
                for (int i = 0; i < 4; i++)
                    Widgets.DrawLine(Screen(outline[i], viewport.size), Screen(outline[(i + 1) % 4], viewport.size), new Color(0.4f, 0.7f, 1, 0.65f), 1);
                Vector2 point = Screen(pivotPoint, viewport.size);
                Widgets.DrawBoxSolid(new Rect(point.x - 4, point.y - 4, 8, 8), new Color(0.3f, 1, 0.55f));
            }
            finally { GUI.EndGroup(); }
        }

        //将相机归一化坐标转为窗口内像素。
        private static Vector2 Screen(Vector2 point, Vector2 size) => new Vector2(point.x * size.x, (1 - point.y) * size.y);

        //只在视口尺寸变化时重新创建纹理。
        private void EnsureTexture(Rect viewport)
        {
            int width = Mathf.Clamp(Mathf.RoundToInt(viewport.width * Prefs.UIScale), 64, 1600);
            int height = Mathf.Clamp(Mathf.RoundToInt(viewport.height * Prefs.UIScale), 64, 1600);
            if (texture != null && texture.width == width && texture.height == height) return;
            Dispose();
            texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            { name = "侵蚀体蛇头编辑预览", filterMode = FilterMode.Bilinear, useMipMap = false };
            if (!texture.Create()) throw new InvalidOperationException("无法创建蛇头预览纹理。");
            Invalidate();
        }

        //数值改变后暂停画面也立即刷新。
        internal void Invalidate() { dirty = true; nextRender = 0; }

        //窗口关闭时释放显存。
        internal void Dispose()
        {
            if (texture == null) return;
            texture.Release(); UnityEngine.Object.Destroy(texture); texture = null;
        }
    }
}
