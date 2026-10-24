using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Combat.GroundSpike.Abilities;
using NingshaRaceLib.Combat.GroundSpike.Utility;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Combat.GroundSpike.Tracking
{
    //类职责：保存一次砂岩棘环攻击，并逐圈生成地刺、结算伤害和径向击退。
    public sealed class GroundSpikeRingAttackSequence : IGroundSpikeAttackSequence
    {
        //字段职责：保存本次攻击的施术者与固定释放圆心。
        private readonly Pawn attacker;
        private readonly IntVec3 origin;

        //字段职责：保存攻击发生的地图和独立玩法参数。
        private readonly Map map;
        private readonly DamageDef damageDef;
        private readonly float damageAmount;
        private readonly float armorPenetration;
        private readonly int knockbackCells;
        private readonly float effectScale;

        //字段职责：保存由内向外排列的地刺圆环。
        private readonly List<GroundSpikeRingStep> steps;

        //字段职责：确保同一对象在整次环形扩散中最多受到一次伤害。
        private readonly HashSet<Thing> damagedTargets = new HashSet<Thing>();

        //构造函数职责：根据固定圆心和能力参数建立完整环形扩散步骤。
        public GroundSpikeRingAttackSequence(
            Pawn attacker,
            IntVec3 origin,
            CompProperties_AbilitySandstoneSpikeRing props,
            int startTick)
        {
            this.attacker = attacker;
            this.origin = origin;
            map = attacker.Map;
            damageDef = props.damageDef;
            damageAmount = props.damageAmount;
            armorPenetration = props.armorPenetration;
            knockbackCells = props.knockbackCells;
            effectScale = props.effectScale;

            if (props.radius <= 0f
                || props.ringStepTicks < 0
                || props.animationFrameCount <= 0
                || props.animationDurationTicks <= 0
                || props.impactFrame < 1
                || props.impactFrame > props.animationFrameCount)
            {
                throw new InvalidOperationException("砂岩棘环参数无效，无法建立攻击序列。");
            }

            int ringCount = Mathf.CeilToInt(props.radius);
            List<IntVec3>[] ringCells = new List<IntVec3>[ringCount];
            for (int i = 0; i < ringCells.Length; i++)
            {
                ringCells[i] = new List<IntVec3>();
            }

            //按到圆心的向上取整距离分组，确保每个地图格只属于一层一格厚圆环。
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(origin, props.radius, useCenter: false))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                int ringIndex = Mathf.CeilToInt(cell.DistanceTo(origin)) - 1;
                if (ringIndex >= 0 && ringIndex < ringCells.Length)
                {
                    ringCells[ringIndex].Add(cell);
                }
            }

            int impactOffset = (props.impactFrame - 1)
                * props.animationDurationTicks
                / props.animationFrameCount;
            steps = new List<GroundSpikeRingStep>(ringCount);
            for (int ringIndex = 0; ringIndex < ringCells.Length; ringIndex++)
            {
                if (ringCells[ringIndex].Count == 0)
                {
                    continue;
                }

                int spawnTick = startTick + ringIndex * props.ringStepTicks;
                steps.Add(new GroundSpikeRingStep(
                    ringCells[ringIndex],
                    spawnTick,
                    spawnTick + impactOffset));
            }
        }

        //函数职责：逐 Tick 生成到期圆环并在对应动画伤害帧结算目标。
        public bool Tick(int currentTick)
        {
            bool allImpacted = true;
            for (int i = 0; i < steps.Count; i++)
            {
                GroundSpikeRingStep step = steps[i];
                if (!step.spawned && currentTick >= step.spawnTick)
                {
                    SpawnMotes(step.cells);
                    step.spawned = true;
                }

                if (!step.impacted && currentTick >= step.impactTick)
                {
                    ApplyImpact(step.cells);
                    step.impacted = true;
                }

                if (!step.impacted)
                {
                    allImpacted = false;
                }
            }

            return allImpacted;
        }

        //函数职责：在当前圆环每个有效地图格生成现有逐帧地刺 Mote。
        private void SpawnMotes(List<IntVec3> cells)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                MoteMaker.MakeStaticMote(
                    cells[i].ToVector3Shifted(),
                    map,
                    DefOfRefs.NingshaRace_Mote_GroundSpikeFrameAnimation,
                    effectScale,
                    exactRot: 0f);
            }
        }

        //函数职责：扫描当前圆环内的敌对对象并施加一次伤害与远离圆心的击退。
        private void ApplyImpact(List<IntVec3> cells)
        {
            for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
            {
                List<Thing> things = cells[cellIndex].GetThingList(map);
                for (int thingIndex = things.Count - 1; thingIndex >= 0; thingIndex--)
                {
                    Thing target = things[thingIndex];
                    if (damagedTargets.Contains(target)
                        || !GroundSpikeCombatUtility.IsDamageTarget(attacker, target))
                    {
                        continue;
                    }

                    damagedTargets.Add(target);
                    DamageWorker.DamageResult result = GroundSpikeCombatUtility.ApplyDamage(
                        attacker,
                        DefOfRefs.NingshaRace_GroundSpikeSummoner,
                        origin,
                        damageDef,
                        damageAmount,
                        armorPenetration,
                        target);
                    if (result.totalDamageDealt > 0f && target is Pawn targetPawn)
                    {
                        Vector3 knockbackDirection = GroundSpikeCombatUtility.HorizontalDirection(
                            origin.ToVector3Shifted(),
                            targetPawn.Position.ToVector3Shifted());
                        GroundSpikeCombatUtility.ApplyPawnFlyerKnockback(
                            targetPawn,
                            knockbackDirection,
                            knockbackCells);
                    }
                }
            }
        }
    }
}
