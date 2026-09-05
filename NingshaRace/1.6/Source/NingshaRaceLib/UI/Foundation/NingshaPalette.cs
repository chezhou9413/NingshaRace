using UnityEngine;

namespace NingshaRaceLib.UI.Foundation
{
    //类职责：定义凝砂界面砂岩、旧铜、绿松石和侵蚀警示的语义配色与公共间距。
    public static class NingshaPalette
    {
        public const float Gap = 8f;
        public const float Padding = 12f;
        public static readonly Color Stone = new Color(0.13f, 0.105f, 0.075f);
        public static readonly Color Recess = new Color(0.065f, 0.057f, 0.045f);
        public static readonly Color Brass = new Color(0.57f, 0.43f, 0.23f);
        public static readonly Color Sand = new Color(0.84f, 0.68f, 0.40f);
        public static readonly Color Ink = new Color(0.94f, 0.87f, 0.70f);
        public static readonly Color Muted = new Color(0.65f, 0.59f, 0.46f);
        public static readonly Color Jade = new Color(0.34f, 0.66f, 0.57f);
        public static readonly Color Warning = new Color(0.83f, 0.42f, 0.22f);
        public static readonly Color Erosion = new Color(0.64f, 0.35f, 0.56f);
        public static readonly Color Danger = new Color(0.78f, 0.24f, 0.18f);
    }
}
