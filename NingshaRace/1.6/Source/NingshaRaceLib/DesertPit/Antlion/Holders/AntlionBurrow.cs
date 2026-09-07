using System;
using System.Collections.Generic;
using RimWorld;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Antlion.AI;
using NingshaRaceLib.DesertPit.Antlion.Components;
using NingshaRaceLib.DesertPit.Antlion.Effects;
using Verse;

namespace NingshaRaceLib.DesertPit.Antlion.Holders
{
    //类职责：以不可选中的地下实体独占持有一只蚁狮，只在安全出土后释放同一 Pawn。
    public sealed class AntlionBurrow : ThingWithComps, IThingHolderTickable, ISuspendableThingHolder
    {
        private ThingOwner<Pawn> contents;
        private int rearmAtTick;

        //属性职责：关闭原版持有物自动更新，使地下 Pawn 不运行任务、出血或自然治疗。
        public bool ShouldTickContents => false;

        //属性职责：下潜转移完成的当前帧也立即挂起 Pawn，防止外层更新继续运行健康与任务。
        public bool IsContentsSuspended => true;

        //构造职责：建立只允许一个实体的深度存档容器。
        public AntlionBurrow()
        {
            contents = new ThingOwner<Pawn>(this, true);
        }

        //函数职责：保存地下蚁狮及重新触发期限，不额外保存第二份 Pawn。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref contents, "contents", this);
            Scribe_Values.Look(ref rearmAtTick, "rearmAtTick");
        }

        //函数职责：向原版地图存档和持有物查询提供实际容器。
        public ThingOwner GetDirectlyHeldThings()
        {
            return contents;
        }

        //函数职责：递归登记蚁狮身上附属容器，保证存档与地图移除能发现完整持有链。
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, contents);
        }

        //函数职责：错峰进行小半径检查并播放稀疏沙粒，不更新地下 Pawn 的战斗或治疗逻辑。
        protected override void Tick()
        {
            base.Tick();
            if (contents.Count == 0)
            {
                Destroy();
                return;
            }
            Pawn pawn = contents[0];
            CompAntlionAmbush comp = pawn.GetComp<CompAntlionAmbush>();
            int now = Find.TickManager.TicksGame;
            AntlionSandEffects.TickBuried(Map, Position, thingIDNumber);
            if ((now + thingIDNumber) % comp.Props.scanIntervalTicks != 0) return;

            //铺地或占用潜伏格后只尝试安全释放原实体，不破坏覆盖的建筑，也不隔墙寻找出口。
            bool displaced = !AntlionSandUtility.CanBurrowAt(Map, Position, comp.Props, this);
            Pawn prey = now >= rearmAtTick
                ? AntlionTargetUtility.FindNearest(Map, Position, comp.Props, Position) : null;
            if (!displaced && prey == null) return;
            IntVec3 cell = FindSurfaceCell();
            if (!cell.IsValid) return;
            Pawn released;
            if (!contents.TryDrop(pawn, cell, Map, ThingPlaceMode.Direct, out released))
                throw new InvalidOperationException("蚁狮无法从已验证的地下容器出土：" + this);
            if (prey != null) comp.BeginEmerging(prey);
            //无猎物的安全释放由地表组件从地下阶段转入寻找沙地，不给予突袭加速。
            Destroy();
        }

        //函数职责：优先原格出土，被 Pawn 占据时只使用相邻且可直达的空格，不挤走实体。
        private IntVec3 FindSurfaceCell()
        {
            for (int i = 0; i < 9; i++)
            {
                IntVec3 cell = Position + GenRadial.RadialPattern[i];
                if (AntlionSandUtility.CanSurfaceAt(Map, cell, this)
                    && GenSight.LineOfSight(Position, cell, Map, skipFirstCell: false)) return cell;
            }
            return IntVec3.Invalid;
        }

        //函数职责：创建地下容器后转移原 Pawn 的唯一所有权，不重生、不治疗、不覆盖地面实体。
        public static void Store(Pawn pawn, Map map, IntVec3 cell, int rearmAt)
        {
            CompAntlionAmbush comp = pawn.GetComp<CompAntlionAmbush>();
            if (pawn.Dead || pawn.Downed || pawn.IsBurning() || !AntlionSandUtility.CanBurrowAt(map, cell, comp.Props, pawn))
                throw new InvalidOperationException("蚁狮不能在当前状态或格子潜伏：" + pawn + "，" + cell);
            AntlionBurrow burrow = (AntlionBurrow)ThingMaker.MakeThing(DefOfRefs.NingshaRace_AntlionBurrow);
            burrow.rearmAtTick = rearmAt;
            GenSpawn.Spawn(burrow, cell, map);
            if (pawn.Spawned) pawn.DeSpawn();
            comp.PrepareBuried();
            if (!burrow.contents.TryAddOrTransfer(pawn))
                throw new InvalidOperationException("蚁狮地下容器拒绝接收原 Pawn：" + pawn);
        }

        //函数职责：清理被删除容器的持有物，空容器出土销毁时不影响已释放蚁狮。
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            ClearContents();
            base.Destroy(mode);
        }

        //函数职责：地图卸载时清空地下持有物，不保留指向已移除地图的潜伏实体。
        public override void Notify_MyMapRemoved()
        {
            ClearContents();
            base.Notify_MyMapRemoved();
        }

        //函数职责：移除持有链和原版地图卸载产生的世界登记，再销毁并回收唯一地下 Pawn。
        private void ClearContents()
        {
            while (contents.Count > 0)
            {
                Pawn pawn = contents[0];
                contents.Remove(pawn);
                if (Find.WorldPawns.Contains(pawn)) Find.WorldPawns.RemovePawn(pawn);
                if (!pawn.Destroyed) pawn.Destroy();
                if (!pawn.Discarded) pawn.Discard(true);
            }
        }
    }
}
