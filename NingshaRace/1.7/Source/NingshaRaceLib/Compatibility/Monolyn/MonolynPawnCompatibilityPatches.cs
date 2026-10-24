using System;
using System.Reflection;
using HarmonyLib;
using NingshaRaceLib.SandGolem.Utility;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Compatibility.Monolyn
{
    //类职责：集中提供 Monolyn 可选兼容补丁的启用判定与反射目标定位。
    internal static class MonolynCompatibilityUtility
    {
        //字段职责：标识 Monolyn 的包 ID，避免兼容补丁在目标模组未启用时参与初始化。
        internal const string PackageId = "ASEL.MonolynRace";

        //函数职责：判断当前加载列表是否包含 Monolyn。
        internal static bool IsActive()
        {
            return ModsConfig.IsActive(PackageId);
        }

        //函数职责：按完整类型名和方法签名定位 Monolyn 内部方法。
        internal static MethodBase FindMethod(string typeName, string methodName, Type[] argumentTypes = null)
        {
            Type targetType = AccessTools.TypeByName(typeName);
            return targetType == null ? null : AccessTools.Method(targetType, methodName, argumentTypes);
        }
    }

    //类职责：阻止 Monolyn 在游戏组件尚未建立时响应全局 Pawn.SetFaction 回调。
    [HarmonyPatch]
    internal static class Patch_Monolyn_SetFactionPostfix
    {
        //函数职责：仅在 Monolyn 实际启用时注册兼容补丁。
        private static bool Prepare()
        {
            return MonolynCompatibilityUtility.IsActive();
        }

        //函数职责：定位 Monolyn 注册到 Pawn.SetFaction 的全局后置回调。
        private static MethodBase TargetMethod()
        {
            return MonolynCompatibilityUtility.FindMethod(
                "ASEL.HarmonyPatches",
                "SetFaction_Postfix",
                new[] { typeof(Pawn) });
        }

        //函数职责：仅在当前游戏和地图运行状态就绪后允许 Monolyn 刷新亲族关系。
        private static bool Prefix()
        {
            return Current.Game != null && Current.ProgramState != ProgramState.Entry;
        }
    }

    //类职责：阻止 Monolyn 在地图与 Pawn 组件尚未稳定时遍历并访问未初始化的基因追踪器。
    [HarmonyPatch]
    internal static class Patch_Monolyn_UpdateKin
    {
        //函数职责：仅在 Monolyn 实际启用时注册兼容补丁。
        private static bool Prepare()
        {
            return MonolynCompatibilityUtility.IsActive();
        }

        //函数职责：定位 Monolyn 对所有 SetFaction 调用触发的亲族关系刷新方法。
        private static MethodBase TargetMethod()
        {
            return MonolynCompatibilityUtility.FindMethod("ASEL.ASELComponent", "UpdateKin");
        }

        //函数职责：地图生成期间发现游戏状态或殖民者基因组件未就绪时延后本次全局刷新。
        private static bool Prefix()
        {
            if (Current.Game == null || Current.ProgramState == ProgramState.Entry)
            {
                return false;
            }

            foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonists)
            {
                if (pawn == null || pawn.genes == null)
                {
                    return false;
                }
            }

            return true;
        }
    }

    //类职责：让 Monolyn 的 Pawn.Kill 前置回调忽略由凝砂族接管死亡流程的沙傀。
    [HarmonyPatch]
    internal static class Patch_Monolyn_PawnKillPrefix
    {
        //函数职责：仅在 Monolyn 实际启用时注册兼容补丁。
        private static bool Prepare()
        {
            return MonolynCompatibilityUtility.IsActive();
        }

        //函数职责：定位 Monolyn 的 Pawn.Kill 前置回调。
        private static MethodBase TargetMethod()
        {
            return MonolynCompatibilityUtility.FindMethod(
                "ASEL.HarmonyPatches+Pawn_Kill_Patch",
                "Prefix",
                new[] { typeof(Pawn) });
        }

        //函数职责：沙傀消散时跳过 Monolyn 的死亡状态采集，并保持其回调原本的放行结果。
        private static bool Prefix(Pawn __0, ref bool __result)
        {
            if (!SandGolemUtility.IsSandGolem(__0))
            {
                return true;
            }

            __result = true;
            return false;
        }
    }

    //类职责：让 Monolyn 的 Pawn.Kill 后置回调忽略没有生成尸体的沙傀消散流程。
    [HarmonyPatch]
    internal static class Patch_Monolyn_PawnKillPostfix
    {
        //函数职责：仅在 Monolyn 实际启用时注册兼容补丁。
        private static bool Prepare()
        {
            return MonolynCompatibilityUtility.IsActive();
        }

        //函数职责：定位 Monolyn 的 Pawn.Kill 后置回调。
        private static MethodBase TargetMethod()
        {
            return MonolynCompatibilityUtility.FindMethod(
                "ASEL.HarmonyPatches+Pawn_Kill_Patch",
                "Postfix");
        }

        //函数职责：沙傀没有进入原版死亡流程时阻止 Monolyn 读取尸体和跨 Pawn 死亡状态。
        private static bool Prefix(Pawn __0)
        {
            return !SandGolemUtility.IsSandGolem(__0);
        }
    }
}
