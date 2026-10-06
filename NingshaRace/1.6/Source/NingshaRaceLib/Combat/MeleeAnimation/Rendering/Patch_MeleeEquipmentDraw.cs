using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //仅替换原版装备函数的最终绘制调用，不复制持械坐标、角度或攻击状态判断。
    [HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAiming))]
    internal static class Patch_MeleeEquipmentDraw
    {
        //未接入框架的装备由包装函数原样提交；原版调用变化时明确报错。
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var original = AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawMesh),
                new[] { typeof(Mesh), typeof(Matrix4x4), typeof(Material), typeof(int) });
            var replacement = AccessTools.Method(typeof(MeleeWeaponDraws), nameof(MeleeWeaponDraws.Draw));
            int replaced = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                if (!instruction.Calls(original)) { yield return instruction; continue; }
                var loadEquipment = new CodeInstruction(OpCodes.Ldarg_0);
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
