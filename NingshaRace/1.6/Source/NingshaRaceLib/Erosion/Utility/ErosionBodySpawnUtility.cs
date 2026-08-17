using System;
using RimWorld;
using RimWorld.Planet;
using Verse;

using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Erosion.Utility
{
    //类职责：集中完成侵蚀体原身生成、原版Mutant转化、实体阵营分配和地图落点。
    public static class ErosionBodySpawnUtility
    {
        //函数职责：生成一名穿戴PawnKind凝砂服装、无武器和轻量背景数据的未落地侵蚀体。
        public static Pawn Generate(PawnKindDef pawnKind = null, Faction faction = null, PlanetTile? tile = null)
        {
            if (!ModsConfig.AnomalyActive)
            {
                throw new InvalidOperationException("生成侵蚀体需要启用异象 DLC。");
            }

            PawnKindDef sourceKind = pawnKind ?? DefOfRefs.NingshaRace_Colonist;
            if (sourceKind.race != DefOfRefs.NingshaRace)
            {
                throw new InvalidOperationException("侵蚀体原身必须是凝砂族 PawnKind: " + sourceKind.defName);
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
                forceNoGear: false);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            TurnIntoErosionBody(pawn, faction);
            return pawn;
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

        //函数职责：把现有凝砂族永久转化为侵蚀体并分配目标实体阵营。
        public static void TurnIntoErosionBody(Pawn pawn, Faction faction = null)
        {
            if (pawn == null)
            {
                throw new ArgumentNullException(nameof(pawn));
            }
            if (pawn.def != DefOfRefs.NingshaRace)
            {
                throw new InvalidOperationException("只能把凝砂族转化为侵蚀体: " + pawn);
            }
            if (pawn.IsMutant)
            {
                throw new InvalidOperationException("不能重复转化已经是 Mutant 的 Pawn: " + pawn);
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
