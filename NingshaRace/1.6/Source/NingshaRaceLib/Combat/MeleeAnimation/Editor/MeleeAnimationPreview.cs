using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //使用原版 Pawn 相机预览任意接入单位，不创建额外 Pawn 或执行战斗。
    internal sealed class MeleeAnimationPreview : IDisposable
    {
        private readonly Window_MeleeAnimationEditor editor;
        private readonly IMeleeAnimationPreviewAddon[] addons;
        private RenderTexture texture;
        private float zoom = 0.7f, nextRender;
        private Vector3 offset = new Vector3(0, 0, 0.35f);
        private bool dirty = true, light;
        private string error;
        private float Row => Mathf.Max(32, Text.LineHeight + 10);
        internal Rot4 Facing = Rot4.South;
        //外观后处理通过接口参与预览，不依赖任何系列的怪物类。
        internal MeleeAnimationPreview(Window_MeleeAnimationEditor editor)
        { this.editor = editor; addons = editor.Owner.Pawn.AllComps.OfType<IMeleeAnimationPreviewAddon>().ToArray(); }
        //参数或视角变化要求下一次重绘。
        internal void Invalidate() { dirty = true; }

        //相机参数和渲染目标在异常时也完整恢复。
        internal void Render()
        {
            if (Event.current.type != EventType.Repaint || texture == null || error != null || editor.Owner.Draft == null) return;
            if ((!dirty && editor.Owner.Paused) || Time.realtimeSinceStartup < nextRender) return;
            Camera camera = Find.PawnCacheCamera;
            RenderTexture active = RenderTexture.active, target = camera.targetTexture;
            Color background = camera.backgroundColor; CameraClearFlags flags = camera.clearFlags;
            Vector3 position = camera.transform.position; float size = camera.orthographicSize;
            try
            {
                camera.backgroundColor = light ? new Color(0.6f, 0.61f, 0.63f, 1) : new Color(0.08f, 0.09f, 0.12f, 1);
                camera.clearFlags = CameraClearFlags.SolidColor; editor.Owner.CapturePreview = true;
                foreach (var addon in addons) addon.BeginMeleePreview(texture, editor.Owner.Clock);
                Find.PawnCacheRenderer.RenderPawn(editor.Owner.Pawn, texture, offset, zoom, 0, Facing,
                    renderHead: true, renderHeadgear: true, renderClothes: true, portrait: false);
                foreach (var addon in addons) addon.CheckMeleePreview();
                dirty = false; nextRender = Time.realtimeSinceStartup + 1f / 30;
            }
            catch (Exception e) { error = "近战预览失败：" + e.Message; editor.Status = error; Log.Error("[近战动画编辑器] " + e); }
            finally
            {
                foreach (var addon in addons) addon.EndMeleePreview();
                editor.Owner.CapturePreview = false; camera.backgroundColor = background; camera.clearFlags = flags;
                camera.transform.position = position; camera.orthographicSize = size;
                camera.targetTexture = target; RenderTexture.active = active;
            }
        }

        //四向按钮与视角工具固定在预览上方。
        internal void Draw(Rect area)
        {
            float width = (area.width - 12) / 3;
            if (Widgets.ButtonText(new Rect(area.x, area.y, width, Row), editor.Owner.Paused ? "继续" : "暂停")) editor.Owner.Paused = !editor.Owner.Paused;
            if (Widgets.ButtonText(new Rect(area.x + width + 6, area.y, width, Row), "复位视角")) { zoom = 0.7f; offset = new Vector3(0, 0, 0.35f); Invalidate(); }
            if (Widgets.ButtonText(new Rect(area.x + 2 * (width + 6), area.y, width, Row), "切换背景")) { light = !light; Invalidate(); }
            width = (area.width - 18) / 4;
            string[] directions = MeleeAnimationValidation.Facings;
            for (int i = 0; i < directions.Length; i++)
            {
                Rot4 facing = Rot4.FromString(directions[i]);
                if (Widgets.ButtonText(new Rect(area.x + i * (width + 6), area.y + Row + 6, width, Row),
                    (Facing == facing ? "● " : "") + Window_MeleeAnimationEditor.FacingLabel(directions[i]))) { Facing = facing; Invalidate(); }
            }
            Rect view = new Rect(area.x, area.y + Row * 2 + 14, area.width, Mathf.Max(8, area.height - Row * 2 - 14));
            int w = Mathf.Clamp(Mathf.RoundToInt(view.width * Prefs.UIScale), 64, 1600), h = Mathf.Clamp(Mathf.RoundToInt(view.height * Prefs.UIScale), 64, 1600);
            if (texture == null || texture.width != w || texture.height != h)
            {
                if (texture != null) { texture.Release(); UnityEngine.Object.Destroy(texture); }
                texture = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { name = "近战动画预览", filterMode = FilterMode.Bilinear };
                if (!texture.Create()) throw new InvalidOperationException("近战预览纹理创建失败。");
                Invalidate();
            }
            if (Event.current.type == EventType.Repaint) GUI.DrawTexture(view, texture);
            if (error != null) Widgets.Label(view.ContractedBy(8), error);
            Event e = Event.current;
            if (!view.Contains(e.mousePosition)) return;
            if (e.type == EventType.ScrollWheel) { zoom = Mathf.Clamp(zoom * Mathf.Pow(1.08f, e.delta.y), 0.25f, 2); Invalidate(); e.Use(); }
            if (e.type == EventType.MouseDrag && e.button == 1) { offset += new Vector3(-e.delta.x, 0, e.delta.y) * zoom / Mathf.Max(100, view.height); Invalidate(); e.Use(); }
        }

        //离开编辑器时释放预览纹理和外观附加处理器。
        public void Dispose()
        {
            if (texture != null) { texture.Release(); UnityEngine.Object.Destroy(texture); texture = null; }
            foreach (var addon in addons) addon.CloseMeleePreview();
        }
    }
}
