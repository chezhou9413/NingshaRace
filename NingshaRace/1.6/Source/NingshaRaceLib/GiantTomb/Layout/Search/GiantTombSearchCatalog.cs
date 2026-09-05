using System.Collections.Generic;
using ChezhouLib.CustomMission.MapTemplates;
using Verse;

namespace NingshaRaceLib.GiantTomb.Layout
{
    //类职责：在主线程冻结模板几何与变换规则，让全部布局尝试共享一次预计算结果。
    internal sealed class GiantTombSearchCatalog
    {
        public readonly GiantTombModule[] Modules;
        private readonly Dictionary<GiantTombModule, int> indices = new Dictionary<GiantTombModule, int>();
        private readonly GiantTombPlacementVariant[][] byDirection;

        //职责：按模板允许的旋转镜像组合构造原型，并按接口朝向建立稳定查找表。
        public GiantTombSearchCatalog(IReadOnlyList<GiantTombModule> modules)
        {
            Modules = new GiantTombModule[modules.Count];
            List<GiantTombPlacementVariant>[] directions = new List<GiantTombPlacementVariant>[4];
            for (int direction = 0; direction < 4; direction++) directions[direction] = new List<GiantTombPlacementVariant>();
            for (int index = 0; index < modules.Count; index++)
            {
                GiantTombModule module = Modules[index] = modules[index];
                indices.Add(module, index);
                int rotations = module.Template.AllowRotation ? 4 : 1;
                int mirrors = module.Template.AllowMirror ? 2 : 1;
                for (int rotation = 0; rotation < rotations; rotation++)
                {
                    for (int mirror = 0; mirror < mirrors; mirror++)
                    {
                        GiantTombPlacementPrototype prototype = new GiantTombPlacementPrototype(module,
                            new ClMapTransform(new Rot4(rotation), mirror != 0));
                        for (int connector = 0; connector < prototype.Connectors.Length; connector++)
                        {
                            directions[prototype.Connectors[connector].Direction.AsInt].Add(
                                new GiantTombPlacementVariant(index, connector, prototype));
                        }
                    }
                }
            }
            byDirection = new GiantTombPlacementVariant[4][];
            for (int direction = 0; direction < 4; direction++) byDirection[direction] = directions[direction].ToArray();
        }

        //职责：查询稳定模块编号，避免在回溯热点中反复去重同类实例。
        public int IndexOf(GiantTombModule module)
        {
            return indices[module];
        }

        //职责：返回只读使用的指定朝向接口原型数组。
        public GiantTombPlacementVariant[] Facing(Rot4 direction)
        {
            return byDirection[direction.AsInt];
        }
    }
}
