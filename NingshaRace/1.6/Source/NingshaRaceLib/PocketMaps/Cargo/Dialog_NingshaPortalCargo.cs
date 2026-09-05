using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;
using NingshaRaceLib.PocketMaps.Cargo.UI;
using NingshaRaceLib.UI.Controls;
using NingshaRaceLib.UI.Foundation;
using NingshaRaceLib.UI.Layout;
using NingshaRaceLib.UI.Windows;

namespace NingshaRaceLib.PocketMaps.Cargo
{
    //类职责：把原版货运分组和派发模型接入凝砂页签、清单及操作区，不更改跨地图传输协议。
    public sealed class Dialog_NingshaPortalCargo : NingshaWindow
    {
        private readonly MapPortal portal;
        private readonly Comp_NingshaPortalCargo cargoComp;
        private readonly NingshaCargoListPanel list = new NingshaCargoListPanel();
        private List<TransferableOneWay> transferables;
        private bool animalsTab = true;

        //属性职责：按屏幕可用尺寸给清单和页脚留出空间。
        public override Vector2 InitialSize => new Vector2(Mathf.Min(1024f, Verse.UI.screenWidth), Mathf.Min(800f, Verse.UI.screenHeight));

        //构造职责：建立强制暂停且吸收外部输入的货运窗口。
        public Dialog_NingshaPortalCargo(MapPortal portal, Comp_NingshaPortalCargo cargoComp)
        {
            this.portal = portal;
            this.cargoComp = cargoComp;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnAccept = false;
        }

        //函数职责：窗口打开时收集当前地图可发送的动物和物资。
        public override void PostOpen()
        {
            base.PostOpen();
            RebuildTransferables();
        }

        //函数职责：组合标题、双页签、搜索清单、选择统计和底部操作栏。
        public override void DoWindowContents(Rect inRect)
        {
            using (new NingshaGuiScope(GameFont.Small))
            {
                Rect area = DrawShell(inRect, portal is PocketMapExit ? "向地表搬运物资" : "向地下搬运物资", "选择要搬运的动物和物品。");
                NingshaLayout layout = new NingshaLayout(area);
                Rect tabs = layout.Take(NingshaLayout.RowHeight(padding: 14f));
                if (NingshaButton.Draw(NingshaLayout.Column(tabs, 0, 2), "动物", "cargo:animals", selected: animalsTab)) animalsTab = true;
                if (NingshaButton.Draw(NingshaLayout.Column(tabs, 1, 2), "物品", "cargo:items", selected: !animalsTab)) animalsTab = false;
                float footerHeight = NingshaLayout.RowHeight(padding: 16f) + NingshaLayout.RowHeight(GameFont.Tiny, 4f) + 8f;
                Rect body = NingshaLayout.BodyWithFooter(layout.Remaining, footerHeight, out Rect footer);
                list.Bind(transferables, animalsTab);
                list.Draw(body);
                DrawFooter(footer);
            }
        }

        //函数职责：仅在确认通过数量检查并产生有效货运时响应确认快捷键。
        public override void OnAcceptKeyPressed()
        {
            if (TryAccept()) Close(false);
        }

        //函数职责：显示全部页签选择总数，并提供取消、重置和确认操作。
        private void DrawFooter(Rect footer)
        {
            NingshaLayout layout = new NingshaLayout(footer);
            int groups = transferables.Count(item => item.CountToTransfer > 0);
            int count = transferables.Sum(item => item.CountToTransfer);
            bool invalid = NingshaCargoListPanel.HasInvalidAmount(transferables);
            string summary = invalid ? "存在无效数量，请检查红框输入。" : "已选择 " + groups + " 组 · 共 " + count + " 只 / 件";
            NingshaText.Label(layout.Take(NingshaLayout.RowHeight(GameFont.Tiny, 4f)), summary,
                invalid ? NingshaPalette.Warning : NingshaPalette.Muted, GameFont.Tiny);
            Rect row = layout.Remaining;
            if (NingshaButton.Draw(NingshaLayout.Column(row, 0, 3), "取消", "cargo:cancel")) Close();
            if (NingshaButton.Draw(NingshaLayout.Column(row, 1, 3), "重置清单", "cargo:reset")) RebuildTransferables();
            if (NingshaButton.Draw(NingshaLayout.Column(row, 2, 3), "开始搬运", "cargo:accept", !invalid && groups > 0,
                invalid ? "请先修正数量。" : groups == 0 ? "请至少选择一只动物或一件物品。" : "确认后派发搬运任务。", selected: groups > 0)
                && TryAccept()) Close(false);
        }

        //函数职责：校验清单后交给原版传送门模型，保留独立货运和按需地图生成流程。
        private bool TryAccept()
        {
            if (NingshaCargoListPanel.HasInvalidAmount(transferables))
            {
                Messages.Message("请修正货运清单中的无效数量。", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if (!transferables.Any(item => item.CountToTransfer > 0))
            {
                Messages.Message("请至少选择一只动物或一件物品。", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            portal.leftToLoad = new List<TransferableOneWay>();
            foreach (TransferableOneWay item in transferables) portal.AddToTheToLoadList(item, item.CountToTransfer);
            if (!portal.LoadInProgress) return false;
            cargoComp.ActivateCargoTransfer();
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
            return true;
        }

        //函数职责：沿用原版可发送动物和可达物资规则构建清单，不混入殖民者运输。
        private void RebuildTransferables()
        {
            transferables = new List<TransferableOneWay>();
            foreach (Pawn pawn in CaravanFormingUtility.AllSendablePawns(portal.Map, true, false, false, false, true))
            {
                if (pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer) AddToTransferables(pawn);
            }
            bool pocket = portal.Map.IsPocketMap;
            foreach (Thing thing in CaravanFormingUtility.AllReachableColonyItems(portal.Map, pocket, pocket)) AddToTransferables(thing);
            foreach (TransferableOneWay item in transferables) item.EditBuffer = item.CountToTransfer.ToString();
        }

        //函数职责：使用原版规则把同类实体归为一项，避免重复加入同一实体。
        private void AddToTransferables(Thing thing)
        {
            TransferableOneWay item = TransferableUtility.TransferableMatching(thing, transferables, TransferAsOneMode.PodsOrCaravanPacking);
            if (item == null)
            {
                item = new TransferableOneWay();
                transferables.Add(item);
            }
            if (!item.things.Contains(thing)) item.things.Add(thing);
        }
    }
}
