using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.SandGolem.Defs;
using NingshaRaceLib.SandGolem.Health;
using NingshaRaceLib.SandGolem.Lifecycle;
using NingshaRaceLib.SandGolem.Rendering;
using NingshaRaceLib.SandGolem.Tracking;

namespace NingshaRaceLib.SandGolem.Utility
{
    //类职责：提供沙傀系统共享的身份判断、目标校验和组件维护方法。
    public static class SandGolemUtility
    {
        //字段职责：缓存 Pawn_NeedsTracker 内部主需求列表字段。
        private static readonly FieldInfo NeedsField = typeof(Pawn_NeedsTracker).GetField("needs", BindingFlags.Instance | BindingFlags.NonPublic);

        //字段职责：缓存 Pawn_NeedsTracker 内部杂项需求列表字段。
        private static readonly FieldInfo NeedsMiscField = typeof(Pawn_NeedsTracker).GetField("needsMisc", BindingFlags.Instance | BindingFlags.NonPublic);

        //属性职责：返回沙傀 ThingDef 上配置的汇聚和消散动画持续 Tick。
        public static int AnimationTicks => Settings.animationTicks;

        //属性职责：返回沙傀 ThingDef 上配置的低频身份维护间隔。
        public static int MaintenanceIntervalTicks => Settings.maintenanceIntervalTicks;

        //属性职责：返回沙傀 ThingDef 上声明的生命周期配置。
        private static SandGolemDefExtension Settings =>
            DefOfRefs.NingshaRace_SandGolem.GetModExtension<SandGolemDefExtension>();

        //函数职责：判断 Pawn 是否是凝砂族主种族。
        public static bool IsNingshaPawn(Pawn pawn)
        {
            return pawn?.def == DefOfRefs.NingshaRace;
        }

        //函数职责：判断 Pawn 是否是玩家阵营凝砂族。
        public static bool IsPlayerNingshaPawn(Pawn pawn)
        {
            return IsNingshaPawn(pawn) && pawn.Faction == Faction.OfPlayer;
        }

        //函数职责：判断 Pawn 是否带有沙傀标记状态。
        public static bool IsSandGolem(Pawn pawn)
        {
            return pawn?.def == DefOfRefs.NingshaRace_SandGolem || pawn?.health?.hediffSet?.HasHediff(DefOfRefs.NingshaRace_SandGolemMarker) == true;
        }

        //函数职责：判断沙傀当前是否处于不可移动的动画阶段。
        public static bool IsMovementLockedSandGolem(Pawn pawn)
        {
            if (!IsSandGolem(pawn))
            {
                return false;
            }

            GameComponent_SandGolemTracker tracker = GameComponent_SandGolemTracker.Current;
            return tracker != null && tracker.TryGetState(pawn, out SandGolemRenderState state) && state.LocksFacingAndMovement();
        }

        //函数职责：启用或解除原版寻路器自带的移动禁用开关。
        public static void SetMovementDisabled(Pawn pawn, bool disabled)
        {
            if (pawn?.pather == null)
            {
                return;
            }

            if (disabled)
            {
                pawn.pather.StopDead();
            }

            pawn.pather.debugDisabled = disabled;
        }

        //函数职责：在汇聚动画结束后恢复沙傀的寻路、姿态和工作扫描。
        public static void RestoreControlAfterMovementLock(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed)
            {
                return;
            }

            SetMovementDisabled(pawn, false);
            pawn.stances?.CancelBusyStanceHard();
            if (pawn.Spawned)
            {
                pawn.Map.pawnDestinationReservationManager.ReleaseAllClaimedBy(pawn);
            }

            if (pawn.jobs?.curJob != null)
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                return;
            }

