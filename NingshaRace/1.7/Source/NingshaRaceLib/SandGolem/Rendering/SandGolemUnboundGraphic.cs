using UnityEngine;
using Verse;

namespace NingshaRaceLib.SandGolem.Rendering
{
    //类职责：为开发工具或第三方直接生成的未绑定沙傀提供可见的砂色外形。
    [StaticConstructorOnStartup]
    public static class SandGolemUnboundGraphic
    {
        private static readonly Graphic graphic = GraphicDatabase.Get<Graphic_Multi>(
            "Things/Pawn/Humanlike/Silhouettes/Silhouette_HumanAdult", ShaderDatabase.Cutout,
            Vector2.one, new Color(0.76f, 0.62f, 0.38f));

        //函数职责：返回预加载材质，避免绘制线程创建 Unity 资源。
        public static Material MaterialFor(Rot4 facing)
        {
            return graphic.MatAt(facing);
        }
    }
}
