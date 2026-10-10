using LudeonTK;
using RimWorld;
using Verse;

using NingshaRaceLib.Erosion.Editor;
using NingshaRaceLib.Erosion.Components;
using NingshaRaceLib.Erosion.Utility;
using NingshaRaceLib.SandGolem.Tracking;

namespace NingshaRaceLib.DevTools.Erosion
{
    //提供侵蚀体生成、即时转化和蛇头可视化调整入口。
    public static class NingshaErosionDebugActions
    {
        //在当前鼠标空格调用统一生成工具放置一名侵蚀体。
        [DebugAction("NingshaRace", "生成凝砂族侵蚀体", actionType = DebugActionType.ToolMap,
            allowedGameStates = AllowedGameStates.PlayingOnMap, requiresAnomaly = true)]
        public static void SpawnErosionBody()
        {
            SpawnAtMouse(false);
        }

        //每次点击重新抽取非凝砂原身，供原版实体生成菜单调用。
        public static void SpawnRandomErosionBody()
        {
            SpawnAtMouse(true);
        }

        //先检查候选和落点，再复用侵蚀体生成流程。
        private static void SpawnAtMouse(bool randomNonNingsha)
        {
            Map map = Find.CurrentMap;
            IntVec3 cell = Verse.UI.MouseCell();
            if (map == null || !cell.InBounds(map) || !cell.Standable(map) || cell.GetFirstPawn(map) != null)
            {
                Messages.Message("请在可站立且没有角色的地图格子生成侵蚀体。", MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (Faction.OfEntities == null)
            {
                Messages.Message("当前游戏不存在实体阵营，无法生成侵蚀体。", MessageTypeDefOf.RejectInput, false);
                return;
            }
            PawnKindDef kind = randomNonNingsha ? ErosionBodySpawnUtility.RandomNonNingshaKind() : null;
            if (randomNonNingsha && kind == null)
            {
                Messages.Message("没有可生成的非凝砂人形原身。", MessageTypeDefOf.RejectInput, false);
                return;
            }
            ErosionBodySpawnUtility.Spawn(map, cell, kind);
        }

        //点击存活的人形角色，跳过侵蚀积累与起身动画并立即安装侵蚀体身份。
        [DebugAction("NingshaRace", "立刻转换为侵蚀体", actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap, requiresAnomaly = true)]
        public static void TurnIntoErosionBodyNow(Pawn pawn)
        {
            string rejection = ErosionBodySpawnUtility.ConversionRejectionReason(pawn);
            if (rejection == null && Faction.OfEntities == null)
                rejection = "当前游戏不存在实体阵营，无法转化为侵蚀体。";
            if (rejection != null)
            {
                Messages.Message(rejection, MessageTypeDefOf.RejectInput, false);
                return;
            }

            pawn.TryGetComp<CompNingshaErosion>()?.CancelTransformation();
            ErosionTransformationUtility.UnlockPawn(pawn);
            ErosionTransformationUtility.RemoveTransformationHediff(pawn);
            ErosionTransformationUtility.DropFromCarrier(pawn);
            GameComponent_SandGolemTracker.Current?.RecallGolemForCaster(pawn);
            ErosionBodySpawnUtility.TurnIntoErosionBody(pawn);
        }

        //从已选中的侵蚀体打开四向编辑窗口。
        [DebugAction("NingshaRace", "编辑侵蚀体蛇头（选中角色）", allowedGameStates = AllowedGameStates.PlayingOnMap, requiresAnomaly = true)]
        public static void EditSnakeHeads()
        {
            Pawn pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null || !pawn.Spawned || !ErosionPawnUtility.IsNingshaErosionBody(pawn))
            { Messages.Message("请先选中地图上的一名凝砂族侵蚀体。", MessageTypeDefOf.RejectInput, false); return; }
            Find.WindowStack.Add(new Window_ErosionSnakeEditor(pawn));
        }
    }
}
