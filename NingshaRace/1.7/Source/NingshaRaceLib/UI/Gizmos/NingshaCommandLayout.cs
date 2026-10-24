using UnityEngine;
using Verse;

namespace NingshaRaceLib.UI.Gizmos
{
    //结构职责：统一分配命令图标、名称和冷却细条，不为角落提示空出整行。
    internal readonly struct NingshaCommandLayout
    {
        public const float Width = 80f;
        public readonly Rect Icon;
        public readonly Rect Label;
        public readonly Rect Cooldown;
        public readonly bool HasLabel;

        //职责：按实测文字高度从底部预留名称，其余主要空间全部交给图标。
        public NingshaCommandLayout(Rect rect, bool shrunk, bool ability)
        {
            Rect inner = rect.ContractedBy(3f);
            float line = Text.LineHeightOf(GameFont.Tiny) + 2f;
            HasLabel = !shrunk && inner.height >= line + 12f;
            Label = HasLabel ? new Rect(inner.x, inner.yMax - line, inner.width, line) : Rect.zero;
            float bottom = HasLabel ? Label.y - 1f : inner.yMax;
            Cooldown = new Rect(inner.x, bottom - 3f, inner.width, 3f);
            Icon = new Rect(inner.x, inner.y, inner.width, Mathf.Max(0f, (ability ? Cooldown.y - 1f : bottom) - inner.y));
        }
    }
}
