using AlienRace;
using Verse;

using NingshaRaceLib.Erosion.Utility;

namespace NingshaRaceLib.Erosion.Rendering
{
    //识别人形侵蚀体的实际头部及凝砂专用表情层。
    public static class ErosionBodyRenderingUtility
    {
        //字段职责：标识承载凝砂族完整头部贴图的 HAR BodyAddon。
        private const string HeadBodyAddonName = "NingshaRace_Head";

        //字段职责：标识侵蚀体状态下不再绘制的脸部表情 HAR BodyAddon。
        private const string FaceExpressionBodyAddonName = "NingshaRace_FaceExpression";

        //标准人形与 HAR 使用头部节点，凝砂使用完整头部 BodyAddon。
        public static bool IsErosionHeadNode(PawnRenderNode node, Pawn pawn)
        {
            if (!ErosionPawnUtility.IsErosionBody(pawn)) return false;
            if (ErosionPawnUtility.IsNingshaErosionBody(pawn))
                return TryGetBodyAddonName(node, out string addonName) && addonName == HeadBodyAddonName;
            return node is PawnRenderNode_Head;
        }

        //函数职责：判断渲染节点是否为侵蚀体应当隐藏的脸部表情附加层。
        public static bool IsErosionFaceExpressionNode(PawnRenderNode node, Pawn pawn)
        {
            return ErosionPawnUtility.IsNingshaErosionBody(pawn)
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
