using System;
using System.Collections.Generic;
using System.Linq;
using ChezhouLib.CustomMission.MapTemplates;
using NingshaRaceLib.GiantTomb.Layout;
using Verse;

namespace NingshaRaceLib.GiantTomb.Generation
{
    //类职责：在主线程组建满足接口守恒的模板池，并按固定预算准备确定性搜索尝试。
    internal static class GiantTombSearchPoolBuilder
    {
        //函数职责：按模板矩形面积选择指定数量的小型模块，供保底布局降低空间拥挤。
        internal static List<GiantTombModule> SelectSmallModules(List<GiantTombModule> modules, int count)
        {
            return modules.OrderBy((GiantTombModule module) => module.Width * module.Height)
                .ThenBy((GiantTombModule module) => module.Def.defName).Take(count).ToList();
        }

        //函数职责：在主线程把终点Def引用解析为稳定模块数组，避免后台访问Def配置集合。
        internal static GiantTombModule[] ResolveTerminalModules(List<GiantTombModule> required, List<ClMapTemplateDef> definitions)
        {
            GiantTombModule[] result = new GiantTombModule[definitions.Count];
            for (int i = 0; i < definitions.Count; i++)
            {
                GiantTombModule module = required.FirstOrDefault((GiantTombModule candidate) => candidate.Def == definitions[i]);
                if (module == null) throw new InvalidOperationException("巨型墓葬终点模板未加载: " + definitions[i]?.defName);
                result[i] = module;
            }
            return result;
        }

        //函数职责：在主线程预先冻结全部模块池、预算和局部随机种子，作为后台并行搜索的唯一输入。
        internal static GiantTombLayoutSearchAttempt[] BuildSearchAttempts(List<GiantTombModule> required, List<GiantTombModule> branches,
            List<GiantTombModule> leaves, List<GiantTombModule> transitRooms, List<GiantTombModule> corridors,
            int maximumAttempts, int totalCandidateBudget, IntRange repeatCountRange)
        {
            if (maximumAttempts < 1 || totalCandidateBudget < 1)
                throw new InvalidOperationException("墓葬布局尝试数和候选预算必须为正数");
            int attemptCount = Math.Min(maximumAttempts, totalCandidateBudget);
            int perAttemptBudget = totalCandidateBudget / attemptCount;
            List<GiantTombLayoutSearchAttempt> result = new List<GiantTombLayoutSearchAttempt>(attemptCount);
            for (int index = 0; index < attemptCount; index++)
            {
                int repeatCount = repeatCountRange.RandomInRange;
                List<GiantTombModule> pool = BuildPool(required, branches, leaves, transitRooms, corridors, repeatCount, out RepeatPoolCounts counts);
                ValidateDegreeInvariant(pool);
                int budget = perAttemptBudget + (index < totalCandidateBudget % attemptCount ? 1 : 0);
                result.Add(new GiantTombLayoutSearchAttempt(index, pool.ToArray(), Rand.Int, budget,
                    counts.Branches, counts.Leaves, counts.TransitRooms, counts.Corridors));
            }
            if (result.Count == 0) throw new InvalidOperationException("巨型墓葬布局没有可执行的搜索预算");
            return result.ToArray();
        }

        //函数职责：把配置Def列表解析为已经加载的模块并验证该类别要求的连接点数量。
        internal static List<GiantTombModule> ResolveRepeatModules(List<GiantTombModule> required, List<ClMapTemplateDef> definitions, int connectorCount, string category)
        {
            List<GiantTombModule> result = new List<GiantTombModule>();
            for (int i = 0; i < definitions.Count; i++)
            {
                GiantTombModule module = required.FirstOrDefault((GiantTombModule candidate) => candidate.Def == definitions[i]);
                if (module == null || module.Connectors.Count != connectorCount)
                {
                    throw new InvalidOperationException("巨型墓葬" + category + "必须使用" + connectorCount + "连接点模板: " + definitions[i]?.defName);
                }
                result.Add(module);
            }
            return result;
        }

