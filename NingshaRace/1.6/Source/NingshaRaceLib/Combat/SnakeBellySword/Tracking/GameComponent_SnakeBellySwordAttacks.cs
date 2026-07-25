using System.Collections.Generic;
using UnityEngine;
using Verse;

using NingshaRaceLib.Combat.SnakeBellySword.Rendering;
using NingshaRaceLib.Combat.SnakeBellySword.Utility;
using NingshaRaceLib.Combat.SnakeBellySword.Verbs;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Combat.SnakeBellySword.Tracking
{
    //类职责：登记蛇腹剑攻击并在游戏 Tick 中推进所有动画同步伤害序列。
    public class GameComponent_SnakeBellySwordAttacks : GameComponent
    {
        //字段职责：保存当前仍有伤害段尚未结算的蛇腹剑攻击。
        private readonly List<SnakeBellySwordAttackSequence> attacks = new List<SnakeBellySwordAttackSequence>();

        //构造函数职责：让 RimWorld 为当前游戏创建蛇腹剑攻击组件。
        public GameComponent_SnakeBellySwordAttacks(Game game)
        {
        }

        //属性职责：获取当前游戏的蛇腹剑攻击组件。
        public static GameComponent_SnakeBellySwordAttacks Current
        {
            get
            {
                return Verse.Current.Game.GetComponent<GameComponent_SnakeBellySwordAttacks>();
            }
        }

        //函数职责：登记一轮以当前 Tick 为动画首帧的蛇腹剑攻击。
        public void Register(Verb_SnakeBellySword verb, Vector3 attackDirection)
        {
            attacks.Add(new SnakeBellySwordAttackSequence(verb, attackDirection, Find.TickManager.TicksGame));
        }

        //函数职责：逐 Tick 结算到达指定动画帧的伤害并移除完成的序列。
        public override void GameComponentTick()
        {
            int currentTick = Find.TickManager.TicksGame;
            for (int i = attacks.Count - 1; i >= 0; i--)
            {
                if (attacks[i].Tick(currentTick))
                {
                    attacks.RemoveAt(i);
                }
            }
        }
    }
}
