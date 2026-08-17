using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.GiantTomb.Content.Config
{
    //类职责：配置一种房间类别的模板敌人表与共享奖励池。
    public sealed class NingshaGiantTombRoomContentDef : Def
    {
        public List<NingshaGiantTombRoomProfile> rooms = new List<NingshaGiantTombRoomProfile>();
        public int rewardPickCount;
        public bool rewardWithReplacement;
        public List<NingshaWeightedThingEntry> rewards = new List<NingshaWeightedThingEntry>();

        //函数职责：在Def加载阶段验证房间表、抽取规则和奖励池。
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }
            if (rooms == null || rooms.Count == 0)
            {
                yield return defName + ": rooms不能为空";
            }
            else
            {
                for (int i = 0; i < rooms.Count; i++)
                {
                    if (rooms[i] == null)
                    {
                        yield return defName + ": rooms[" + i + "]不能为空";
                        continue;
                    }
                    foreach (string error in rooms[i].ConfigErrors(defName + ".rooms[" + i + "]"))
                    {
                        yield return error;
                    }
                }
            }
            if (rewardPickCount < 1)
            {
                yield return defName + ": rewardPickCount必须大于零";
            }
            if (rewards == null || rewards.Count == 0)
            {
                yield return defName + ": rewards不能为空";
                yield break;
            }
            if (!rewardWithReplacement && rewardPickCount > rewards.Count)
            {
                yield return defName + ": 无放回抽取次数不能超过奖励种类数";
            }
            for (int i = 0; i < rewards.Count; i++)
            {
                if (rewards[i] == null)
                {
                    yield return defName + ": rewards[" + i + "]不能为空";
                    continue;
                }
                foreach (string error in rewards[i].ConfigErrors(defName + ".rewards[" + i + "]"))
                {
                    yield return error;
                }
            }
        }
    }
}
