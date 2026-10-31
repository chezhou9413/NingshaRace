using LudeonTK;
using NingshaRaceLib.Core.Defs;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DevTools.Race
{
    //生成立即进入指定动作的凝砂族，供检查读书朝向和睡眠表现。
    public static class NingshaPawnStateDebugActions
    {
        //自带小说并原地向东阅读，跳过寻找书本和座位的准备步骤。
        [DebugAction("NingshaRace", "生成凝砂族并强制看书", actionType = DebugActionType.ToolMap,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnReadingPawn()
        {
            Pawn pawn = SpawnAtMouse();
            if (pawn == null) return;

            Book book = BookUtility.MakeBook(ThingDefOf.Novel, ArtGenerationContext.Outsider);
            GenSpawn.Spawn(book, pawn.Position, pawn.Map);
            if (pawn.carryTracker.TryStartCarry(book, 1, reserve: false) != 1)
            {
                Messages.Message("凝砂族未能拿起测试用小说，书本保留在生成位置。", pawn,
                    MessageTypeDefOf.RejectInput, false);
                return;
            }

            Job job = JobMaker.MakeJob(JobDefOf.Reading, book);
            job.playerForced = true;
            pawn.jobs.StartJob(job, JobCondition.InterruptForced, tag: JobTag.Misc,
                keepCarryingThingOverride: true);
        }

        //生成木制单人床，并让凝砂直接在床位上进入原版睡眠任务。
        [DebugAction("NingshaRace", "生成凝砂族并强制睡觉（床上）", actionType = DebugActionType.ToolMap,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnSleepingPawn()
        {
            Map map = Find.CurrentMap;
            IntVec3 cell = Verse.UI.MouseCell();
            if (map == null || !CanPlaceBed(map, cell))
            {
                Messages.Message("请点击能完整放下一张单人床、没有角色和障碍物的空地。",
                    MessageTypeDefOf.RejectInput, false);
                return;
            }

            Building_Bed bed = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
            bed.SetFaction(Faction.OfPlayer);
            GenSpawn.Spawn(bed, cell, map, Rot4.South);
            Pawn pawn = SpawnPawn(map, bed.GetSleepingSlotPos(0));

            pawn.needs.rest.CurLevelPercentage = 0.1f;
            Job job = JobMaker.MakeJob(JobDefOf.LayDown, bed);
            job.playerForced = true;
            job.forceSleep = true;
            pawn.jobs.StartJob(job, JobCondition.InterruptForced, tag: JobTag.Misc);
        }

        //检查完整床体占地，避免调试生成覆盖已有物体或人物。
        private static bool CanPlaceBed(Map map, IntVec3 center)
        {
            foreach (IntVec3 cell in GenAdj.OccupiedRect(center, Rot4.South, ThingDefOf.Bed.size))
            {
                if (!cell.InBounds(map) || !cell.Standable(map) || cell.GetFirstPawn(map) != null)
                    return false;
                foreach (Thing thing in cell.GetThingList(map))
                    if (GenSpawn.SpawningWipes(ThingDefOf.Bed, thing.def)) return false;
            }
            return true;
        }

        //读书调试使用鼠标所在的可站立空格。
        private static Pawn SpawnAtMouse()
        {
            Map map = Find.CurrentMap;
            IntVec3 cell = Verse.UI.MouseCell();
            if (map == null || !cell.InBounds(map) || !cell.Standable(map) || cell.GetFirstPawn(map) != null)
            {
                Messages.Message("请点击可站立且没有角色的地图格子。", MessageTypeDefOf.RejectInput, false);
                return null;
            }
            return SpawnPawn(map, cell);
        }

        //生成能够阅读的成年殖民者并补满食物和休息，避免需求立即打断测试。
        private static Pawn SpawnPawn(Map map, IntVec3 cell)
        {
            PawnGenerationRequest request = new PawnGenerationRequest(
                DefOfRefs.NingshaRace_Colonist, Faction.OfPlayer, PawnGenerationContext.NonPlayer, map.Tile,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                allowPregnant: false,
                allowFood: false,
                allowAddictions: false,
                validatorPreGear: BookUtility.CanReadEver,
                fixedBiologicalAge: 25f,
                dontGiveWeapon: true,
                pawnKindDefGetter: _ => DefOfRefs.NingshaRace_Colonist);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            GenSpawn.Spawn(pawn, cell, map, Rot4.East);
            pawn.needs.food.CurLevelPercentage = 1f;
            pawn.needs.rest.CurLevelPercentage = 1f;
            Find.Selector.ClearSelection();
            Find.Selector.Select(pawn);
            return pawn;
        }
    }
}
