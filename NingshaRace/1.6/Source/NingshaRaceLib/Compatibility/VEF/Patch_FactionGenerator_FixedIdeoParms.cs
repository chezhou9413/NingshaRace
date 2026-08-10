using HarmonyLib;
using RimWorld;
using RimWorld.Planet;

namespace NingshaRaceLib.Compatibility.VEF
{
    //类职责：恢复第三方派系生成器遗漏的固定意识形态参数，避免 Monolyn 固定派系生成非法意识形态。
    [HarmonyPatch(
        typeof(FactionGenerator),
        nameof(FactionGenerator.NewGeneratedFaction),
        new[] { typeof(PlanetLayer), typeof(FactionGeneratorParms) })]
    public static class Patch_FactionGenerator_FixedIdeoParms
    {
        //函数职责：仅在固定意识形态派系收到全空参数时，从 FactionDef 重建原版生成参数。
        public static void Prefix(ref FactionGeneratorParms parms)
        {
            FactionDef factionDef = parms.factionDef;
            IdeoGenerationParms ideoParms = parms.ideoGenerationParms;
            if (factionDef == null ||
                !factionDef.fixedIdeo ||
                !IsDefaultIdeoParms(ideoParms))
            {
                return;
            }

            parms.ideoGenerationParms = CreateFixedIdeoParms(factionDef);
        }

        //函数职责：确认调用方没有提供任何自定义意识形态约束，避免覆盖其他模组的显式生成参数。
        private static bool IsDefaultIdeoParms(IdeoGenerationParms parms)
        {
            return parms.forFaction == null &&
                !parms.forceNoExpansionIdeo &&
                !parms.classicExtra &&
                (parms.disallowedPrecepts == null || parms.disallowedPrecepts.Count == 0) &&
                (parms.disallowedMemes == null || parms.disallowedMemes.Count == 0) &&
                (parms.forcedMemes == null || parms.forcedMemes.Count == 0) &&
                !parms.forceNoWeaponPreference &&
                !parms.forNewFluidIdeo &&
                !parms.fixedIdeo &&
                string.IsNullOrEmpty(parms.name) &&
                (parms.styles == null || parms.styles.Count == 0) &&
                (parms.deities == null || parms.deities.Count == 0) &&
                !parms.hidden &&
                string.IsNullOrEmpty(parms.description) &&
                !parms.requiredPreceptsOnly;
        }

        //函数职责：按原版固定派系创建流程完整复制意识形态名称、模因、样式、神祇与描述。
        private static IdeoGenerationParms CreateFixedIdeoParms(FactionDef factionDef)
        {
            return new IdeoGenerationParms(
                factionDef,
                forceNoExpansionIdeo: false,
                disallowedPrecepts: null,
                disallowedMemes: null,
                forcedMemes: factionDef.forcedMemes,
                classicExtra: false,
                forceNoWeaponPreference: false,
                forNewFluidIdeo: false,
                fixedIdeo: true,
                name: factionDef.ideoName,
                styles: factionDef.styles,
                deities: factionDef.deityPresets,
                hidden: factionDef.hiddenIdeo,
                description: factionDef.ideoDescription,
                requiredPreceptsOnly: factionDef.requiredPreceptsOnly);
        }
    }
}
