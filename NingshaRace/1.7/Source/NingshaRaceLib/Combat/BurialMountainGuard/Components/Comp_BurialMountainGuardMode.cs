using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

using NingshaRaceLib.Combat.BurialMountainGuard.Rendering;
using NingshaRaceLib.Combat.BurialMountainGuard.Utility;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Combat.BurialMountainGuard.Components
{
    //类职责：保存并执行葬岳格挡模式的开关、减伤、蓄力、爆发和护盾显示。
    public class Comp_BurialMountainGuardMode : ThingComp
    {
        //字段职责：记录当前武器是否处于格挡模式。
        private bool guardMode;

        //字段职责：记录本轮格挡是否已经显示过禁止攻击提示。
        private bool guardAttackMessageShown;

        //字段职责：累计尚未用于格挡爆发的吸收伤害。
        private float storedDamage;

        //字段职责：保存当前武器对应的常驻护盾，避免读档或重复通知时再次生成。
        private Mote_BurialMountainGuardShield shieldMote;

        //属性职责：返回葬岳 ThingDef 上配置的格挡参数。
        private CompProperties_BurialMountainGuardMode Props => (CompProperties_BurialMountainGuardMode)props;

        //属性职责：返回当前是否启用了格挡模式。
        public bool GuardMode => guardMode;

        //属性职责：返回尚未释放的格挡蓄力值。
        public float StoredDamage => storedDamage;

        //属性职责：把当前蓄力换算为零到一的护盾显示比例。
        public float ChargeRatio => Mathf.Clamp01(storedDamage / Mathf.Max(Props.chargeThreshold, 0.01f));

        //函数职责：保存格挡开关和蓄力值。
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref guardMode, "guardMode", false);
            Scribe_Values.Look(ref storedDamage, "storedDamage", 0f);
            Scribe_References.Look(ref shieldMote, "shieldMote");
        }

        //函数职责：装备葬岳时按当前格挡状态刷新护盾显示。
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            if (guardMode)
            {
                pawn.stances?.CancelBusyStanceHard();
                EnsureShieldMote(pawn);
            }
        }

        //函数职责：卸下葬岳时关闭格挡并清理护盾。
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            guardMode = false;
            DestroyShieldMote();
        }

        //函数职责：切换格挡模式，并在进入格挡时打断当前攻击动作。
        public void ToggleGuardMode(Pawn pawn)
        {
            guardMode = !guardMode;
            guardAttackMessageShown = false;
            if (guardMode)
            {
                InterruptCurrentAttack(pawn);
                EnsureShieldMote(pawn);
                return;
            }

            DestroyShieldMote();
        }

        //函数职责：记录本次格挡期间是否已经显示过禁攻提示，并只允许首次攻击尝试显示。
        public bool TryConsumeAttackBlockedMessage()
        {
            if (!guardMode || guardAttackMessageShown)
            {
                return false;
            }

            guardAttackMessageShown = true;
            return true;
        }

        //函数职责：按格挡减伤规则修改伤害并把吸收值加入蓄力。
        public void AbsorbDamage(Pawn pawn, ref DamageInfo dinfo, ref bool absorbed)
        {
            if (!guardMode || absorbed || pawn == null || pawn.Dead || dinfo.Amount <= 0f)
            {
                return;
            }

            float absorbedAmount = Mathf.Min(dinfo.Amount, Props.damageReduction);
            if (absorbedAmount <= 0f)
            {
                return;
            }

            float remainingDamage = dinfo.Amount - absorbedAmount;
            storedDamage += absorbedAmount;
            SpawnAbsorbDust(pawn);

            if (remainingDamage <= 0.01f)
            {
                absorbed = true;
                dinfo.SetAmount(0f);
            }
            else
            {
                dinfo.SetAmount(remainingDamage);
            }

            ReleaseIfCharged(pawn);
        }

        //函数职责：蓄力满值时消耗阈值并释放敌对目标范围伤害。
        private void ReleaseIfCharged(Pawn pawn)
        {
            while (storedDamage >= Props.chargeThreshold)
            {
                storedDamage -= Props.chargeThreshold;
                DoReleaseDamage(pawn);
                SpawnBurstMote(pawn);
                SpawnReleaseDust(pawn);
            }
        }

        //函数职责：对爆发半径内敌对 Pawn 造成配置倍率伤害。
        private void DoReleaseDamage(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
            {
                return;
            }

            float damageAmount = Props.chargeThreshold * Props.releaseDamageMultiplier;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, Props.releaseRadius, true))
            {
                if (!cell.InBounds(pawn.Map))
                {
                    continue;
                }

                foreach (Thing thing in cell.GetThingList(pawn.Map).ToArray())
                {
                    Pawn targetPawn = thing as Pawn;
                    if (targetPawn == null || targetPawn == pawn || targetPawn.Dead || !targetPawn.HostileTo(pawn))
                    {
                        continue;
                    }

                    DamageInfo damageInfo = new DamageInfo(Props.releaseDamageDef, damageAmount, Props.armorPenetration, -1f, pawn, null, parent.def);
                    targetPawn.TakeDamage(damageInfo);
                }
            }
        }

        //函数职责：进入格挡时中断当前攻击、施法和瞄准姿态。
        private void InterruptCurrentAttack(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            pawn.stances?.CancelBusyStanceHard();
            Job currentJob = pawn.CurJob;
            if (currentJob != null && (currentJob.verbToUse != null || (currentJob.ability != null && currentJob.ability.def == DefOfRefs.NingshaRace_Ability_FallingMountainSlash) || currentJob.def == JobDefOf.AttackMelee))
            {
                pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
            }
        }

        //函数职责：生成并初始化跟随持剑者的常驻菲涅尔护盾。
        private void EnsureShieldMote(Pawn pawn)
        {
            if (shieldMote != null && !shieldMote.Destroyed && shieldMote.Spawned)
            {
                shieldMote.Initialize(pawn, this);
                return;
            }

            Mote mote = MoteMaker.MakeStaticMote(pawn.DrawPos, pawn.Map, DefOfRefs.NingshaRace_Mote_BurialMountainGuardShield, Props.shieldScale, true);
            shieldMote = mote as Mote_BurialMountainGuardShield;
            if (shieldMote != null)
            {
                shieldMote.Initialize(pawn, this);
                shieldMote.UpdateVisuals(ChargeRatio);
            }
        }

        //函数职责：销毁当前护盾 Mote。
        private void DestroyShieldMote()
        {
            if (shieldMote != null && !shieldMote.Destroyed)
            {
                shieldMote.Destroy();
            }
            shieldMote = null;
        }

        //函数职责：在满蓄力释放时生成爆发 Mote。
        private void SpawnBurstMote(Pawn pawn)
        {
            Mote mote = MoteMaker.MakeStaticMote(pawn.DrawPos, pawn.Map, DefOfRefs.NingshaRace_Mote_BurialMountainGuardBurst, Props.burstScale, true);
            Mote_BurialMountainGuardBurst burstMote = mote as Mote_BurialMountainGuardBurst;
            if (burstMote != null)
            {
                burstMote.Initialize(pawn.DrawPos);
            }
        }

        //函数职责：吸收伤害时生成短促土尘。
        private void SpawnAbsorbDust(Pawn pawn)
        {
            if (pawn.Spawned && pawn.Map != null && pawn.Position.ShouldSpawnMotesAt(pawn.Map))
            {
                FleckMaker.ThrowDustPuff(pawn.DrawPos, pawn.Map, 0.6f + ChargeRatio * 0.5f);
            }
        }

        //函数职责：释放爆发时生成环状土尘和灵能冲击提示。
        private void SpawnReleaseDust(Pawn pawn)
        {
            if (pawn.Map == null || !pawn.Position.ShouldSpawnMotesAt(pawn.Map))
            {
                return;
            }

            FleckMaker.Static(pawn.DrawPos, pawn.Map, FleckDefOf.PsycastAreaEffect, Props.releaseRadius);
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = Rand.InsideUnitCircle.normalized * Rand.Range(0.8f, Props.releaseRadius);
                Vector3 dustPos = pawn.DrawPos + new Vector3(offset.x, 0f, offset.y);
                FleckMaker.ThrowDustPuff(dustPos, pawn.Map, Rand.Range(0.8f, 1.4f));
            }
        }
    }
}
