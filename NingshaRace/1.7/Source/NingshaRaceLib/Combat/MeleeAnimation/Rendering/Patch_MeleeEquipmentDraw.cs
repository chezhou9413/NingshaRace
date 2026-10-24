using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //仅替换原版装备函数的最终绘制调用，不复制持械坐标、角度或攻击状态判断。
    [HarmonyPatch]
    internal static class Patch_MeleeEquipmentDraw
    {
        //CE 已替换原版末尾调用，改在其内部接收完成后坐力与偏移计算的矩阵。
        private static MethodBase TargetMethod()
        {
            var ce = AccessTools.TypeByName("CombatExtended.HarmonyCE.Harmony_PawnRenderer_DrawEquipmentAiming");
            return ce == null
                ? AccessTools.Method(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAiming))
                : AccessTools.Method(ce, "DrawMesh", new[] { typeof(Mesh), typeof(Matrix4x4),
                    typeof(Material), typeof(int), typeof(Thing), typeof(Vector3), typeof(float) })
                    ?? throw new MissingMethodException(ce.FullName, "DrawMesh");
        }

        //未接入框架的装备由包装函数原样提交；原版调用变化时明确报错。
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
        {
            var original = AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawMesh),
                new[] { typeof(Mesh), typeof(Matrix4x4), typeof(Material), typeof(int) });
            var replacement = AccessTools.Method(typeof(MeleeWeaponDraws), nameof(MeleeWeaponDraws.Draw));
            int equipmentArgument = originalMethod.DeclaringType == typeof(PawnRenderUtility) ? 0 : 4;
            int replaced = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                if (!instruction.Calls(original)) { yield return instruction; continue; }
                var loadEquipment = new CodeInstruction(OpCodes.Ldarg, equipmentArgument);
                loadEquipment.MoveLabelsFrom(instruction);
                loadEquipment.MoveBlocksFrom(instruction);
                yield return loadEquipment;
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                yield return instruction;
                replaced++;
            }
            if (replaced != 1)
                throw new InvalidOperationException("近战动画持械接入失败：原版装备绘制调用数量为 " + replaced + "，预期为 1。");
        }
    }
}
