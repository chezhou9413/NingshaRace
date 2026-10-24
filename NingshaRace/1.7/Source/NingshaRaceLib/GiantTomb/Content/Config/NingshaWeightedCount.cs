using Verse;

namespace NingshaRaceLib.GiantTomb.Content.Config
{
    //类职责：声明一个带相对权重的整数数量档位。
    public sealed class NingshaWeightedCount
    {
        public int count;
        public float weight;

        //函数职责：报告数量档位中会阻止随机抽取的配置错误。
        public string ConfigError(string owner)
        {
            if (count < 1)
            {
                return owner + ": count必须大于零";
            }
            if (weight <= 0f)
            {
                return owner + ": weight必须大于零";
            }
            return null;
        }
    }
}
