using System;
using HarmonyLib;
using NingshaRaceLib.Petrification.Rendering;
using NingshaRaceLib.Petrification.Utility;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.Petrification.Patches
{
    //类职责：完全石化期间拦截 Pawn 的朝向写入，使石像保持进入状态时的朝向。
    [HarmonyPatch(typeof(Thing), nameof(Thing.Rotation), MethodType.Setter)]
    public static class Patch_PetrificationRotation
    {
        //函数职责：阻止完全石化 Pawn 改变朝向，并放行其他 Thing 的正常旋转。
        public static bool Prefix(Thing __instance)
        {
            Pawn pawn = __instance as Pawn;
            return pawn == null || !PetrificationUtility.IsFullyPetrified(pawn);
        }
    }

    //类职责：让所有经过原版可用性检查的攻击 Verb 在完全石化期间不可用。
    [HarmonyPatch(typeof(Verb), nameof(Verb.Available))]
    public static class Patch_PetrificationVerbAvailability
    {
        //函数职责：把完全石化 Pawn 持有的 Verb 可用性统一设为否。
        public static void Postfix(Verb __instance, ref bool __result)
        {
            if (PetrificationUtility.IsFullyPetrified(__instance?.CasterPawn))
            {
                __result = false;
            }
        }
    }

    //类职责：在攻击或能力 Verb 建立目标和蓄力姿态前拦截完全石化施法者。
    [HarmonyPatch(typeof(Verb), nameof(Verb.TryStartCastOn), new[]
    {
        typeof(LocalTargetInfo),
        typeof(LocalTargetInfo),
        typeof(bool),
        typeof(bool),
        typeof(bool),
        typeof(bool)
    })]
    public static class Patch_PetrificationVerbStart
    {
        //函数职责：拒绝完全石化 Pawn 发起近战、远程攻击或能力释放。
        public static bool Prefix(Verb __instance, ref bool __result)
        {
            if (!PetrificationUtility.IsFullyPetrified(__instance?.CasterPawn))
            {
                return true;
            }

            __result = false;
            return false;
        }
    }

    //类职责：在近战系统选择 Verb 前直接拒绝完全石化 Pawn 的攻击请求。
    [HarmonyPatch(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.TryMeleeAttack))]
    public static class Patch_PetrificationMeleeAttack
    {
        //函数职责：让完全石化 Pawn 的近战入口返回失败，避免攻击 Job 误判为已经完成一次攻击。
        public static bool Prefix(Pawn_MeleeVerbs __instance, ref bool __result)
        {
            if (!PetrificationUtility.IsFullyPetrified(__instance?.Pawn))
            {
                return true;
            }

            __result = false;
            return false;
        }
    }

    //类职责：完全石化期间阻止近战系统进入原版无可用 Verb 的报错分支。
    [HarmonyPatch(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.TryGetMeleeVerb))]
    public static class Patch_PetrificationGetMeleeVerb
    {
        //函数职责：完全石化 Pawn 查询近战动作时直接返回空，其他 Pawn 继续执行原版选择逻辑。
        public static bool Prefix(Pawn_MeleeVerbs __instance, ref Verb __result)
        {
            if (!PetrificationUtility.IsFullyPetrified(__instance?.Pawn))
            {
                return true;
            }

            __result = null;
            return false;
        }
    }

    //类职责：在 Verb 每 Tick 推进前终止完全石化 Pawn 已经开始的连发状态。
    [HarmonyPatch(typeof(Verb), nameof(Verb.VerbTick))]
    public static class Patch_PetrificationVerbTick
    {
        //函数职责：在保留原版特效清理 Tick 的同时把既有连发状态重置为空闲。
        public static void Prefix(Verb __instance)
        {
            if (__instance.state == VerbState.Bursting
                && PetrificationUtility.IsFullyPetrified(__instance.CasterPawn))
            {
                __instance.Reset();
            }
        }
    }

    //类职责：拦截任何绕过常规起手流程而直接请求下一发连射的完全石化 Verb。
    [HarmonyPatch(typeof(Verb), "TryCastNextBurstShot")]
    public static class Patch_PetrificationBurstShot
    {
        //函数职责：在射击结算前重置完全石化 Pawn 的 Verb 并跳过本次发射。
        public static bool Prefix(Verb __instance)
        {
            if (!PetrificationUtility.IsFullyPetrified(__instance?.CasterPawn))
            {
                return true;
            }

            __instance.Reset();
            return false;
        }
    }

    //类职责：完全石化期间拒绝 Pawn 开始新路径，防止野生动物漫游 Job 重新推动移动插值。
    [HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.StartPath))]
    public static class Patch_PetrificationStartPath
    {
        //函数职责：仅允许未完全石化的 Pawn 进入原版寻路流程。
        public static bool Prefix(Pawn ___pawn)
        {
            return !PetrificationUtility.IsFullyPetrified(___pawn);
        }
    }

    //类职责：完全石化期间暂停 JobTracker 的间隔推进，避免重新分配等待、漫游和战斗 Job。
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.JobTrackerTickInterval))]
    public static class Patch_PetrificationJobTrackerTickInterval
    {
        //函数职责：仅允许未完全石化的 Pawn 执行工作扫描和当前 Job 间隔逻辑。
        public static bool Prefix(Pawn ___pawn)
        {
            return !PetrificationUtility.IsFullyPetrified(___pawn);
        }
    }

    //类职责：完全石化期间拒绝外部系统直接向 Pawn 安装新的 Job。
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    public static class Patch_PetrificationStartJob
    {
        //函数职责：阻止完全石化 Pawn 获得等待、移动、攻击或玩家命令 Job。
        public static bool Prefix(Pawn ___pawn)
        {
            return !PetrificationUtility.IsFullyPetrified(___pawn);
        }
    }

    //类职责：完全石化期间把 Pawn 显示位置固定在当前格中心，排除所有动态视觉偏移。
    [HarmonyPatch(typeof(Pawn_DrawTracker), nameof(Pawn_DrawTracker.DrawPos), MethodType.Getter)]
    public static class Patch_PetrificationDrawPos
    {
        //函数职责：覆盖 Tween、碰撞避让、抖动、倾身、Job 和飞行造成的绘制偏移。
        public static void Postfix(Pawn_DrawTracker __instance, Pawn ___pawn, ref Vector3 __result)
        {
            if (PetrificationUtility.IsFullyPetrified(___pawn))
            {
                __result = ___pawn.Position.ToVector3ShiftedWithAltitude(
                    ___pawn.def.Altitude + __instance.SeededYOffset);
            }
        }
    }

    //类职责：完全石化期间拒绝设置新的渲染树动画，同时允许清空已有动画。
    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.SetAnimation))]
    public static class Patch_PetrificationAnimation
    {
        //函数职责：完全石化 Pawn 只允许传入空动画以完成状态清理。
        public static bool Prefix(AnimationDef animation, Pawn ___pawn)
        {
            return animation == null || !PetrificationUtility.IsFullyPetrified(___pawn);
        }
    }

    //类职责：按原版隐身的主线程预热流程缓存石化材质，并在并行预绘制时只读替换最终材质。
    public static class PetrificationRenderingPatchInstaller
    {
        //字段职责：同步首次补丁安装，防止重复向同一渲染方法追加后置处理。
        private static readonly object InstallLock = new object();

        //字段职责：阻止最终材质预热再次进入材质变体初始化后置处理。
        [ThreadStatic]
        private static bool prewarmingFinalizedMaterials;

        //字段职责：记录石化渲染动态补丁是否已经完成安装。
        private static bool installed;

        //函数职责：在确实需要显示石化材质时安装主线程预热与并行只读替换补丁。
        public static void EnsureInstalled()
        {
            if (installed)
            {
                return;
            }

            lock (InstallLock)
            {
                if (installed)
                {
                    return;
                }

                var initializeOriginal = AccessTools.Method(
                    typeof(PawnRenderNode),
                    "EnsureMaterialVariantsInitialized",
                    new[] { typeof(Graphic) });
                var initializePostfix = AccessTools.Method(
                    typeof(PetrificationRenderingPatchInstaller),
                    nameof(PostfixEnsureMaterialVariantsInitialized));
                var materialOriginal = AccessTools.Method(
                    typeof(PawnRenderNodeWorker),
                    nameof(PawnRenderNodeWorker.GetFinalizedMaterial));
                var materialPostfix = AccessTools.Method(
                    typeof(PetrificationRenderingPatchInstaller),
                    nameof(PostfixGetFinalizedMaterial));
                if (initializeOriginal == null
                    || initializePostfix == null
                    || materialOriginal == null
                    || materialPostfix == null)
                {
                    throw new MissingMethodException("无法定位 Pawn 石化材质补丁方法。");
                }

                Harmony harmony = new Harmony("chezhou.race.ningsharace.petrification.rendering");
                harmony.Patch(
                    initializeOriginal,
                    postfix: new HarmonyMethod(initializePostfix) { priority = Priority.Last });
                harmony.Patch(
                    materialOriginal,
                    postfix: new HarmonyMethod(materialPostfix) { priority = Priority.Last });
                installed = true;
            }
        }

        //函数职责：在原版为隐身材质预热的同一主线程阶段缓存石化图形的四向材质。
        public static void PostfixEnsureMaterialVariantsInitialized(PawnRenderNode __instance, Graphic g)
        {
            Pawn pawn = __instance?.tree?.pawn;
            if (!UnityData.IsInMainThread
                || prewarmingFinalizedMaterials
                || __instance?.Worker is PawnRenderNodeWorker_Carried
                || pawn == null
                || !PetrificationUtility.IsFullyPetrified(pawn))
            {
                return;
            }

            prewarmingFinalizedMaterials = true;
            try
            {
                PetrificationMaterialPool.PrewarmGraphic(g, pawn);
                PetrificationMaterialPool.PrewarmFinalizedMaterials(__instance, pawn);
            }
            finally
            {
                prewarmingFinalizedMaterials = false;
            }
        }

        //函数职责：完全石化期间替换身体、头部、服装和 HAR BodyAddon 的最终材质。
        public static void PostfixGetFinalizedMaterial(
            PawnRenderNodeWorker __instance,
            PawnDrawParms parms,
            ref Material __result)
        {
            if (!(__instance is PawnRenderNodeWorker_Carried)
                && !ReferenceEquals(__result, null)
                && PetrificationUtility.IsFullyPetrified(parms.pawn))
            {
                __result = UnityData.IsInMainThread
                    ? PetrificationMaterialPool.GetOrCreatePetrifiedMaterial(__result)
                    : PetrificationMaterialPool.GetPetrifiedMaterial(__result);
            }
        }
    }
}
