using System.Collections.Generic;

namespace NingshaRaceLib.GiantTomb.Content.Config
{
    //类职责：声明一个敌人抽取结果及结果内可同时生成的威胁条目。
    public sealed class NingshaGiantTombEnemyOutcome
    {
        public float weight;
        public List<NingshaGiantTombThreatSpawn> spawns = new List<NingshaGiantTombThreatSpawn>();

        //函数职责：报告结果权重和内部威胁条目的配置错误。
        public IEnumerable<string> ConfigErrors(string owner)
        {
            if (weight <= 0f)
            {
                yield return owner + ": weight必须大于零";
            }
            if (spawns == null)
            {
                yield return owner + ": spawns不能为null，空结果应使用空列表";
                yield break;
            }
            for (int i = 0; i < spawns.Count; i++)
            {
                if (spawns[i] == null)
                {
                    yield return owner + ": spawns[" + i + "]不能为空";
                    continue;
                }
                foreach (string error in spawns[i].ConfigErrors(owner + ".spawns[" + i + "]"))
                {
                    yield return error;
                }
            }
        }
    }
}
