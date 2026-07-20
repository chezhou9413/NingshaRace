using System.Collections.Generic;
using AlienRace;
using Verse;

namespace NingshaRaceLib.Rendering.BodyAddons
{
    //类职责：注册种族的 BodyAddon 链接规则，并让后层变体数量与源层保持一致。
    public static class BodyAddonLinkFallbackUtility
    {
        //字段职责：保存需要对缺失方向使用透明材质的 BodyAddon 基础贴图路径。
        private static readonly List<string> linkedTexturePaths = new List<string>();

        //函数职责：扫描所有种族扩展、同步链接层变体数量并缓存受管贴图路径。
        public static void Initialize()
        {
            linkedTexturePaths.Clear();
            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                BodyAddonLinkFallbackExtension extension = thingDef.GetModExtension<BodyAddonLinkFallbackExtension>();
                if (extension?.rules == null)
                {
                    continue;
                }

                ThingDef_AlienRace alienRaceDef = thingDef as ThingDef_AlienRace;
                if (alienRaceDef == null)
                {
                    Log.Error(thingDef.defName + " 的 BodyAddonLinkFallbackExtension 只能用于 HAR 种族 ThingDef");
                    continue;
                }

                List<AlienPartGenerator.BodyAddon> bodyAddons = alienRaceDef.alienRace.generalSettings.alienPartGenerator.bodyAddons;
                for (int index = 0; index < extension.rules.Count; index++)
                {
                    RegisterRule(thingDef.defName, bodyAddons, extension.rules[index]);
                }
            }
        }

        //函数职责：判断 Graphic 基础路径是否属于允许缺失方向的链接 BodyAddon 变体。
        public static bool IsManagedTexturePath(string graphicPath)
        {
            if (graphicPath.NullOrEmpty())
            {
                return false;
            }

            for (int index = 0; index < linkedTexturePaths.Count; index++)
            {
                string basePath = linkedTexturePaths[index];
                if (graphicPath == basePath || IsNumberedVariantPath(graphicPath, basePath))
                {
                    return true;
                }
            }

            return false;
        }

        //函数职责：解析一条链接规则并把链接层的变体统计同步为源层数量。
        private static void RegisterRule(string raceDefName, List<AlienPartGenerator.BodyAddon> bodyAddons, BodyAddonLinkFallbackRule rule)
        {
            if (rule == null)
            {
                return;
            }

            AlienPartGenerator.BodyAddon sourceAddon = FindAddon(bodyAddons, rule.sourceBodyAddonName);
            AlienPartGenerator.BodyAddon linkedAddon = FindAddon(bodyAddons, rule.linkedBodyAddonName);
            if (sourceAddon == null || linkedAddon == null)
            {
                Log.Error(raceDefName + " 的 BodyAddon 链接规则找不到源层或链接层：" + rule.sourceBodyAddonName + " -> " + rule.linkedBodyAddonName);
                return;
            }
            if (!linkedAddon.linkVariantIndexWithPrevious)
            {
                Log.Error(raceDefName + " 的链接层 " + linkedAddon.Name + " 必须设置 linkVariantIndexWithPrevious=true");
                return;
            }

            int variantCount = rule.sourceVariantCount > 0 ? rule.sourceVariantCount : sourceAddon.GetVariantCount();
            if (variantCount <= 0)
            {
                Log.Error(raceDefName + " 的源 BodyAddon " + sourceAddon.Name + " 没有可用变体");
                return;
            }

            if (rule.sourceVariantCount > 0)
            {
                SetVariantCount(sourceAddon, variantCount);
                RegisterManagedPath(sourceAddon.path);
            }
            SetVariantCount(linkedAddon, variantCount);
            RegisterManagedPath(linkedAddon.path);
        }

        //函数职责：初始化指定 BodyAddon 的路径集合，并把变体统计设置为目标数量。
        private static void SetVariantCount(AlienPartGenerator.BodyAddon addon, int variantCount)
        {
            if (addon.paths.Count == 0)
            {
                addon.paths.Add(addon.path);
            }

            addon.variantCount = variantCount;
            addon.variantCounts.Clear();
            for (int index = 0; index < addon.paths.Count; index++)
            {
                addon.variantCounts.Add(index == 0 ? variantCount : 0);
            }
            addon.variantCountMax = variantCount;
        }

        //函数职责：把允许缺图透明的 BodyAddon 基础路径加入运行时匹配集合。
        private static void RegisterManagedPath(string texturePath)
        {
            if (!linkedTexturePaths.Contains(texturePath))
            {
                linkedTexturePaths.Add(texturePath);
            }
        }

        //函数职责：按名称从当前种族的 BodyAddon 列表中定位指定层。
        private static AlienPartGenerator.BodyAddon FindAddon(List<AlienPartGenerator.BodyAddon> bodyAddons, string addonName)
        {
            if (bodyAddons == null)
            {
                return null;
            }

            for (int index = 0; index < bodyAddons.Count; index++)
            {
                AlienPartGenerator.BodyAddon addon = bodyAddons[index];
                if (addon != null && addon.Name == addonName)
                {
                    return addon;
                }
            }

            return null;
        }

        //函数职责：判断路径是否为指定基础路径追加纯数字编号形成的变体路径。
        private static bool IsNumberedVariantPath(string graphicPath, string basePath)
        {
            if (!graphicPath.StartsWith(basePath) || graphicPath.Length == basePath.Length)
            {
                return false;
            }

            for (int index = basePath.Length; index < graphicPath.Length; index++)
            {
                if (!char.IsDigit(graphicPath[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
