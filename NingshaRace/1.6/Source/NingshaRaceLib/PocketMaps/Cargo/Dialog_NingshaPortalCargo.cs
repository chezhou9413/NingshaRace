using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NingshaRaceLib.PocketMaps.Cargo
{
    //类职责：用原版单向装载控件呈现仅含动物与物品的跨地图货运选择窗口。
    public sealed class Dialog_NingshaPortalCargo : Window
    {
        //枚举职责：区分货运窗口当前展示的动物页与物品页。
        private enum CargoTab
        {
            Animals,
            Items
        }

        //字段职责：定义窗口外框、页签和底部按钮之间的安全间距。
        private const float ContentMargin = 18f;
        private const float TabGap = 34f;
        private const float FooterHeight = 58f;
        private const float ButtonWidth = 160f;
        private const float ButtonHeight = 40f;

        //字段职责：记录本次货运所属的传送门。
        private readonly MapPortal portal;

        //字段职责：记录负责切换货运模式的传送门组件。
        private readonly Comp_NingshaPortalCargo cargoComp;

        //字段职责：保存窗口内全部可调整的单向装载项。
        private List<TransferableOneWay> transferables;

        //字段职责：保存动物页的原版装载控件实例。
        private TransferableOneWayWidget animalsWidget;

        //字段职责：保存物品页的原版装载控件实例。
        private TransferableOneWayWidget itemsWidget;

        //字段职责：记录当前页签。
        private CargoTab selectedTab;

        //字段职责：复用页签列表以避免每帧产生临时集合。
        private static readonly List<TabRecord> Tabs = new List<TabRecord>();

        //属性职责：让货运窗口占用足够宽度和当前界面高度。
        public override Vector2 InitialSize => new Vector2(1024f, UI.screenHeight);

        //属性职责：由窗口内部统一管理安全边距。
        protected override float Margin => 0f;

        //函数职责：建立强制暂停且吸收外部输入的货运窗口。
        public Dialog_NingshaPortalCargo(MapPortal portal, Comp_NingshaPortalCargo cargoComp)
        {
            this.portal = portal;
            this.cargoComp = cargoComp;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        //函数职责：窗口打开后收集一次当前地图上可发送的动物与物品。
        public override void PostOpen()
        {
            base.PostOpen();
            RebuildTransferables();
        }

        //函数职责：按实测标题高度划分标题、页签主体与底部按钮，并恢复所有全局绘制状态。
        public override void DoWindowContents(Rect inRect)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            Color oldColor = GUI.color;

            try
            {
                Rect outer = inRect.ContractedBy(ContentMargin);
                string title = portal is PocketMapExit ? "向地表搬运物资" : "向地下搬运物资";
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.WordWrap = true;
                float titleHeight = Mathf.Max(35f, Text.CalcHeight(title, outer.width));
                Widgets.Label(new Rect(outer.x, outer.y, outer.width, titleHeight), title);

                Rect panel = new Rect(outer.x, outer.y + titleHeight + TabGap, outer.width, outer.height - titleHeight - TabGap);
                Widgets.DrawMenuSection(panel);
                DrawTabs(panel);

                Rect inner = panel.ContractedBy(ContentMargin);
                Rect footer = new Rect(inner.x, inner.yMax - FooterHeight, inner.width, FooterHeight);
                Rect body = new Rect(inner.x, inner.y, inner.width, Mathf.Max(0f, inner.height - FooterHeight - 8f));
                DrawBody(body);
                DrawFooter(footer);
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
                GUI.color = oldColor;
            }
        }

        //函数职责：响应确认快捷键并只在存在有效装载内容时关闭窗口。
        public override void OnAcceptKeyPressed()
        {
            if (TryAccept())
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                Close(false);
            }
        }

        //函数职责：绘制动物与物品页签并切换当前装载控件。
        private void DrawTabs(Rect panel)
        {
            Tabs.Clear();
            Tabs.Add(new TabRecord("动物", () => selectedTab = CargoTab.Animals, selectedTab == CargoTab.Animals));
            Tabs.Add(new TabRecord("物品", () => selectedTab = CargoTab.Items, selectedTab == CargoTab.Items));
            TabDrawer.DrawTabs(panel, Tabs);
        }

        //函数职责：在独立主体区域绘制当前页签对应的原版装载列表。
        private void DrawBody(Rect body)
        {
            bool anythingChanged;
            if (selectedTab == CargoTab.Animals)
            {
                animalsWidget.OnGUI(body, out anythingChanged);
            }
            else
            {
                itemsWidget.OnGUI(body, out anythingChanged);
            }
        }

        //函数职责：绘制取消、重置和确认按钮，并保证按钮区域不与滚动主体重叠。
        private void DrawFooter(Rect footer)
        {
            float buttonY = footer.yMax - ButtonHeight;
            if (Widgets.ButtonText(new Rect(footer.x, buttonY, ButtonWidth, ButtonHeight), "CancelButton".Translate()))
            {
                Close();
            }

            if (Widgets.ButtonText(new Rect(footer.center.x - ButtonWidth / 2f, buttonY, ButtonWidth, ButtonHeight), "ResetButton".Translate()))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                RebuildTransferables();
            }

            if (Widgets.ButtonText(new Rect(footer.xMax - ButtonWidth, buttonY, ButtonWidth, ButtonHeight), "AcceptButton".Translate()) && TryAccept())
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                Close(false);
            }
        }

        //函数职责：把玩家选择写入原版传送门清单，并启动独立货运与必要的地图生成。
        private bool TryAccept()
        {
            portal.leftToLoad = new List<TransferableOneWay>();
            foreach (TransferableOneWay transferable in transferables)
            {
                portal.AddToTheToLoadList(transferable, transferable.CountToTransfer);
            }

            if (!portal.LoadInProgress)
            {
                Messages.Message("请至少选择一只动物或一件物品。", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            cargoComp.ActivateCargoTransfer();
            return true;
        }

        //函数职责：依据原版可发送动物和可达殖民地物资规则重建装载项与控件缓存。
        private void RebuildTransferables()
        {
            transferables = new List<TransferableOneWay>();
            foreach (Pawn pawn in CaravanFormingUtility.AllSendablePawns(portal.Map, true, false, false, false, true))
            {
                if (pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer)
                {
                    AddToTransferables(pawn);
                }
            }

            bool isPocketMap = portal.Map.IsPocketMap;
            foreach (Thing thing in CaravanFormingUtility.AllReachableColonyItems(portal.Map, isPocketMap, isPocketMap))
            {
                AddToTransferables(thing);
            }

            IEnumerable<TransferableOneWay> animals = transferables.Where(transferable => transferable.ThingDef.category == ThingCategory.Pawn);
            IEnumerable<TransferableOneWay> items = transferables.Where(transferable => transferable.ThingDef.category != ThingCategory.Pawn);
            animalsWidget = CreateWidget(null);
            animalsWidget.AddSection("可发送的玩家动物", animals);
            itemsWidget = CreateWidget(items);
        }

        //函数职责：创建与原版传送门装载窗口一致的单向装载控件。
        private TransferableOneWayWidget CreateWidget(IEnumerable<TransferableOneWay> source)
        {
            return new TransferableOneWayWidget(source, null, null, "TransferMapPortalColonyThingCountTip".Translate(), true,
                IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, true, () => float.MaxValue, 0f, false, portal.Map.Tile);
        }

        //函数职责：按照原版运输分组规则合并同类 Thing，避免同一实体被重复加入。
        private void AddToTransferables(Thing thing)
        {
            TransferableOneWay transferable = TransferableUtility.TransferableMatching(thing, transferables, TransferAsOneMode.PodsOrCaravanPacking);
            if (transferable == null)
            {
                transferable = new TransferableOneWay();
                transferables.Add(transferable);
            }

            if (!transferable.things.Contains(thing))
            {
                transferable.things.Add(thing);
            }
        }
    }
}
