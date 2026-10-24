using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Combat.SnakeBellySword.Rendering;
using NingshaRaceLib.Combat.SnakeBellySword.Utility;
using NingshaRaceLib.Combat.SnakeBellySword.Verbs;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Combat.SnakeBellySword.Tracking
{
    //类职责：保存一次蛇腹剑挥击的固定方向，并按武器 Def 配置的帧序列结算伤害。
    public class SnakeBellySwordAttackSequence
    {
        //字段职责：提供本轮攻击的伤害参数、攻击者和武器来源。
        private readonly Verb_SnakeBellySword verb;

        //字段职责：固定动画开始时的地图和攻击起点。
        private readonly Map map;
        private readonly IntVec3 origin;

        //字段职责：固定动画开始时的水平挥击方向。
        private readonly Vector3 attackDirection;

        //字段职责：记录动画开始 Tick 和下一段待结算伤害的索引。
        private readonly int startTick;
        private int nextDamageIndex;

        //构造函数职责：验证武器 Def 中的动画结算帧，并建立本次伤害序列。
        public SnakeBellySwordAttackSequence(Verb_SnakeBellySword verb, Vector3 attackDirection, int startTick)
        {
            this.verb = verb;
            map = verb.CasterPawn.Map;
            origin = verb.CasterPawn.Position;
            this.attackDirection = attackDirection;
            this.startTick = startTick;

            VerbProperties_SnakeBellySword props = verb.Props;
            if (props.animationFrameCount <= 0 || props.damageFrames == null || props.damageFrames.Count == 0)
            {
                throw new InvalidOperationException("蛇腹剑动画总帧或伤害帧列表未正确配置。");
            }

            int previousFrame = 0;
            for (int i = 0; i < props.damageFrames.Count; i++)
            {
                int frame = props.damageFrames[i];
                if (frame <= previousFrame || frame > props.animationFrameCount)
                {
                    throw new InvalidOperationException("蛇腹剑伤害帧必须按升序排列并位于动画总帧范围内。");
                }

                previousFrame = frame;
            }
        }

        //函数职责：结算当前 Tick 已经到达的动画伤害帧，并返回序列是否结束。
        public bool Tick(int currentTick)
        {
            Pawn attacker = verb.CasterPawn;
            if (attacker == null || attacker.Destroyed || !attacker.Spawned || attacker.Map != map)
            {
                return true;
            }

            List<int> damageFrames = verb.Props.damageFrames;
            while (nextDamageIndex < damageFrames.Count && currentTick >= DamageTick(nextDamageIndex))
            {
                ApplyDamageStage(nextDamageIndex == damageFrames.Count - 1);
                nextDamageIndex++;
            }

            return nextDamageIndex >= damageFrames.Count;
        }

        //函数职责：把指定伤害帧换算为动画开始后的游戏 Tick。
        private int DamageTick(int damageIndex)
        {
            int frameIndex = verb.Props.damageFrames[damageIndex] - 1;
            int tickOffset = frameIndex * verb.Props.weaponHiddenTicks / verb.Props.animationFrameCount;
            return startTick + tickOffset;
        }

        //函数职责：重新扫描挥击扇形并对当前范围内的每个有效目标结算一次伤害，最后一段附带击退。
        private void ApplyDamageStage(bool applyKnockback)
        {
            Pawn attacker = verb.CasterPawn;
            List<Thing> targets = SnakeBellySwordCombatUtility.FindTargets(
                attacker,
                map,
                origin,
                attackDirection,
                verb.Props.coneAngle,
                verb.EffectiveRange);
            for (int i = 0; i < targets.Count; i++)
            {
                Thing target = targets[i];
                DamageWorker.DamageResult result = SnakeBellySwordCombatUtility.ApplyDamage(verb, target);
                if (applyKnockback && result.totalDamageDealt > 0f && target is Pawn targetPawn)
                {
                    SnakeBellySwordCombatUtility.ApplyKnockback(origin, targetPawn, verb.Props.knockbackCells);
                }
            }
        }
    }
}