        //函数职责：按接口守恒比例组合分支、尽头、中转房和少量走廊，使新增模块仍能构成完整树。
        private static List<GiantTombModule> BuildPool(List<GiantTombModule> required, List<GiantTombModule> branches, List<GiantTombModule> leaves, List<GiantTombModule> transitRooms, List<GiantTombModule> corridors, int repeatCount, out RepeatPoolCounts counts)
        {
            int branchCount;
            int leafCount;
            int degreeTwoCount;
            if (!TryResolveRepeatCounts(required, repeatCount, out branchCount, out leafCount, out degreeTwoCount))
            {
                throw new InvalidOperationException("墓葬必选模板与额外模块数量无法满足接口守恒: " + repeatCount);
            }
            int transitRoomCount = Math.Min(degreeTwoCount, Math.Max(1, (degreeTwoCount * 3 + 2) / 4));
            int corridorCount = degreeTwoCount - transitRoomCount;
            counts = new RepeatPoolCounts(branchCount, leafCount, transitRoomCount, corridorCount);

            for (int attempt = 0; attempt < 32; attempt++)
            {
                List<GiantTombModule> result = new List<GiantTombModule>(required.Count + repeatCount);
                result.AddRange(required);
                AddWeightedWithDiversity(result, branches, branchCount);
                AddWeightedWithDiversity(result, leaves, leafCount);
                AddWeightedWithDiversity(result, transitRooms, transitRoomCount);
                AddWeightedWithDiversity(result, corridors, corridorCount);
                if (GiantTombConnectorCompatibility.HasEvenConnectorComponents(result)) return result;
            }
            throw new InvalidOperationException("巨型墓葬随机模板池无法满足各接口兼容组的偶数闭合条件");
        }

        //函数职责：根据必选模板实际接口总数求出四接口、单接口和双接口额外模块数量。
        private static bool TryResolveRepeatCounts(List<GiantTombModule> required, int repeatCount,
            out int branches, out int leaves, out int degreeTwo)
        {
            int requiredConnectors = required.Sum(module => module.Connectors.Count);
            int expectedTotal = 2 * (required.Count + repeatCount - 1);
            int preferredBranches = Math.Min(repeatCount / 3, Math.Max(0, (repeatCount + 2) / 5));
            int preferredLeaves = Math.Min(repeatCount - preferredBranches, preferredBranches * 2);
            int preferredDegreeTwo = repeatCount - preferredBranches - preferredLeaves;
            int bestScore = int.MaxValue;
            branches = leaves = degreeTwo = 0;
            for (int branchCount = 0; branchCount <= repeatCount; branchCount++)
            {
                //接口守恒对叶节点数量是一次方程，每个分支数只存在一个可能的叶节点数。
                int leafCount = requiredConnectors + branchCount * 2 + repeatCount * 2 - expectedTotal;
                int twoCount = repeatCount - branchCount - leafCount;
                if (leafCount < 0 || twoCount < 0) continue;
                int score = Math.Abs(branchCount - preferredBranches) * 100
                    + Math.Abs(leafCount - preferredLeaves) * 10
                    + Math.Abs(twoCount - preferredDegreeTwo);
                if (score >= bestScore) continue;
                bestScore = score;
                branches = branchCount;
                leaves = leafCount;
                degreeTwo = twoCount;
            }
            return bestScore != int.MaxValue;
        }

        //函数职责：先尽量覆盖类别中的不同模板，再按Def权重补足该类别的重复数量。
        private static void AddWeightedWithDiversity(List<GiantTombModule> target, List<GiantTombModule> choices, int count)
        {
            if (count <= 0) return;
            List<GiantTombModule> shuffled = choices.InRandomOrder().ToList();
            int diverseCount = Math.Min(count, shuffled.Count);
            for (int i = 0; i < diverseCount; i++) target.Add(shuffled[i]);
            for (int i = diverseCount; i < count; i++)
            {
                target.Add(choices.RandomElementByWeight((GiantTombModule module) => module.Def.selectionWeight));
            }
        }

        //函数职责：验证所有连接点数量恰好能构成一个使用全部接口的树。
        private static void ValidateDegreeInvariant(List<GiantTombModule> pool)
        {
            int connectorCount = pool.Sum((GiantTombModule module) => module.Connectors.Count);
            int expected = 2 * (pool.Count - 1);
            if (connectorCount != expected)
            {
                throw new InvalidOperationException("巨型墓葬连接点总数不能构成完整树: " + connectorCount + " != " + expected);
            }
            if (!GiantTombConnectorCompatibility.HasEvenConnectorComponents(pool))
            {
                throw new InvalidOperationException("巨型墓葬接口兼容组存在奇数个连接点，无法全部闭合");
            }
        }

        //结构职责：保存一次额外模块池中各探索类型的实际数量，供日志和布局结果核对。
        private readonly struct RepeatPoolCounts
        {
            public readonly int Branches;
            public readonly int Leaves;
            public readonly int TransitRooms;
            public readonly int Corridors;

            //函数职责：记录一次满足接口守恒的分类抽取数量。
            public RepeatPoolCounts(int branches, int leaves, int transitRooms, int corridors)
            {
                Branches = branches;
                Leaves = leaves;
                TransitRooms = transitRooms;
                Corridors = corridors;
            }
        }
    }
}
