using System.Collections.Generic;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Buildings;
using NingshaRaceLib.DesertPit.Generation.Caves;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Landmarks;
using NingshaRaceLib.DesertPit.Generation.Steps;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DevTools.DesertPit
{
    //类职责：集中维护凝砂开发者摆放工具可用的地形、建筑、植物和罐子条目。
    public static class NingshaDevPlacementCatalog
    {
        //函数职责：按显示顺序创建开发者摆放工具的全部条目。
        public static List<NingshaDevPlacementEntry> CreateEntries()
        {
            return new List<NingshaDevPlacementEntry>
            {
                new NingshaDevPlacementEntry("地形", "沙地", "Sand", true),
                new NingshaDevPlacementEntry("地形", "软沙", "SoftSand", true),
                new NingshaDevPlacementEntry("地形", "砂砾", "Gravel", true),
                new NingshaDevPlacementEntry("地形", "粗糙砂岩", "Sandstone_Rough", true),
                new NingshaDevPlacementEntry("地形", "浅水", "WaterShallow", true),
                new NingshaDevPlacementEntry("地形", "浅流水", "WaterMovingShallow", true),
                new NingshaDevPlacementEntry("地形", "沼泽", "Marsh", true),
                new NingshaDevPlacementEntry("地形", "泥泞地", "MarshyTerrain", true),

                new NingshaDevPlacementEntry("图腾石棺", "裂纹砂岩图腾", "NingshaRace_DesertPitTotemStatueA", false),
                new NingshaDevPlacementEntry("图腾石棺", "残缺砂岩图腾", "NingshaRace_DesertPitTotemStatueB", false),
                new NingshaDevPlacementEntry("图腾石棺", "倾斜砂岩图腾", "NingshaRace_DesertPitTotemStatueC", false),
                new NingshaDevPlacementEntry("图腾石棺", "封闭砂岩石棺", "NingshaRace_DesertPitSarcophagus", false),
                new NingshaDevPlacementEntry("图腾石棺", "开启砂岩石棺", "NingshaRace_DesertPitOpenSarcophagus", false),
                new NingshaDevPlacementEntry("图腾石棺", "古旧砂陶罐", "NingshaRace_DesertPitPot", false),

                new NingshaDevPlacementEntry("洞穴装饰", "沙岩钟乳石", "NingshaRace_DesertPitStalactiteA", false),
                new NingshaDevPlacementEntry("洞穴装饰", "细沙钟乳石", "NingshaRace_DesertPitStalactiteB", false),
                new NingshaDevPlacementEntry("洞穴装饰", "裂痕钟乳石", "NingshaRace_DesertPitStalactiteC", false),
                new NingshaDevPlacementEntry("洞穴装饰", "暗砂钟乳石", "NingshaRace_DesertPitStalactiteD", false),
                new NingshaDevPlacementEntry("洞穴装饰", "巨砂钟乳石", "NingshaRace_DesertPitStalactiteE", false),
                new NingshaDevPlacementEntry("洞穴装饰", "短砂钟乳石", "NingshaRace_DesertPitStalactiteF", false),
                new NingshaDevPlacementEntry("洞穴装饰", "沙埋骨骸", "NingshaRace_DesertPitBonesA", false),
                new NingshaDevPlacementEntry("洞穴装饰", "风化骨堆", "NingshaRace_DesertPitBonesB", false),
                new NingshaDevPlacementEntry("洞穴装饰", "幽光砂晶", "NingshaRace_DesertPitGlowCrystal", false),
                new NingshaDevPlacementEntry("洞穴装饰", "碎生砂晶", "NingshaRace_DesertPitGlowCrystalShard", false),
                new NingshaDevPlacementEntry("洞穴装饰", "高砂晶柱", "NingshaRace_DesertPitGlowCrystalPillar", false),
                new NingshaDevPlacementEntry("洞穴装饰", "簇生砂晶", "NingshaRace_DesertPitGlowCrystalBloom", false),
                new NingshaDevPlacementEntry("洞穴装饰", "坠砂裂隙", "NingshaRace_DesertPitCeilingSandfall", false),

                new NingshaDevPlacementEntry("植物", "蓝灰洞杯菌", "NingshaRace_DesertPitPlantA", false),
                new NingshaDevPlacementEntry("植物", "白晶砂芽", "NingshaRace_DesertPitPlantB", false),
                new NingshaDevPlacementEntry("植物", "浅青伞菇", "NingshaRace_DesertPitPlantC", false),
                new NingshaDevPlacementEntry("植物", "紫辉花簇菌", "NingshaRace_DesertPitPlantD", false),
                new NingshaDevPlacementEntry("植物", "黄绿晶卵草", "NingshaRace_DesertPitPlantE", false)
            };
        }
    }
}
