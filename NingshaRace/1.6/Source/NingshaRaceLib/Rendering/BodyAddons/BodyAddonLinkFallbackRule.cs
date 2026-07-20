using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.Rendering.BodyAddons
{
    //类职责：描述负责提供变体编号的源 BodyAddon 与需要透明回退的链接 BodyAddon。
    public sealed class BodyAddonLinkFallbackRule
    {
        //字段职责：指定提供完整变体编号的源 BodyAddon 名称。
        public string sourceBodyAddonName;

        //字段职责：指定跟随源编号并允许缺图透明的链接 BodyAddon 名称。
        public string linkedBodyAddonName;

        //字段职责：显式声明源层变体数量，使源层自身缺图时也能由透明回退接管。
        public int sourceVariantCount;

        //函数职责：生成用于识别重复链接规则的稳定键。
        public string BuildKey()
        {
            return (sourceBodyAddonName ?? string.Empty) + "\n" + (linkedBodyAddonName ?? string.Empty);
        }

        //函数职责：检查单条链接规则的两个 BodyAddon 名称是否完整。
        public IEnumerable<string> ConfigErrors(int index)
        {
            if (sourceBodyAddonName.NullOrEmpty())
            {
                yield return "BodyAddonLinkFallbackExtension.rules[" + index + "].sourceBodyAddonName 不能为空";
            }
            if (linkedBodyAddonName.NullOrEmpty())
            {
                yield return "BodyAddonLinkFallbackExtension.rules[" + index + "].linkedBodyAddonName 不能为空";
            }
            if (sourceBodyAddonName == linkedBodyAddonName)
            {
                yield return "BodyAddonLinkFallbackExtension.rules[" + index + "] 不能链接到自身";
            }
            if (sourceVariantCount < 0)
            {
                yield return "BodyAddonLinkFallbackExtension.rules[" + index + "].sourceVariantCount 不能小于零";
            }
        }
    }
}
