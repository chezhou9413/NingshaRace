using System.Collections.Generic;
using ChezhouLib.CustomMission.MapTemplates;

namespace NingshaRaceLib.GiantTomb.Content.Config
{
    //类职责：把一个墓葬模板映射到其专属敌人结果表。
    public sealed class NingshaGiantTombRoomProfile
    {
        public ClMapTemplateDef template;
        public List<NingshaGiantTombEnemyOutcome> enemyOutcomes = new List<NingshaGiantTombEnemyOutcome>();

        //函数职责：报告模板引用和敌人结果表中的配置错误。
        public IEnumerable<string> ConfigErrors(string owner)
        {
            if (template == null)
            {
                yield return owner + ": template不能为空";
            }
            if (enemyOutcomes == null || enemyOutcomes.Count == 0)
            {
                yield return owner + ": enemyOutcomes不能为空";
                yield break;
            }
            for (int i = 0; i < enemyOutcomes.Count; i++)
            {
                if (enemyOutcomes[i] == null)
                {
                    yield return owner + ": enemyOutcomes[" + i + "]不能为空";
                    continue;
                }
                foreach (string error in enemyOutcomes[i].ConfigErrors(owner + ".enemyOutcomes[" + i + "]"))
                {
                    yield return error;
                }
            }
        }
    }
}
