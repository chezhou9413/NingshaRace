using System;
using System.Collections;
using System.Collections.Generic;
using ChezhouLib.CustomMission.MapTemplates;
using NingshaRaceLib.DesertPit.Generation.Progress;
using NingshaRaceLib.GiantTomb.Content.Config;
using NingshaRaceLib.GiantTomb.Content.Generation;
using NingshaRaceLib.GiantTomb.Layout;
using NingshaRaceLib.PocketMaps.Generation;
using Verse;

namespace NingshaRaceLib.GiantTomb.Generation.Steps
{
    //类职责：在模板生成后按十九种模板的XML配置即时生成敌人和房间奖励。
    public sealed class GenStep_GiantTombContents : GenStep, INingshaIncrementalGenStep
    {
        public NingshaGiantTombContentCatalogDef catalogDef;

        public override int SeedPart => 682194507;

        //函数职责：兼容原版同步生成入口并完整执行房间内容生成流程。
        public override void Generate(Map map, GenStepParams parms)
        {
            foreach (object unused in GenerateIncrementally(map, parms))
            {
            }
        }

        //函数职责：逐模板抽取敌人与奖励，并在每个实例完成后交还画面帧。
        public IEnumerable GenerateIncrementally(Map map, GenStepParams parms)
        {
            if (catalogDef == null)
            {
                throw new InvalidOperationException("巨型墓葬内容步骤缺少catalogDef。");
            }
            GiantTombLayoutData data = GiantTombGenUtility.GetLayoutData();
            Dictionary<ClMapTemplateDef, RoomBinding> bindings = BuildBindings(catalogDef);
            int colonyIndex = 0;
            for (int i = 0; i < data.Placements.Count; i++)
            {
                GiantTombPlacement placement = data.Placements[i];
                RoomBinding binding;
                if (!bindings.TryGetValue(placement.Module.Def, out binding))
                {
                    throw new InvalidOperationException("墓葬模板缺少内容配置: " + placement.Module.Def.defName);
                }
                DesertPitGenerationProgress.SetStage("安置生物与物资 " + (i + 1) + "/" + data.Placements.Count);
                GiantTombContentCellPool cells = new GiantTombContentCellPool(map, placement);
                GiantTombThreatSpawner.Spawn(map, cells, binding.Profile, ref colonyIndex);
                GiantTombRewardSpawner.SpawnRoomRewards(map, cells, binding.Content);
                DesertPitGenerationProgress.SetStepFraction((i + 1f) / data.Placements.Count);
                yield return null;
            }
            DesertPitGenerationProgress.SetStepFraction(1f);
        }

        //函数职责：把五类内容Def展开为按模板直接查询的运行时映射。
        private static Dictionary<ClMapTemplateDef, RoomBinding> BuildBindings(NingshaGiantTombContentCatalogDef catalog)
        {
            Dictionary<ClMapTemplateDef, RoomBinding> result = new Dictionary<ClMapTemplateDef, RoomBinding>();
            for (int i = 0; i < catalog.roomContents.Count; i++)
            {
                NingshaGiantTombRoomContentDef content = catalog.roomContents[i];
                for (int j = 0; j < content.rooms.Count; j++)
                {
                    result.Add(content.rooms[j].template, new RoomBinding(content, content.rooms[j]));
                }
            }
            return result;
        }

        //类职责：关联模板所属房间类别及其专属敌人结果表。
        private sealed class RoomBinding
        {
            public readonly NingshaGiantTombRoomContentDef Content;
            public readonly NingshaGiantTombRoomProfile Profile;

            //构造函数职责：保存一个模板查询结果需要的两层配置引用。
            public RoomBinding(NingshaGiantTombRoomContentDef content, NingshaGiantTombRoomProfile profile)
            {
                Content = content;
                Profile = profile;
            }
        }
    }
}
