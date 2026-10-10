using FacialAnimation;
using Verse;

namespace NingshaRaceLib.Compatibility.FA
{
    //按 FA 的控制器和图层识别完整头部，不依赖贴图名称或调试标签。
    internal static class ErosionFacialAnimationUtility
    {
        //基础头部承载黑雾，其他 FA 部件属于可隐藏的面部叠层。
        internal static bool IsBaseHead(PawnRenderNode node)
        {
            return node is NLFacialAnimationPartNode part
                && part.controller is HeadControllerComp
                && part.layerType == NLFacialAnimationLayerType.Basic;
        }
    }
}
