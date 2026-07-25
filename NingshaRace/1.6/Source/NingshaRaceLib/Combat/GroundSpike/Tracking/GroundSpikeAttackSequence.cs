using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Combat.GroundSpike.Rendering;
using NingshaRaceLib.Combat.GroundSpike.Utility;
using NingshaRaceLib.Combat.GroundSpike.Verbs;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Combat.GroundSpike.Tracking
{
    //类职责：保存一次直线地刺攻击，并同步各排唯一 Mote、三格伤害和击退。
    public class GroundSpikeAttackSequence
    {
        //字段职责：提供本次攻击的攻击者、武器和 XML 参数。
        private readonly Verb_GroundSpikeSummoner verb;

        //字段职责：保存攻击地图和固定击退方向。
        private readonly Map map;
        private readonly Vector3 attackDirection;

        //字段职责：保存各横排的唯一视觉格、三格伤害范围和结算时间。
        private readonly List<GroundSpikeWaveStep> steps;

        //字段职责：确保同一对象在整条地刺路径中最多受到一次伤害。
        private readonly HashSet<Thing> damagedTargets = new HashSet<Thing>();

        //构造函数职责：根据起点、目标和动画帧参数建立完整直线地刺步骤。
        public GroundSpikeAttackSequence(
            Verb_GroundSpikeSummoner verb,
            IntVec3 origin,
            IntVec3 targetCell,
            Vector3 attackDirection,
            int startTick)
        {
            this.verb = verb;
            map = verb.CasterPawn.Map;
            this.attackDirection = attackDirection;
            VerbProperties_GroundSpikeSummoner props = verb.Props;
            if (props.animationFrameCount <= 0 || props.impactFrame < 1 || props.impactFrame > props.animationFrameCount)
            {
                throw new InvalidOperationException("地刺动画帧参数无效，无法建立攻击序列。");
            }

            int impactOffset = (props.impactFrame - 1) * props.animationDurationTicks / props.animationFrameCount;
            List<List<IntVec3>> rows = GroundSpikeCombatUtility.BuildWaveRows(
                map,
                origin,
                targetCell,
                props.lineHalfWidth,
                out List<IntVec3> visualCells);
            steps = new List<GroundSpikeWaveStep>(rows.Count);
            for (int i = 0; i < rows.Count; i++)
            {
                int spawnTick = startTick + i * props.waveStepTicks;
                steps.Add(new GroundSpikeWaveStep(rows[i], visualCells[i], spawnTick, spawnTick + impactOffset));
            }
        }

        //函数职责：逐 Tick 生成各排中心地刺并在对应伤害帧结算横向三格。
        public bool Tick(int currentTick)
        {
            bool allImpacted = true;
            for (int i = 0; i < steps.Count; i++)
            {
                GroundSpikeWaveStep step = steps[i];
                if (!step.spawned && currentTick >= step.spawnTick)
                {
                    SpawnMote(step.visualCell);
                    step.spawned = true;
                }

                if (!step.impacted && currentTick >= step.impactTick)
                {
                    ApplyImpact(step.damageCells);
                    step.impacted = true;
                }

                if (!step.impacted)
                {
                    allImpacted = false;
                }
            }

            return allImpacted;
        }

        //函数职责：只在当前横排中心生成一份放大的地刺逐帧 Mote。
        private void SpawnMote(IntVec3 visualCell)
        {
            MoteMaker.MakeStaticMote(
                visualCell.ToVector3Shifted(),
                map,
                DefOfRefs.NingshaRace_Mote_GroundSpikeFrameAnimation,
                verb.Props.effectScale,
                exactRot: 0f);
        }

        //函数职责：动态扫描当前横排三格内的敌对对象并施加伤害和击退。
        private void ApplyImpact(List<IntVec3> damageCells)
        {
            Pawn attacker = verb.CasterPawn;
            for (int cellIndex = 0; cellIndex < damageCells.Count; cellIndex++)
            {
                List<Thing> things = damageCells[cellIndex].GetThingList(map);
                for (int thingIndex = things.Count - 1; thingIndex >= 0; thingIndex--)
                {
                    Thing target = things[thingIndex];
                    if (damagedTargets.Contains(target) || !GroundSpikeCombatUtility.IsDamageTarget(attacker, target))
                    {
                        continue;
                    }

                    damagedTargets.Add(target);
                    DamageWorker.DamageResult result = GroundSpikeCombatUtility.ApplyDamage(verb, target);
                    if (result.totalDamageDealt > 0f && target is Pawn targetPawn)
                    {
                        GroundSpikeCombatUtility.ApplyPawnFlyerKnockback(
                            targetPawn,
                            attackDirection,
                            verb.Props.knockbackCells);
                    }
                }
            }
        }
    }
}
