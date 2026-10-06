using System;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //使用游戏自身的 Pawn 相机预览，并把鼠标操作映射到真实部件坐标。
    internal sealed class BladebearerPreview
    {
        private readonly Window_BladebearerEditor editor;
        private readonly BladebearerBloomPreview bloom = new BladebearerBloomPreview();
        private RenderTexture texture;
        private float zoom = 0.7f, nextRender;
        private Vector3 cameraOffset = new Vector3(0, 0, 0.35f);
        private bool dirty = true, lightBackground;
        internal Rot4 Facing = Rot4.South;
        private string renderError;
        private int dragMode, hotControl;
        private BladebearerProjection dragProjection;
        private Vector2 dragStart, originalPosition;
        private Rect dragViewport;
        private float Row => Mathf.Max(32, Text.LineHeight + 8);

        //持有当前编辑会话。
        internal BladebearerPreview(Window_BladebearerEditor editor) { this.editor = editor; }

        //在窗口分配 IMGUI 控件前渲染，防止 Pawn 相机打断滑条拖动。
        internal void BeforeWindowGUI()
        {
            if (Event.current.type != EventType.Repaint || texture == null || renderError != null) return;
            if ((!dirty && editor.Owner.Paused) || Time.realtimeSinceStartup < nextRender) return;
            Camera camera = Find.PawnCacheCamera;
            RenderTexture previousTarget = RenderTexture.active;
            RenderTexture cameraTarget = camera.targetTexture;
            Color background = camera.backgroundColor;
            CameraClearFlags flags = camera.clearFlags;
            Vector3 cameraPosition = camera.transform.position;
            float size = camera.orthographicSize;
            try
            {
                //背景写入 Alpha 1，避免 IMGUI 二次透明混合丢失原图光束的加色效果。
                camera.backgroundColor = lightBackground ? new Color(0.62f, 0.63f, 0.65f, 1) : new Color(0.08f, 0.09f, 0.12f, 1);
                camera.clearFlags = CameraClearFlags.SolidColor;
                editor.Owner.CapturePreview = true;
                editor.Owner.Projections.Clear();
                bloom.Begin(texture);
                Find.PawnCacheRenderer.RenderPawn(editor.Owner.Pawn, texture, cameraOffset, zoom, 0, Facing,
                    renderHead: true, renderHeadgear: true, renderClothes: true, portrait: false);
                bloom.CheckError();
                dirty = false; nextRender = Time.realtimeSinceStartup + 1f / 30f;
            }
            catch (Exception e)
            {
                renderError = "预览失败：" + e.Message;
                Log.Error("[震尼拥刀者编辑器] " + e);
                editor.Status = renderError;
            }
            finally
            {
                bloom.End();
                editor.Owner.CapturePreview = false;
                camera.backgroundColor = background; camera.clearFlags = flags;
                camera.transform.position = cameraPosition; camera.orthographicSize = size;
                camera.targetTexture = cameraTarget; RenderTexture.active = previousTarget;
            }
        }

        //顶部工具栏与底部操作提示之外才是可拖动视口。
        internal void Draw(Rect area)
        {
            float buttonWidth = (area.width - 12) / 3;
            if (Widgets.ButtonText(new Rect(area.x, area.y, buttonWidth, Row), editor.Owner.Paused ? "播放" : "暂停"))
                editor.Owner.Paused = !editor.Owner.Paused;
            if (Widgets.ButtonText(new Rect(area.x + buttonWidth + 6, area.y, buttonWidth, Row), "复位视角"))
            { zoom = 0.7f; cameraOffset = new Vector3(0, 0, 0.35f); Invalidate(); }
            if (Widgets.ButtonText(new Rect(area.x + 2 * (buttonWidth + 6), area.y, buttonWidth, Row), lightBackground ? "深色背景" : "浅色背景"))
            { lightBackground = !lightBackground; Invalidate(); }
            float directionWidth = (area.width - 18) / 4;
            Rot4[] directions = { Rot4.South, Rot4.East, Rot4.West, Rot4.North };
            for (int i = 0; i < directions.Length; i++)
            {
                Rot4 direction = directions[i];
                string label = BladebearerParameterPanel.FacingLabel(direction.ToStringWord());
                if (Widgets.ButtonText(new Rect(area.x + i * (directionWidth + 6), area.y + Row + 6, directionWidth, Row),
                    (Facing == direction ? "● " : "") + label))
                editor.SetFacing(direction);
            }
            string hint = "左键拖部件；绿点是根部或粒子发射点，橙点是方向基准，青色箭头调整飘散角度。滚轮缩放，右键平移。";
            float hintHeight = Text.CalcHeight(hint, area.width) + 8;
            float toolbarHeight = Row * 2 + 6;
            bool showHint = area.height > toolbarHeight + hintHeight + 72;
            Rect viewport = new Rect(area.x, area.y + toolbarHeight + 8, area.width, Mathf.Max(8, area.height - toolbarHeight - 8 - (showHint ? hintHeight + 8 : 0)));
            EnsureTexture(viewport);
            if (Event.current.type == EventType.Repaint) GUI.DrawTexture(viewport, texture);
            if (renderError != null) Widgets.Label(viewport.ContractedBy(12), renderError);
            HandleViewport(viewport);
            if (showHint) Widgets.Label(new Rect(area.x, viewport.yMax + 8, area.width, hintHeight), hint);
            else TooltipHandler.TipRegion(viewport, hint);
        }

        //只在尺寸变化时重新分配纹理，释放旧的 GPU 资源。
        private void EnsureTexture(Rect rect)
        {
            int width = Mathf.Clamp(Mathf.RoundToInt(rect.width * Prefs.UIScale), 64, 1600);
            int height = Mathf.Clamp(Mathf.RoundToInt(rect.height * Prefs.UIScale), 64, 1600);
            if (texture != null && texture.width == width && texture.height == height) return;
            Dispose();
            texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            { name = "震尼拥刀者编辑器预览", filterMode = FilterMode.Bilinear, useMipMap = false };
            if (!texture.Create()) throw new InvalidOperationException("无法创建震尼拥刀者预览纹理。");
            Invalidate();
        }

        //背景内的操作由独立热控件接管，拖出视口后仍能正确结束拖动。
        private void HandleViewport(Rect viewport)
        {
            Event e = Event.current;
            int id = GUIUtility.GetControlID(0xDA0B, FocusType.Passive, viewport);
            BladebearerPart p = editor.Selected;
            bool projected = p != null && editor.Owner.Projections.TryGetValue(p.id, out _);
            if (projected)
            {
                BladebearerProjection projection = editor.Owner.Projections[p.id];
                if (e.type == EventType.Repaint) DrawHandles(viewport, projection, p);
                if (e.type == EventType.MouseDown && e.button == 0 && viewport.Contains(e.mousePosition))
                {
                    Vector2 root = projection.Screen(viewport, p.root), tip = projection.Screen(viewport, p.tip);
                    Vector2 flow = projection.Screen(viewport, p.FlowHandleUv());
                    Vector2 uv = projection.Uv(viewport, e.mousePosition);
                    bool directional = p.flame || p.particle;
                    dragMode = directional && Vector2.Distance(e.mousePosition, root) < 12 ? 2
                        : directional && Vector2.Distance(e.mousePosition, flow) < 12 ? 5
                        : directional && Vector2.Distance(e.mousePosition, tip) < 12 ? 3
                        : uv.x >= 0 && uv.x <= 1 && uv.y >= 0 && uv.y <= 1 ? 1 : 0;
                    if (dragMode != 0)
                    {
                        GUI.FocusControl(null);
                        dragProjection = projection; dragViewport = viewport; dragStart = e.mousePosition;
                        originalPosition = p.position; GUIUtility.hotControl = hotControl = id; e.Use();
                    }
                }
            }
            if (e.type == EventType.ScrollWheel && viewport.Contains(e.mousePosition))
            { zoom = Mathf.Clamp(zoom * Mathf.Exp(-e.delta.y * 0.08f), 0.2f, 4); Invalidate(); e.Use(); }
            if (e.type == EventType.MouseDown && e.button == 1 && viewport.Contains(e.mousePosition))
            { dragMode = 4; GUIUtility.hotControl = hotControl = id; e.Use(); }
            if (e.type == EventType.MouseDrag && GUIUtility.hotControl == hotControl && hotControl != 0)
            {
                if (dragMode == 4)
                {
                    cameraOffset.x -= e.delta.x * 2f / (zoom * viewport.height);
                    cameraOffset.z += e.delta.y * 2f / (zoom * viewport.height);
                    Invalidate();
                }
                else if (p != null)
                {
                    Vector2 uv = dragProjection.Uv(dragViewport, e.mousePosition);
                    if (dragMode == 1)
                    {
                        Vector2 delta = uv - dragProjection.Uv(dragViewport, dragStart);
                        //投影反算已回到部件局部坐标，不再额外乘握持父节点的角度。
                        Vector3 shift = Quaternion.AngleAxis(p.angle, Vector3.up) * new Vector3(delta.x * p.size.x, 0, delta.y * p.size.y);
                        Vector2 position = originalPosition + new Vector2(shift.x, shift.z);
                        p.position = new Vector2(Mathf.Clamp(position.x, -5, 5), Mathf.Clamp(position.y, -5, 5));
                    }
                    else if (dragMode == 2) p.root = new Vector2(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y));
                    else if (dragMode == 3) p.tip = new Vector2(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y));
                    else if (dragMode == 5)
                    {
                        Vector2 axis = p.tip - p.root, target = uv - p.root;
                        if (target.sqrMagnitude > 0.000001f && axis.sqrMagnitude > 0.000001f)
                            p.flowAngle = Mathf.DeltaAngle(Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg,
                                Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg);
                    }
                    editor.Changed();
                }
                e.Use();
            }
            if (e.type == EventType.MouseUp && GUIUtility.hotControl == hotControl && hotControl != 0)
            { GUIUtility.hotControl = 0; hotControl = 0; dragMode = 0; e.Use(); }
        }

        //手柄图形裁剪在视口内，不覆盖右侧滑条。
        private static void DrawHandles(Rect viewport, BladebearerProjection projection, BladebearerPart part)
        {
            GUI.BeginGroup(viewport);
            try
            {
                Rect local = new Rect(0, 0, viewport.width, viewport.height);
                Vector2 a = projection.Screen(local, Vector2.zero), b = projection.Screen(local, Vector2.right);
                Vector2 c = projection.Screen(local, Vector2.one), d = projection.Screen(local, Vector2.up);
                Color outline = new Color(0.48f, 0.7f, 0.9f, 0.65f);
                Widgets.DrawLine(a, b, outline, 1); Widgets.DrawLine(b, c, outline, 1);
                Widgets.DrawLine(c, d, outline, 1); Widgets.DrawLine(d, a, outline, 1);
                if (!part.flame && !part.particle) return;
                Vector2 root = projection.Screen(local, part.root), tip = projection.Screen(local, part.tip);
                Widgets.DrawLine(root, tip, new Color(1, 0.7f, 0.25f), 2);
                Widgets.DrawBoxSolid(new Rect(root.x - 5, root.y - 5, 10, 10), Color.green);
                Widgets.DrawBoxSolid(new Rect(tip.x - 5, tip.y - 5, 10, 10), new Color(1, 0.5f, 0));
                Vector2 flow = projection.Screen(local, part.FlowHandleUv());
                Color flowColor = new Color(0.15f, 0.95f, 1f);
                if (part.particle)
                {
                    Vector2 axis = part.FlowHandleUv() - part.root;
                    float radians = part.particleSpread * 0.5f * Mathf.Deg2Rad;
                    Vector2 sideUv = new Vector2(-axis.y, axis.x) * Mathf.Sin(radians);
                    Vector2 centerUv = part.root + axis * Mathf.Cos(radians);
                    Color cone = new Color(flowColor.r, flowColor.g, flowColor.b, 0.45f);
                    Widgets.DrawLine(root, projection.Screen(local, centerUv + sideUv), cone, 1);
                    Widgets.DrawLine(root, projection.Screen(local, centerUv - sideUv), cone, 1);
                }
                Vector2 direction = (flow - root).normalized;
                Vector2 side = new Vector2(-direction.y, direction.x);
                Widgets.DrawLine(root, flow, flowColor, 2);
                Widgets.DrawLine(flow, flow - direction * 10 + side * 5, flowColor, 2);
                Widgets.DrawLine(flow, flow - direction * 10 - side * 5, flowColor, 2);
                Widgets.DrawBoxSolid(new Rect(flow.x - 4, flow.y - 4, 8, 8), flowColor);
            }
            finally { GUI.EndGroup(); }
        }

        //参数变更或相机操作后允许下一次重绘立即更新。
        internal void Invalidate() { dirty = true; nextRender = 0; renderError = null; }
        //窗口关闭或尺寸改变时释放纹理和拖动状态。
        internal void Dispose()
        {
            bloom.Dispose();
            if (texture != null) { texture.Release(); UnityEngine.Object.Destroy(texture); texture = null; }
            if (hotControl != 0 && GUIUtility.hotControl == hotControl) GUIUtility.hotControl = 0;
            hotControl = 0; dragMode = 0;
        }
    }
}
