using System.Collections.Generic;
using System.Linq;
using ChezhouLib.CustomMission.MapTemplates;
using Verse;

namespace NingshaRaceLib.GiantTomb.Config
{
    //类职责：声明巨型墓葬使用的全部模板、入口、终点候选和布局搜索限制。
    public sealed class NingshaGiantTombLayoutDef : Def
    {
        //字段职责：规定使用该布局定义时地图必须采用的正方形边长。
        public int requiredMapSize = 200;

        //字段职责：保存每张墓葬必须各出现一次的完整模板集合。
        public List<ClMapTemplateDef> modules = new List<ClMapTemplateDef>();

        //字段职责：指定承载返回出口并固定在地图南部的入口模板。
        public ClMapTemplateDef entranceTemplate;

        //字段职责：保存优先安排到最深支路的终点大墓室候选。
        public List<ClMapTemplateDef> terminalTemplates = new List<ClMapTemplateDef>();

        //字段职责：保存用于扩展支路数量的四连接点中转节点。
        public List<ClMapTemplateDef> repeatBranchTemplates = new List<ClMapTemplateDef>();

        //字段职责：保存用于封闭支路末端的尽头走廊和小房间。
        public List<ClMapTemplateDef> repeatLeafTemplates = new List<ClMapTemplateDef>();

        //字段职责：保存能够连接前后路径的二连接点房间。
        public List<ClMapTemplateDef> repeatTransitRoomTemplates = new List<ClMapTemplateDef>();

        //字段职责：保存少量用于调节路径长度和转向的二连接点走廊。
        public List<ClMapTemplateDef> repeatCorridorTemplates = new List<ClMapTemplateDef>();

        //字段职责：控制从分类模板池中额外抽取的实例总数。
        public IntRange repeatCount = new IntRange(24, 32);

        //字段职责：限制所有模块与地图边界之间的最小岩层宽度。
        public int borderMargin = 8;

        //字段职责：限制正常与紧凑阶段合计尝试数，正常阶段最多八次，紧凑阶段最多二十四次。
        public int maxRestarts = 32;

        //字段职责：限制正常布局阶段所有尝试合计的候选摆放总数。
        public int maxCandidateEvaluations = 500000;

        //字段职责：限制紧凑布局阶段的候选摆放总数，耗尽后明确报告失败，不无限重试。
        public int maxCompactCandidateEvaluations = 2400000;

        //函数职责：在Def加载阶段报告会使巨型墓葬无法生成的静态配置错误。
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (modules == null || modules.Count == 0)
            {
                yield return defName + ": modules不能为空";
            }
            if (requiredMapSize < 50)
            {
                yield return defName + ": requiredMapSize不能小于50";
            }
            if (modules != null && modules.Distinct().Count() != modules.Count)
            {
                yield return defName + ": modules不能包含重复模板";
            }
            if (entranceTemplate == null || modules == null || !modules.Contains(entranceTemplate))
            {
                yield return defName + ": entranceTemplate必须包含在modules中";
            }
            if (terminalTemplates == null || terminalTemplates.Count == 0)
            {
                yield return defName + ": terminalTemplates不能为空";
            }
            else
            {
                for (int i = 0; i < terminalTemplates.Count; i++)
                {
                    if (modules == null || !modules.Contains(terminalTemplates[i]))
                    {
                        yield return defName + ": 终点模板未包含在modules中: " + terminalTemplates[i]?.defName;
                    }
                }
            }
            if (repeatCount.min < 0 || repeatCount.max < repeatCount.min)
            {
                yield return defName + ": repeatCount无效";
            }
            foreach (string error in ValidateRepeatPools())
            {
                yield return error;
            }
            if (borderMargin < 1 || borderMargin * 2 >= requiredMapSize || maxRestarts < 1
                || maxCandidateEvaluations < 1 || maxCompactCandidateEvaluations < 1)
            {
                yield return defName + ": 布局搜索限制必须为正数";
            }
        }

        //函数职责：确认四类重复模板池非空、引用必选模板且彼此没有重复归类。
        private IEnumerable<string> ValidateRepeatPools()
        {
            HashSet<ClMapTemplateDef> classified = new HashSet<ClMapTemplateDef>();
            List<ClMapTemplateDef>[] pools = { repeatBranchTemplates, repeatLeafTemplates, repeatTransitRoomTemplates, repeatCorridorTemplates };
            string[] names = { "repeatBranchTemplates", "repeatLeafTemplates", "repeatTransitRoomTemplates", "repeatCorridorTemplates" };
            for (int poolIndex = 0; poolIndex < pools.Length; poolIndex++)
            {
                List<ClMapTemplateDef> pool = pools[poolIndex];
                if (pool == null || pool.Count == 0)
                {
                    yield return defName + ": " + names[poolIndex] + "不能为空";
                    continue;
                }
                for (int i = 0; i < pool.Count; i++)
                {
                    ClMapTemplateDef template = pool[i];
                    if (template == null || modules == null || !modules.Contains(template))
                    {
                        yield return defName + ": " + names[poolIndex] + "引用了modules之外的模板: " + template?.defName;
                    }
                    else if (!classified.Add(template))
                    {
                        yield return defName + ": 重复模板不能跨类别归类: " + template.defName;
                    }
                }
            }
        }
    }
}
