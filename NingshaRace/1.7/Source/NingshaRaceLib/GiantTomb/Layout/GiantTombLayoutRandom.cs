using System;
using System.Collections.Generic;

namespace NingshaRaceLib.GiantTomb.Layout
{
    //类职责：为后台布局搜索提供不依赖Verse.Rand全局状态的确定性局部随机数。
    internal sealed class GiantTombLayoutRandom
    {
        private uint state;

        //函数职责：使用主线程预先分配的种子初始化独立随机序列。
        public GiantTombLayoutRandom(int seed)
        {
            state = unchecked((uint)seed);
            if (state == 0u) state = 0x9E3779B9u;
        }

        //函数职责：返回包含上下界的随机整数。
        public int RangeInclusive(int minimum, int maximum)
        {
            if (maximum < minimum) throw new ArgumentOutOfRangeException(nameof(maximum));
            uint range = (uint)(maximum - minimum + 1);
            return minimum + (int)(NextUInt() % range);
        }

        //函数职责：返回零到一之间且不包含一的随机浮点数。
        public float Value()
        {
            return (NextUInt() & 0x00FFFFFFu) / 16777216f;
        }

        //函数职责：以相同概率返回真假值。
        public bool Bool()
        {
            return (NextUInt() & 1u) != 0u;
        }

        //函数职责：使用Fisher-Yates算法原地打乱局部列表。
        public void Shuffle<T>(IList<T> values)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int other = RangeInclusive(0, i);
                T temporary = values[i];
                values[i] = values[other];
                values[other] = temporary;
            }
        }

        //函数职责：推进xorshift32状态并返回下一个无符号整数。
        private uint NextUInt()
        {
            uint value = state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            state = value;
            return value;
        }
    }
}
