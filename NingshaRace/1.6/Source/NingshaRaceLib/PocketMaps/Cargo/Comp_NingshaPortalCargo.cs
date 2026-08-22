using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.PocketMaps.Buildings;

namespace NingshaRaceLib.PocketMaps.Cargo
{
    //类职责：管理传送门的独立货运模式、按钮、进入锁定和存档状态。
    public sealed class Comp_NingshaPortalCargo : ThingComp
    {
        //字段职责：复用原版装载命令图标展示货运入口。
        private static readonly Texture2D CargoIcon = ContentFinder<Texture2D>.Get("UI/Commands/LoadTransporter");

        //字段职责：记录当前装载清单是否由独立货运窗口创建。
        private bool cargoTransferActive;

        //属性职责：在清单仍有待搬运内容时报告独立货运状态。
        public bool CargoTransferActive => cargoTransferActive && Portal.LoadInProgress;

        //属性职责：取得组件所属的原版地图传送门。
        private MapPortal Portal => (MapPortal)parent;

        //函数职责：保存独立货运模式以保持存档中的按钮与文案一致。
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref cargoTransferActive, "cargoTransferActive", false);
        }

        //函数职责：提供独立货运按钮，并在其他装载或地图生成期间给出明确禁用原因。
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Command_Action command = new Command_Action
            {
                defaultLabel = "搬运物资……",
                defaultDesc = "选择本地图上的玩家动物与可达物资，由殖民者搬运到传送门并送往另一张地图。搬运者不会跟随货物过图。",
                icon = CargoIcon,
                action = OpenCargoDialog
            };

            if (Portal.LoadInProgress)
            {
                command.Disable(CargoTransferActive ? "当前已有货运任务正在装载。" : "当前已有整队进入任务正在装载。");
            }
            else if (parent is Building_NingshaPocketMapPortal gate && gate.GenerationInProgress)
            {
                command.Disable("正在生成地下地图，请稍候。");
            }

            yield return command;
        }

        //函数职责：货运进行期间阻止原版整队进入，避免两个装载流程共用同一清单。
        public override AcceptanceReport CanEnterPortal()
        {
            if (CargoTransferActive)
            {
                return "货运任务完成或取消前无法整队进入。";
            }

            return true;
        }

        //函数职责：把窗口确认的清单标记为独立货运并按需启动分帧地图生成。
        public void ActivateCargoTransfer()
        {
            cargoTransferActive = Portal.LoadInProgress;
            if (cargoTransferActive && parent is Building_NingshaPocketMapPortal gate)
            {
                gate.BeginPocketMapGeneration();
            }
        }

        //函数职责：在地图生成失败时取消清单并立即恢复传送门进入能力。
        public void CancelCargoTransfer()
        {
            if (Portal.LoadInProgress)
            {
                Portal.CancelLoad();
            }

            cargoTransferActive = false;
        }

        //函数职责：打开只选择动物和物品的凝砂族货运窗口。
        private void OpenCargoDialog()
        {
            Find.WindowStack.Add(new Dialog_NingshaPortalCargo(Portal, this));
        }
    }
}