            pawn.jobs?.ClearQueuedJobs();
            pawn.jobs?.CheckForJobOverride();
        }

        //函数职责：在动画阶段固定沙傀朝南并停止实际寻路移动。
        public static void LockFacingAndMovement(Pawn pawn, bool stopJobs)
        {
            if (pawn == null || pawn.Destroyed)
            {
                return;
            }

            pawn.Rotation = Rot4.South;
            SetMovementDisabled(pawn, true);
            if (stopJobs)
            {
                pawn.jobs?.StopAll();
            }
        }

        //函数职责：判断地格是否是可召唤沙傀的沙地。
        public static bool IsValidSandCell(IntVec3 cell, Map map, out string rejectReason)
        {
            rejectReason = null;
            if (map == null || !cell.InBounds(map))
            {
                rejectReason = "目标位置无效";
                return false;
            }

            TerrainDef terrain = cell.GetTerrain(map);
            if (terrain != TerrainDefOf.Sand && terrain != TerrainDefOf.SoftSand)
            {
                rejectReason = "需要选择沙地";
                return false;
            }

            if (!cell.Standable(map) || cell.Filled(map))
            {
                rejectReason = "目标位置不可站立";
                return false;
            }

            if (cell.GetFirstPawn(map) != null)
            {
                rejectReason = "目标位置已有 Pawn";
                return false;
            }

            return true;
        }

        //函数职责：清理沙傀不应保留的需求、关系和疲劳状态。
        public static void StripNeedsAndRelations(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            pawn.relations?.ClearAllRelations();
            if (pawn.needs == null)
            {
                pawn.needs = new Pawn_NeedsTracker(pawn);
            }

            ClearNeedListIfNeeded(pawn.needs, NeedsField);
            ClearNeedListIfNeeded(pawn.needs, NeedsMiscField);
            pawn.needs.mood = null;
            pawn.needs.food = null;
            pawn.needs.rest = null;
            pawn.needs.joy = null;
            pawn.needs.beauty = null;
            pawn.needs.comfort = null;
            pawn.needs.drugsDesire = null;
            pawn.needs.outdoors = null;
            pawn.needs.indoors = null;
            pawn.needs.roomsize = null;
            pawn.needs.learning = null;
            pawn.needs.play = null;
            pawn.needs.energy = null;
            pawn.mindState?.mentalStateHandler?.Reset();
        }

        //函数职责：低频维护沙傀无需求和无关系状态，避免每 Tick 反射分配。
        public static void MaintainIdentity(Pawn pawn, int tick)
        {
            if (pawn == null || tick % MaintenanceIntervalTicks != pawn.thingIDNumber % MaintenanceIntervalTicks)
            {
                return;
            }

            StripNeedsAndRelations(pawn);
            EnsurePlayerControlComponents(pawn);
        }

        //函数职责：仅在需求列表存在内容时才替换为空列表，避免持续分配。
        private static void ClearNeedListIfNeeded(Pawn_NeedsTracker needs, FieldInfo field)
        {
            if (needs == null || field == null)
            {
                return;
            }

            if (!(field.GetValue(needs) is List<Need> list) || list.Count == 0)
            {
                return;
            }

            field.SetValue(needs, new List<Need>());
        }

        //函数职责：确保玩家沙傀保留可选中、可工作和可征召所需的玩家组件。
        public static void EnsurePlayerControlComponents(Pawn pawn, bool actAsIfSpawned = false, Pawn skillSource = null)
        {
            if (pawn == null || pawn.Destroyed)
            {
                return;
            }

            if (IsSandGolem(pawn))
            {
                EnsureSandGolemStoryTracker(pawn);
                if (pawn.guest == null)
                {
                    pawn.guest = new Pawn_GuestTracker(pawn);
                }

                if (pawn.skills == null)
                {
                    pawn.skills = new Pawn_SkillTracker(pawn);
                    SetSandGolemSkillLevels(pawn, skillSource);
                }
            }

            if (pawn.workSettings == null && (pawn.RaceProps.Humanlike || IsSandGolem(pawn)))
            {
                pawn.workSettings = new Pawn_WorkSettings(pawn);
                pawn.workSettings.EnableAndInitialize();
            }

            if (pawn.playerSettings == null)
            {
                pawn.playerSettings = new Pawn_PlayerSettings(pawn);
            }

            if ((pawn.Spawned || actAsIfSpawned) && pawn.drafter == null)
            {
                pawn.drafter = new Pawn_DraftController(pawn);
            }
        }

        //函数职责：给沙傀设置用于工作列表排序和提示的技能等级。
        private static void SetSandGolemSkillLevels(Pawn pawn, Pawn source)
        {
            if (pawn?.skills?.skills == null)
            {
                return;
            }

            for (int i = 0; i < pawn.skills.skills.Count; i++)
            {
                SkillRecord skill = pawn.skills.skills[i];
                if (skill == null)
                {
                    continue;
                }

                SkillRecord sourceSkill = source?.skills?.GetSkill(skill.def);
                if (sourceSkill != null)
                {
                    skill.Level = sourceSkill.Level;
                    skill.passion = sourceSkill.passion;
                }
                else
                {
                    skill.Level = pawn.RaceProps.mechFixedSkillLevel > 0 ? pawn.RaceProps.mechFixedSkillLevel : 6;
                    skill.passion = Passion.None;
                }

                skill.xpSinceLastLevel = 0f;
                skill.xpSinceMidnight = 0f;
            }
        }

        //函数职责：确保沙傀的技能系统拥有原版 Tick 所需的空故事和特质容器。
        private static void EnsureSandGolemStoryTracker(Pawn pawn)
        {
            if (pawn.story == null)
            {
                pawn.story = new Pawn_StoryTracker(pawn);
            }

            if (pawn.story.traits == null)
            {
                pawn.story.traits = new TraitSet(pawn);
            }
        }
    }
}
