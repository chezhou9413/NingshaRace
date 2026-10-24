using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Erosion.Utility
{
    //生成各人形种族的侵蚀体，完成原版异变转化和实体阵营分配。
    public static class ErosionBodySpawnUtility
    {
        //生成保留原身服装、无武器的侵蚀体，未指定原身时仍使用凝砂族。
        public static Pawn Generate(PawnKindDef pawnKind = null, Faction faction = null, PlanetTile? tile = null)
        {
            if (!ModsConfig.AnomalyActive)
            {
                throw new InvalidOperationException("生成侵蚀体需要启用异象 DLC。");
            }

            PawnKindDef sourceKind = pawnKind ?? DefOfRefs.NingshaRace_Colonist;
            if (!sourceKind.RaceProps.Humanlike || sourceKind.mutant != null)
            {
                throw new InvalidOperationException("侵蚀体原身必须是未预设异变的人形 PawnKind: " + sourceKind.defName);
            }

            PawnGenerationRequest request = new PawnGenerationRequest(
                sourceKind,
                null,
                PawnGenerationContext.NonPlayer,
                tile,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                allowGay: false,
                allowPregnant: false,
                allowFood: false,
                allowAddictions: false,
                worldPawnFactionDoesntMatter: true,
                forceNoIdeo: true,
                forceNoBackstory: true,
                dontGiveWeapon: true,
                forceNoGear: false,
                //通过原版类型获取器固定任务种族，避免生成前缀改写 KindDef 后生成其他种族。
                pawnKindDefGetter: xenotype => sourceKind);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            if (pawn.def != sourceKind.race)
            {
                throw new InvalidOperationException("侵蚀体原身生成结果与指定种族不符，指定="
                    + sourceKind.race.defName + "，实际=" + pawn.def.defName);
            }
            TurnIntoErosionBody(pawn, faction);
            return pawn;
        }

        //先等概率选择非凝砂人形种族，再选择该种族允许调试生成的普通原身。
        public static PawnKindDef RandomNonNingshaKind()
        {
            var races = DefDatabase<PawnKindDef>.AllDefsListForReading
                .Where(kind => kind.showInDebugSpawner && kind.RaceProps.Humanlike
                    && kind.race != DefOfRefs.NingshaRace && kind.mutant == null)
                .GroupBy(kind => kind.race).ToList();
            return races.Count == 0 ? null : races.RandomElement().RandomElement();
        }

        //函数职责：在指定地图空格快速生成并放置一名侵蚀体。
        public static Pawn Spawn(Map map, IntVec3 cell, PawnKindDef pawnKind = null, Faction faction = null)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }
            if (!cell.InBounds(map) || !cell.Standable(map) || cell.GetFirstPawn(map) != null)
            {
                throw new InvalidOperationException("侵蚀体生成位置不可用: " + cell);
            }

            Pawn pawn = Generate(pawnKind, faction, map.Tile);
            GenSpawn.Spawn(pawn, cell, map, Rot4.Random);
            return pawn;
        }

        //说明不能转化的实际原因，供调试入口与转化流程共用。
        public static string ConversionRejectionReason(Pawn pawn)
        {
            if (!ModsConfig.AnomalyActive) return "转化为侵蚀体需要启用异象 DLC。";
            if (pawn == null) return "请选择一名人形角色。";
            if (pawn.Dead || pawn.Destroyed) return "只能转化存活的角色。";
            if (!pawn.RaceProps.Humanlike) return "只能把人形角色转化为侵蚀体。";
            if (ErosionPawnUtility.IsErosionBody(pawn)) return "该角色已经是侵蚀体。";
            if (pawn.IsMutant) return "该角色已有其他异变身份，不能直接转化为侵蚀体。";
            return null;
        }

        //把现有人形角色永久转化为侵蚀体并分配目标实体阵营。
        public static void TurnIntoErosionBody(Pawn pawn, Faction faction = null)
        {
            string rejection = ConversionRejectionReason(pawn);
            if (rejection != null)
            {
                throw new InvalidOperationException(rejection);
            }

            Faction targetFaction = faction ?? Faction.OfEntities;
            if (targetFaction == null)
            {
                throw new InvalidOperationException("当前游戏不存在实体阵营，无法生成侵蚀体。");
            }

            MutantUtility.SetPawnAsMutantInstantly(pawn, DefOfRefs.NingshaRace_ErosionBodyMutant);
            if (pawn.Faction != targetFaction)
            {
                pawn.SetFaction(targetFaction);
            }
        }
    }
}
