using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.SandGolem.Health;
using NingshaRaceLib.SandGolem.Rendering;
using NingshaRaceLib.SandGolem.Tracking;
using NingshaRaceLib.SandGolem.Utility;

namespace NingshaRaceLib.SandGolem.Lifecycle
{
    //在聚拢动画结束后创建可控制的沙傀实体。
    public static class SandGolemFactory
    {
        //函数职责：在指定沙地生成施法者对应的沙傀。
        public static Pawn SpawnGolem(Pawn caster, Map map, IntVec3 cell)
        {
            PawnGenerationRequest request = new PawnGenerationRequest(
                DefOfRefs.NingshaRace_SandGolemKind,
                Faction.OfPlayer,
                PawnGenerationContext.NonPlayer,
                map.Tile,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                mustBeCapableOfViolence: false,
                allowPregnant: false,
                allowFood: false,
                allowAddictions: false,
                forceNoIdeo: true,
                forceNoBackstory: true,
                forbidAnyTitle: true,
                forceRecruitable: true,
                dontGiveWeapon: true,
                forceNoGear: true);

            Pawn golem = null;
            try
            {
                golem = SandGolemGenerationGate.GenerateForSummon(request);
                golem.Name = new NameSingle(caster.LabelShort + "的沙傀");
                GenSpawn.Spawn(golem, cell, map, Rot4.South);
                golem.Rotation = Rot4.South;

                MarkAsGolem(golem);
                SandGolemIdentityCleaner.Clean(golem);
                SandGolemUtility.StripNeedsAndRelations(golem);
                SandGolemUtility.EnsurePlayerControlComponents(golem, skillSource: caster);
                golem.Drawer?.renderer?.SetAllGraphicsDirty();
                return golem;
            }
            catch
            {
                if (golem != null && !golem.Destroyed)
                {
                    golem.Destroy(DestroyMode.Vanish);
                }
                throw;
            }
        }

        //函数职责：给 Pawn 添加沙傀标记状态。
        private static void MarkAsGolem(Pawn golem)
        {
            if (golem.health?.hediffSet?.HasHediff(DefOfRefs.NingshaRace_SandGolemMarker) != true)
            {
                golem.health?.AddHediff(DefOfRefs.NingshaRace_SandGolemMarker);
            }
        }

    }
}
