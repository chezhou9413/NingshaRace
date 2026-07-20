using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Combat
{
    //类职责：登记地刺召唤物攻击并逐 Tick 推进所有直线地刺横排。
    public class GameComponent_GroundSpikeAttacks : GameComponent
    {
        //字段职责：保存当前仍有横排尚未完成伤害结算的地刺攻击。
        private readonly List<GroundSpikeAttackSequence> attacks = new List<GroundSpikeAttackSequence>();

        //构造函数职责：让 RimWorld 为当前游戏创建地刺攻击组件。
        public GameComponent_GroundSpikeAttacks(Game game)
        {
        }

        //属性职责：获取当前游戏的地刺攻击组件。
        public static GameComponent_GroundSpikeAttacks Current
        {
            get
            {
                return Verse.Current.Game.GetComponent<GameComponent_GroundSpikeAttacks>();
            }
        }

        //函数职责：登记从起点逐行推进到目标格的地刺攻击。
        public void Register(Verb_GroundSpikeSummoner verb, IntVec3 origin, IntVec3 targetCell, Vector3 attackDirection)
        {
            attacks.Add(new GroundSpikeAttackSequence(
                verb,
                origin,
                targetCell,
                attackDirection,
                Find.TickManager.TicksGame));
        }

        //函数职责：生成到期横排的中心 Mote、结算三格伤害并移除完成的攻击序列。
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
