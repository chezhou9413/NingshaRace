using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Combat.SnakeBellySword.Patches
{
    //蛇腹剑的战斗任务显示并使用近战技能。
    [HarmonyPatch(typeof(Toils_Combat), nameof(Toils_Combat.GetActiveSkillForToil))]
    internal static class SnakeBellySwordActiveSkillPatch
    {
        //原版定点攻击未记录动词时，从实际装备识别蛇腹剑。
        private static void Postfix(Toil toil, ref SkillDef __result)
        {
            Job job = toil.actor.CurJob;
            ThingWithComps weapon = job.verbToUse?.EquipmentSource;
            if (weapon == null && job.def == JobDefOf.AttackStatic)
                weapon = toil.actor.equipment?.Primary;
            if (weapon?.def == DefOfRefs.NingshaRace_SnakeBellySword)
                __result = SkillDefOf.Melee;
        }
    }
}
