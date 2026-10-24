using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //原版人形或实体持械节点都通过同一个入口进入框架。
    [HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAndApparelExtras))]
    internal static class Patch_MeleeWeaponContext
    {
        //嵌套的分层武器保留自己的绘制委托，普通武器使用默认画布。
        private static void Prefix(Pawn pawn, Vector3 drawPos, Rot4 facing, PawnRenderFlags flags, out MeleeWeaponDraws.Context __state)
        {
            __state = MeleeWeaponDraws.Current;
            if (__state.Owner?.Pawn == pawn) return;
            CompMeleeAnimation owner = pawn.TryGetComp<CompMeleeAnimation>();
            MeleeWeaponDraws.Current = new MeleeWeaponDraws.Context
            {
                Owner = owner, Blade = Matrix4x4.identity,
                Parms = new PawnDrawParms { pawn = pawn, facing = facing, flags = flags, tint = Color.white,
                    matrix = Matrix4x4.Translate(drawPos), posture = PawnPosture.Standing }
            };
        }
        //异常也恢复调用上下文，但不吞掉原始异常。
        private static void Finalizer(MeleeWeaponDraws.Context __state) { MeleeWeaponDraws.Current = __state; }
    }

    //目标死亡或消失不会在收刀中途改变武器朝向。
    [HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAiming))]
    internal static class Patch_MeleeAimAngle
    {
        //只在播放阶段覆盖角度，闲置时沿用原版朝向。
        private static void Prefix(Thing eq, ref float aimAngle)
        {
            CompMeleeAnimation owner = MeleeWeaponDraws.Current.Owner;
            if (owner != null && owner.Accepts(eq) && owner.Current.Active) aimAngle = owner.Current.AimAngle;
        }
    }

    //在最终节点矩阵上统一施加身体姿态，兼容原版或自定义的身体、头部与附加节点。
    [HarmonyPatch(typeof(PawnRenderTree), nameof(PawnRenderTree.TryGetMatrix))]
    internal static class Patch_MeleeBodyPose
    {
        //准备阶段只读动作时钟，不创建资源或推进状态。
        private static void Postfix(PawnDrawParms parms, ref Matrix4x4 matrix, bool __result)
        {
            if (!__result || parms.posture != PawnPosture.Standing) return;
            CompMeleeAnimation owner = parms.pawn.TryGetComp<CompMeleeAnimation>();
            if (owner == null || !owner.Current.Active || owner.Current.Facing != parms.facing) return;
            MeleeAnimationPose pose = owner.Current.Pose;
            Vector2 configured = owner.RenderDefinition.bodyPivot;
            Vector3 pivot = parms.matrix.MultiplyPoint3x4(new Vector3(configured.x, 0, configured.y));
            matrix = Matrix4x4.Translate(pivot + new Vector3(pose.Body.x, 0, pose.Body.y))
                * Matrix4x4.Rotate(Quaternion.AngleAxis(pose.Lean, Vector3.up)) * Matrix4x4.Translate(-pivot) * matrix;
        }
    }

    //人形单位缩放到远景时也必须逐帧绘制正在播放的动作。
    [HarmonyPatch(typeof(PawnRenderer), "ParallelGetPreRenderResults")]
    internal static class Patch_MeleeAnimationCache
    {
        //只禁用活动动画的图集缓存，不为其他 Pawn 增加重绘。
        private static void Prefix(Pawn ___pawn, ref bool disableCache)
        {
            CompMeleeAnimation owner = ___pawn.TryGetComp<CompMeleeAnimation>();
            if (owner != null && (owner.Current.Active || owner.CapturePreview)) disableCache = true;
        }
    }
}
