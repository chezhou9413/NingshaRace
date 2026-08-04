using AlienRace;
using Verse;

using NingshaRaceLib.Erosion.Utility;

namespace NingshaRaceLib.Erosion.Rendering
{
    //类职责：识别侵蚀体需要隐藏或替换材质的 HAR 头部附加层。
    public static class ErosionBodyRenderingUtility
    {
        //字段职责：标识承载凝砂族完整头部贴图的 HAR BodyAddon。
        private const string HeadBodyAddonName = "NingshaRace_Head";

        //字段职责：标识侵蚀体状态下不再绘制的脸部表情 HAR BodyAddon。
        private const string FaceExpressionBodyAddonName = "NingshaRace_FaceExpression";

        //函数职责：判断渲染节点是否为侵蚀体当前使用的凝砂族头部附加层。
        public static bool IsErosionHeadNode(PawnRenderNode node, Pawn pawn)
        {
            return ErosionPawnUtility.IsErosionBody(pawn)
                && TryGetBodyAddonName(node, out string addonName)
                && addonName == HeadBodyAddonName;
        }

        //函数职责：判断渲染节点是否为侵蚀体应当隐藏的脸部表情附加层。
        public static bool IsErosionFaceExpressionNode(PawnRenderNode node, Pawn pawn)
        {
            return ErosionPawnUtility.IsErosionBody(pawn)
                && TryGetBodyAddonName(node, out string addonName)
                && addonName == FaceExpressionBodyAddonName;
        }

        //函数职责：从 HAR BodyAddon 渲染节点读取稳定的配置名称。
        private static bool TryGetBodyAddonName(PawnRenderNode node, out string addonName)
        {
            AlienPawnRenderNode_BodyAddon bodyAddonNode = node as AlienPawnRenderNode_BodyAddon;
            addonName = bodyAddonNode?.props?.addon?.Name;
            return !addonName.NullOrEmpty();
        }
    }
}
