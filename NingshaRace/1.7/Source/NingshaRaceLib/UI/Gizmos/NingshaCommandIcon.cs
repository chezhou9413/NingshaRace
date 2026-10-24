using UnityEngine;
using Verse;

namespace NingshaRaceLib.UI.Gizmos
{
    //类职责：按命令图标比例与材质绘制大图标，并去除已知蜕皮图片的透明外边。
    [StaticConstructorOnStartup]
    internal static class NingshaCommandIcon
    {
        //字段职责：在启动主线程预加载蜕皮图标，绘制阶段只用缓存识别图案。
        private static readonly Texture2D MoltingIcon = ContentFinder<Texture2D>.Get("UI/Commands/Molting");
        private static readonly Rect FullTexture = new Rect(0f, 0f, 1f, 1f);
        private static readonly Rect MoltingContent = new Rect(9f / 300f, 20f / 300f, 280f / 300f, 234f / 300f);

        //职责：保持长宽比、旋转、缩放和禁用材质，只调整绘制区域而不修改原始图片。
        public static void Draw(Command command, Rect rect, Material buttonMaterial)
        {
            if (Event.current.type != EventType.Repaint || command.icon == null || rect.width <= 0f || rect.height <= 0f) return;
            Rect coordinates = command.iconTexCoords;
            Vector2 proportions = command.iconProportions;
            if (command.icon == MoltingIcon && coordinates == FullTexture)
            {
                //当前三百像素蜕皮图片的有效内容外保留四像素透明安全边，不裁去图案。
                coordinates = MoltingContent;
                proportions = new Vector2(proportions.x * coordinates.width, proportions.y * coordinates.height);
            }
            Matrix4x4 previous = GUI.matrix;
            GUI.BeginGroup(rect);
            try
            {
                Rect local = new Rect(command.iconOffset.x * rect.width, command.iconOffset.y * rect.height, rect.width, rect.height);
                Widgets.DrawTextureFitted(local, command.icon, command.iconDrawScale, proportions,
                    coordinates, command.iconAngle, command.overrideMaterial ?? buttonMaterial);
            }
            finally
            {
                GUI.EndGroup();
                GUI.matrix = previous;
            }
        }
    }
}
