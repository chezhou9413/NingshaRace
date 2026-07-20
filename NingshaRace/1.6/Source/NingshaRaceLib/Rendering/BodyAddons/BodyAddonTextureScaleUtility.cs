using System.Collections.Generic;
using AlienRace;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Rendering.BodyAddons
{
    //类职责：缓存独立 Def 中的种族规则，并解析当前 BodyAddon 的缩放倍率与绘制偏移。
    public static class BodyAddonTextureScaleUtility
    {
        //字段职责：按 Pawn 种族缓存独立 Def 中声明的全部贴图变换规则。
        private static readonly Dictionary<ThingDef, List<BodyAddonTextureScaleRule>> rulesByRace = new Dictionary<ThingDef, List<BodyAddonTextureScaleRule>>();

        //函数职责：读取全部独立缩放 Def，并按目标种族建立绘制阶段使用的规则缓存。
        public static void Initialize()
        {
            rulesByRace.Clear();
            foreach (BodyAddonTextureScaleDef scaleDef in DefDatabase<BodyAddonTextureScaleDef>.AllDefsListForReading)
            {
                if (scaleDef.race == null || scaleDef.rules == null)
                {
                    continue;
                }

                if (!rulesByRace.TryGetValue(scaleDef.race, out List<BodyAddonTextureScaleRule> raceRules))
                {
                    raceRules = new List<BodyAddonTextureScaleRule>();
                    rulesByRace.Add(scaleDef.race, raceRules);
                }

                for (int index = 0; index < scaleDef.rules.Count; index++)
                {
                    BodyAddonTextureScaleRule rule = scaleDef.rules[index];
                    if (rule != null)
                    {
                        raceRules.Add(rule);
                    }
                }
            }
        }

        //函数职责：从种族规则缓存中查找当前 BodyAddon 绘制请求对应的缩放倍率、位置偏移和层级偏移。
        public static bool TryGetTransform(PawnRenderNode node, PawnDrawParms parms, out Vector2 scale, out Vector2 offset, out float layerOffset)
        {
            scale = Vector2.one;
            offset = Vector2.zero;
            layerOffset = 0f;
            AlienPawnRenderNode_BodyAddon bodyAddonNode = node as AlienPawnRenderNode_BodyAddon;
            AlienPawnRenderNodeProperties_BodyAddon props = bodyAddonNode?.props;
            Pawn pawn = parms.pawn;
            if (props?.addon == null || pawn?.def == null)
            {
                return false;
            }

            if (!rulesByRace.TryGetValue(pawn.def, out List<BodyAddonTextureScaleRule> rules))
            {
                return false;
            }

            Graphic graphic = bodyAddonNode.PrimaryGraphic ?? props.graphic;
            if (graphic == null || graphic.path.NullOrEmpty())
            {
                return false;
            }

            Rot4 facing = parms.facing;
            if (parms.flipHead && props.addon.alignWithHead)
            {
                facing = facing.Opposite;
            }

            for (int index = 0; index < rules.Count; index++)
            {
                BodyAddonTextureScaleRule rule = rules[index];
                if (rule != null && rule.Matches(props.addon.Name, graphic.path, facing))
                {
                    scale = rule.scale;
                    offset = rule.offset;
                    layerOffset = rule.layerOffset;
                    return true;
                }
            }

            return false;
        }
    }
}
