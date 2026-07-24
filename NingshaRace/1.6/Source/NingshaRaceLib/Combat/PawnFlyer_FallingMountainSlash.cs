using System.Collections.Generic;
using NingshaRaceLib.Rendering;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Combat
{
    //类职责：驱动坠岳斩飞行、武器挥砍、落地冲击和跟随式月牙刀光。
    public class PawnFlyer_FallingMountainSlash : PawnFlyer
    {
        private const float AttackProgress = 0.55f;
        private const float LandingImpactRadius = 2.9f;
        private const float LandingAreaDamage = 20f;
        private const float LockedTargetDamage = 40f;
        private const float LandingArmorPenetration = 1f;
        private const float LandingScreenShake = 0.45f;

        private bool attackTriggered;
        private bool landingImpactTriggered;
        private Vector3 lockedDestination;
        private Pawn lockedTargetPawn;

        //函数职责：记录施放瞬间的目标坐标和锁定目标，供刀光与落地冲击使用。
        public void InitializeLandingData(Vector3 destination, Pawn targetPawn)
        {
            lockedDestination = destination;
            lockedTargetPawn = targetPawn;
        }

        //函数职责：保存刀光和落地冲击触发状态，避免读档后重复结算。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref attackTriggered, "attackTriggered", false);
            Scribe_Values.Look(ref landingImpactTriggered, "landingImpactTriggered", false);
            Scribe_Values.Look(ref lockedDestination, "lockedDestination");
            Scribe_References.Look(ref lockedTargetPawn, "lockedTargetPawn");
        }

        //函数职责：推进原版飞行，并在指定进度触发一次挥砍刀光。
        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (Destroyed || FlyingPawn == null || DestinationPos == startVec)
            {
                return;
            }

            Vector3 flightDirection = DestinationPos - startVec;
            flightDirection.y = 0f;
            if (flightDirection.sqrMagnitude > 0.001f)
            {
                FlyingPawn.Rotation = Rot4.FromAngleFlat(flightDirection.AngleFlat());
            }

            float progress = Mathf.Clamp01((float)ticksFlying / Mathf.Max(ticksFlightTime, 1));
            if (!attackTriggered && progress >= AttackProgress)
            {
                attackTriggered = true;
                SpawnSlashMote(flightDirection);
            }
        }

        //函数职责：在当前飞行位置生成会跟随本 Flyer 的土元素月牙刀光。
        private void SpawnSlashMote(Vector3 flightDirection)
        {
            if (Map == null || !DrawPos.ShouldSpawnMotesAt(Map))
            {
                return;
            }

            Mote_TerraCrescentSlash mote = ThingMaker.MakeThing(DefOfRefs.NingshaRace_Mote_TerraCrescentSlash)
                as Mote_TerraCrescentSlash;
            if (mote == null)
            {
                Log.Error("无法创建坠岳斩土元素月牙刀光。");
                return;
            }

            Vector3 normalizedDirection = NormalizeHorizontal(flightDirection);
            float rotation = normalizedDirection.AngleFlat() - 90f;
            mote.Initialize(this, lockedDestination, rotation);
            GenSpawn.Spawn(mote, DrawPos.ToIntVec3(), Map);
        }

        //函数职责：在落地前触发伤害、自定义地裂和震屏，再交还原版流程放回 Pawn。
        protected override void RespawnPawn()
        {
            TriggerLandingImpact();
            base.RespawnPawn();
        }

        //函数职责：确保落地冲击只结算一次，并按当前落点执行完整落地效果。
        private void TriggerLandingImpact()
        {
            Pawn casterPawn = FlyingPawn;
            Map map = Map;
            if (casterPawn == null || map == null)
            {
                return;
            }

            if (landingImpactTriggered)
            {
                return;
            }

            landingImpactTriggered = true;
            IntVec3 landingCell = DestinationPos.ToIntVec3();
            ApplyLockedTargetDamage(casterPawn, map, landingCell);
            ApplyLandingAreaDamage(casterPawn, map, landingCell);
            SpawnLandingCrack(map, landingCell);
            ShakeCurrentMapCamera(map);
        }

        //函数职责：对仍在落点范围内的锁定目标施加额外重击伤害。
        private void ApplyLockedTargetDamage(Pawn casterPawn, Map map, IntVec3 landingCell)
        {
            if (lockedTargetPawn == null
                || lockedTargetPawn == casterPawn
                || lockedTargetPawn.Destroyed
                || !lockedTargetPawn.Spawned
                || lockedTargetPawn.Dead
                || lockedTargetPawn.Map != map
                || lockedTargetPawn.Position.DistanceTo(landingCell) > LandingImpactRadius)
            {
                return;
            }

            lockedTargetPawn.TakeDamage(CreateLandingDamageInfo(casterPawn, lockedTargetPawn, landingCell, LockedTargetDamage));
        }

        //函数职责：收集落点范围内所有非施术者 Pawn，并施加一次范围切割伤害。
        private void ApplyLandingAreaDamage(Pawn casterPawn, Map map, IntVec3 landingCell)
        {
            HashSet<Pawn> damagedPawns = new HashSet<Pawn>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(landingCell, LandingImpactRadius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int index = things.Count - 1; index >= 0; index--)
                {
                    Pawn targetPawn = things[index] as Pawn;
                    if (targetPawn == null
                        || targetPawn == casterPawn
                        || targetPawn.Destroyed
                        || targetPawn.Dead
                        || damagedPawns.Contains(targetPawn))
                    {
                        continue;
                    }

                    targetPawn.TakeDamage(CreateLandingDamageInfo(casterPawn, targetPawn, landingCell, LandingAreaDamage));
                    damagedPawns.Add(targetPawn);
                }
            }
        }

        //函数职责：生成带来源、武器和落点方向的坠岳斩落地伤害信息。
        private static DamageInfo CreateLandingDamageInfo(Pawn casterPawn, Thing target, IntVec3 landingCell, float damageAmount)
        {
            DamageInfo damageInfo = new DamageInfo(
                DamageDefOf.Cut,
                damageAmount,
                LandingArmorPenetration,
                -1f,
                casterPawn,
                null,
                casterPawn.equipment?.Primary?.def);
            damageInfo.SetAngle((target.Position - landingCell).ToVector3());
            return damageInfo;
        }

        //函数职责：在坠岳斩落点生成一次自定义地裂 Mote。
        private static void SpawnLandingCrack(Map map, IntVec3 landingCell)
        {
            MoteMaker.MakeStaticMote(
                landingCell.ToVector3Shifted(),
                map,
                DefOfRefs.NingshaRace_Mote_FallingMountainGroundCrack);
        }

        //函数职责：只在玩家当前查看同一地图时触发一次屏幕震动。
        private static void ShakeCurrentMapCamera(Map map)
        {
            if (Find.CurrentMap == map)
            {
                Find.CameraDriver?.shaker?.DoShake(LandingScreenShake);
            }
        }

        //函数职责：绘制飞行阴影后，按飞行进度额外绘制蓄力和下劈中的主武器。
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            Pawn flyingPawn = FlyingPawn;
            ThingWithComps weapon = flyingPawn?.equipment?.Primary;
            if (weapon == null)
            {
                return;
            }

            Vector3 weaponScale = Vector3.one;
            if (weapon.def.graphicData != null)
            {
                weaponScale = new Vector3(
                    weapon.def.graphicData.drawSize.x,
                    1f,
                    weapon.def.graphicData.drawSize.y);
            }

            float progress = Mathf.Clamp01((float)ticksFlying / Mathf.Max(ticksFlightTime, 1));
            Vector3 flightDirection = DestinationPos - startVec;
            flightDirection.y = 0f;
            float flightAngle = flightDirection.AngleFlat();
            float swingOffset;
            if (progress < AttackProgress)
            {
                float windupProgress = progress / AttackProgress;
                swingOffset = Mathf.Lerp(-45f, -90f, Mathf.SmoothStep(0f, 1f, windupProgress));
            }
            else
            {
                float impactProgress = (progress - AttackProgress) / (1f - AttackProgress);
                swingOffset = Mathf.Lerp(-90f, 70f, impactProgress * impactProgress);
            }

            bool flyingLeft = flightAngle > 200f && flightAngle < 340f;
            Mesh mesh = flyingLeft ? MeshPool.plane10Flip : MeshPool.plane10;
            float finalWeaponAngle = flyingLeft
                ? flightAngle - swingOffset + 45f
                : flightAngle + swingOffset - 45f;
            Vector3 handleLocalPosition = flyingLeft
                ? new Vector3(0.5f, 0f, -0.5f)
                : new Vector3(-0.5f, 0f, -0.5f);
            Vector3 handWorldPosition = DrawPos;
            handWorldPosition.y += 0.04f;
            handWorldPosition += Vector3Utility.FromAngleFlat(flightAngle) * Mathf.Lerp(0f, 0.3f, progress);

            Quaternion rotation = Quaternion.AngleAxis(finalWeaponAngle % 360f, Vector3.up);
            Vector3 handleOffset = rotation * Vector3.Scale(handleLocalPosition, weaponScale);
            Matrix4x4 matrix = Matrix4x4.TRS(handWorldPosition - handleOffset, rotation, weaponScale);
            Graphics.DrawMesh(mesh, matrix, weapon.Graphic.MatSingle, 0);
        }

        //函数职责：把飞行向量转换为稳定的水平单位方向。
        private static Vector3 NormalizeHorizontal(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                return Vector3.forward;
            }

            return direction.normalized;
        }
    }
}
