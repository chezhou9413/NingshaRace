using System;
using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.GiantTomb.Content.Generation
{
    //类职责：使用RimWorld当前随机状态执行可复现的相对权重抽取。
    internal static class GiantTombWeightedUtility
    {
        //函数职责：从非空列表中按正权重返回一个条目。
        public static T Pick<T>(IList<T> entries, Func<T, float> weightSelector)
        {
            if (entries == null || entries.Count == 0)
            {
                throw new InvalidOperationException("权重抽取列表不能为空。");
            }

            float total = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                total += weightSelector(entries[i]);
            }
            if (total <= 0f)
            {
                throw new InvalidOperationException("权重抽取列表的总权重必须大于零。");
            }

            float roll = Rand.Range(0f, total);
            for (int i = 0; i < entries.Count; i++)
            {
                roll -= weightSelector(entries[i]);
                if (roll <= 0f)
                {
                    return entries[i];
                }
            }
            return entries[entries.Count - 1];
        }
    }
}
