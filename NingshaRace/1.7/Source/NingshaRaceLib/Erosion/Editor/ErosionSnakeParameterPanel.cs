using System.Collections.Generic;
using System.Globalization;
using NingshaRaceLib.Erosion.Rendering;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Erosion.Editor
{
    //把常用摆放和动画分成两页，连接点收在高级设置中。
    internal sealed class ErosionSnakeParameterPanel
    {
        private readonly Window_ErosionSnakeEditor editor;
        private readonly Dictionary<string, string> buffers = new Dictionary<string, string>();
        private Vector2 scroll;
        private bool animationPage, pivotExpanded;
        private float width, y, contentHeight = 700;

        //控件只操作当前草稿。
        internal ErosionSnakeParameterPanel(Window_ErosionSnakeEditor editor) { this.editor = editor; }

        //切换蛇头和方向后丢弃旧输入，避免把上一项数值写回。
        internal void ResetInputs() { buffers.Clear(); scroll = Vector2.zero; GUI.FocusControl(null); }

        //长表单在自身区域滚动，不挤占导出按钮。
        internal void Draw(Rect area)
        {
            float row = Window_ErosionSnakeEditor.Row, half = (area.width - 6) / 2;
            if (Widgets.ButtonText(new Rect(area.x, area.y, half, row), (animationPage ? "" : "● ") + "位置与图层")) animationPage = false;
            if (Widgets.ButtonText(new Rect(area.x + half + 6, area.y, half, row), (animationPage ? "● " : "") + "摆动动画")) animationPage = true;
            Rect body = new Rect(area.x, area.y + row + 8, area.width, area.height - row - 8);
            width = body.width - 20; y = 0;
            Widgets.BeginScrollView(body, ref scroll, new Rect(0, 0, width, contentHeight));
            try
            {
                ErosionSnakeDraft snake = editor.Selected;
                int index = editor.Facing.AsInt;
                bool before = snake.Visible[index];
                Widgets.CheckboxLabeled(Next(), "在当前朝向显示", ref snake.Visible[index]);
                if (before != snake.Visible[index]) editor.Changed();
                if (!snake.Visible[index]) Label("此朝向已隐藏；勾选后查看调整效果。");
                if (animationPage) DrawAnimation(snake.Props.swayByFacing[index]);
                else DrawPose(snake, index);
                y += 8;
                if (Widgets.ButtonText(Next(), "恢复此蛇头当前朝向"))
                { snake.ResetFacing(index); ResetInputs(); editor.Changed(); }
                contentHeight = y + 8;
            }
            finally { Widgets.EndScrollView(); }
        }

        //位置和层级直接对应原版绘制参数。
        private void DrawPose(ErosionSnakeDraft snake, int index)
        {
            DrawData.RotationalData pose = snake.Poses[index];
            Vector3 offset = pose.offset.Value;
            float angle = pose.rotationOffset.Value, layer = pose.layer.Value;
            bool changed = Number("左右位置", ref offset.x, -2, 2, 0.005f);
            changed |= Number("上下位置", ref offset.z, -2, 2, 0.005f);
            changed |= Number("前后图层", ref layer,
                PawnRenderNodeProperties_ErosionSnake.MinimumLayer, PawnRenderNodeProperties_ErosionSnake.MaximumLayer, 0.1f);
            changed |= Number("静态角度（°）", ref angle, -180, 180, 0.5f);
            if (Widgets.ButtonText(Next(), (pivotExpanded ? "▼ " : "▶ ") + "高级：蛇尾连接点")) pivotExpanded = !pivotExpanded;
            Vector2 pivot = pose.pivot.Value;
            if (pivotExpanded)
            {
                Label("连接点是摆动轴心，使用贴图左下角为原点的 0～1 坐标。");
                changed |= Number("连接点 X", ref pivot.x, 0, 1, 0.001f);
                changed |= Number("连接点 Y", ref pivot.y, 0, 1, 0.001f);
            }
            if (!changed) return;
            pose.offset = offset; pose.rotationOffset = angle; pose.layer = layer; pose.pivot = pivot;
            snake.Poses[index] = pose; editor.Changed();
        }

        //每个方向单独控制节奏和错峰，摆幅为零时停止摆动。
        private void DrawAnimation(ErosionSnakeSway sway)
        {
            bool changed = Number("摆动幅度（°）", ref sway.angle, 0, 30, 0.1f);
            changed |= Number("摆动周期（秒）", ref sway.period, 0.1f, 30, 0.1f);
            changed |= Number("起始相位", ref sway.phase, -100, 100, 0.05f);
            Label("周期越短，摆动越快；相位用于错开各蛇头的动作。幅度设为 0 即可静止。");
            if (changed) editor.Changed();
        }

        //数值输入支持精调，滑条支持快速定位；无效输入保留原值并显示红色。
        private bool Number(string label, ref float value, float min, float max, float step)
        {
            Rect row = Next();
            float inputWidth = Mathf.Min(90, width * 0.3f);
            Widgets.Label(new Rect(row.x, row.y, width - inputWidth - 8, row.height), label);
            if (!buffers.TryGetValue(label, out string buffer)) buffer = value.ToString("0.###", CultureInfo.InvariantCulture);
            string text = Widgets.TextField(new Rect(width - inputWidth, row.y, inputWidth, row.height), buffer);
            bool changed = false;
            bool valid = float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                && !float.IsNaN(parsed) && !float.IsInfinity(parsed) && parsed >= min && parsed <= max;
            if (text != buffer && valid && parsed != value) { value = parsed; changed = true; }
            buffers[label] = text;
            Rect slider = new Rect(0, y, width, 20); y += 28;
            float next = Widgets.HorizontalSlider(slider, value, min, max, middleAlignment: true);
            if (next != value)
            {
                //仅在拖动时按步长取整，读取参数不会改变原本的小数精度。
                value = Mathf.Clamp(Mathf.Round(next / step) * step, min, max);
                buffers[label] = value.ToString("0.###", CultureInfo.InvariantCulture); changed = true;
            }
            if (!valid)
            {
                Color color = GUI.color; GUI.color = new Color(1, 0.4f, 0.35f);
                try { Label("请输入 " + min + "～" + max + " 之间的数值。"); }
                finally { GUI.color = color; }
            }
            return changed;
        }

        //按当前字体实测说明高度。
        private void Label(string text)
        {
            float height = Text.CalcHeight(text, width) + 8;
            Widgets.Label(new Rect(0, y, width, height), text); y += height + 5;
        }
        //每行高度包含中文行高和必要留白。
        private Rect Next()
        {
            var rect = new Rect(0, y, width, Window_ErosionSnakeEditor.Row);
            y += rect.height + 5; return rect;
        }
    }
}
