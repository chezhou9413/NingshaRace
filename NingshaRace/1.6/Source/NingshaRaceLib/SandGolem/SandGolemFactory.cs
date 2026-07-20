using NingshaRaceLib.Rendering;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.SandGolem
{
    //类职责：负责根据施法者生成真实沙傀 Pawn，并完成初始标记、低血量和渲染状态注册。
    public static class SandGolemFactory
    {
        //函数职责：在指定沙地生成施法者对应的沙傀。
        public static Pawn SpawnGolem(Pawn caster, IntVec3 cell)
        {
            if (caster == null || caster.Map == null)
            {
                return null;
            }

            Texture2D[] textures = SandGolemPawnCapture.CapturePawn(caster);
            PawnGenerationRequest request = new PawnGenerationRequest(
                DefOfRefs.NingshaRace_SandGolemKind,
                Faction.OfPlayer,
                PawnGenerationContext.NonPlayer,
                caster.Map.Tile,
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
                golem = PawnGenerator.GeneratePawn(request);
                golem.Name = new NameSingle(caster.LabelShort + "的沙傀");
                GenSpawn.Spawn(golem, cell, caster.Map, Rot4.South);
                golem.Rotation = Rot4.South;

                MarkAsGolem(golem);
                SandGolemIdentityCleaner.Clean(golem);
                SandGolemUtility.StripNeedsAndRelations(golem);
                SandGolemUtility.EnsurePlayerControlComponents(golem, skillSource: caster);
                GameComponent_SandGolemTracker.Current?.Register(caster, golem, textures);
                golem.Drawer?.renderer?.SetAllGraphicsDirty();
                return golem;
            }
            catch
            {
                DestroyCapturedTextures(textures);
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

        //函数职责：生成流程失败时释放尚未交给渲染状态托管的截图贴图。
        private static void DestroyCapturedTextures(Texture2D[] textures)
        {
            if (textures == null)
            {
                return;
            }

            for (int i = 0; i < textures.Length; i++)
            {
                if (textures[i] != null)
                {
                    Object.Destroy(textures[i]);
                }
            }
        }
    }
}
