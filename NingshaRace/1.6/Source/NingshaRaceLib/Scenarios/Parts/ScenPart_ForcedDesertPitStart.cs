using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace NingshaRaceLib.Scenarios.Parts
{
    //类职责：跳过玩家手动选址，并将凝砂族专属开局固定到可建立殖民地的沙漠世界格。
    public sealed class ScenPart_ForcedDesertPitStart : ScenPart_ForcedMap
    {
        //函数职责：在世界生成完成后选择沙漠世界格，并指定开局专用地下地图生成器。
        public override void PostWorldGenerate()
        {
            if (mapGenerator == null)
            {
                throw new InvalidOperationException("凝砂族沙漠地底开局缺少地图生成器配置。");
            }

            PlanetLayer surface = Find.WorldGrid.FirstLayerOfDef(layerDef);
            if (surface == null)
            {
                throw new InvalidOperationException("凝砂族沙漠地底开局无法找到地表世界层。");
            }

            List<PlanetTile> candidates = CollectDesertTiles(surface);
            if (candidates.Count == 0)
            {
                throw new InvalidOperationException("当前世界参数没有生成可建立殖民地的沙漠区域，无法开始凝砂族沙漠地底开局。");
            }

            Find.GameInitData.startingTile = candidates.RandomElement();
            Find.GameInitData.mapGeneratorDef = mapGenerator;
        }

        //函数职责：收集指定世界层中地貌为沙漠且满足原版殖民地选址规则的世界格。
        private static List<PlanetTile> CollectDesertTiles(PlanetLayer layer)
        {
            List<PlanetTile> candidates = new List<PlanetTile>();
            for (int i = 0; i < layer.TilesCount; i++)
            {
                Tile worldTile = layer[i];
                if (worldTile.PrimaryBiome == BiomeDefOf.Desert && TileFinder.IsValidTileForNewSettlement(worldTile.tile))
                {
                    candidates.Add(worldTile.tile);
                }
            }

            return candidates;
        }

        //函数职责：向场景信息面板说明自动沙漠选址与地下开局规则。
        public override string Summary(Scenario scen)
        {
            return "自动在随机沙漠区域的地下巨坑开始，不能手动选择地形。";
        }

        //函数职责：报告场景定义中缺失的地图生成器或世界层配置。
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (mapGenerator == null)
            {
                yield return "凝砂族沙漠地底开局未配置 mapGenerator。";
            }

            if (layerDef == null)
            {
                yield return "凝砂族沙漠地底开局未配置 layerDef。";
            }
        }
    }
}
