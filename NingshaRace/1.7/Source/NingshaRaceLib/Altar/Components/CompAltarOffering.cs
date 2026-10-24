using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

using NingshaRaceLib.AltarMissions.Core;
using NingshaRaceLib.AltarMissions.Generation;
using NingshaRaceLib.AltarMissions.World;
using NingshaRaceLib.Altar.Jobs;
using NingshaRaceLib.Core.Defs;

using NingshaRaceLib.UI.Gizmos;
using NingshaRaceLib.UI.Models;
using NingshaRaceLib.UI.Windows;

namespace NingshaRaceLib.Altar.Components
{
    //类职责：保存祭坛供奉营养与启用状态，接收生肉并提供填充、调试和任务交互。
    public sealed class CompAltarOffering : ThingComp
    {
        //字段职责：保存已经被祭坛消耗并跨存档保留的生肉营养。
        private float storedNutrition;

        //字段职责：保存玩家是否允许搬运者继续向祭坛供奉生肉。
        private bool offeringEnabled = true;

        //属性职责：提供祭坛供奉配置。
        public CompProperties_AltarOffering Props => (CompProperties_AltarOffering)props;

        //属性职责：提供当前已供奉营养值。
        public float StoredNutrition => storedNutrition;

        //属性职责：判断祭坛是否达到发布任务所需供奉值。
        public bool Full => storedNutrition >= Props.nutritionCapacity - 0.0001f;

        //属性职责：判断祭坛是否已经通过原版占用命令划归玩家殖民地。
        public bool OccupiedByPlayer => parent.Faction == Faction.OfPlayer;

        //属性职责：提供玩家当前设置的供奉许可状态。
        public bool OfferingEnabled => offeringEnabled;

        //属性职责：判断祭坛是否同时满足所有接收生肉的条件。
        public bool CanAcceptOffering => OccupiedByPlayer && offeringEnabled && !Full;

        //属性职责：计算祭坛尚可接收的营养值。
        public float MissingNutrition => Mathf.Max(0f, Props.nutritionCapacity - storedNutrition);

        //函数职责：保存祭坛已消耗的供奉营养值，使缩小包装和存读档均不返还原料。
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref storedNutrition, "storedOfferingNutrition", 0f);
            Scribe_Values.Look(ref offeringEnabled, "offeringEnabled", true);
        }

        //函数职责：显示供奉进度和全局任务占用状态。
        public override string CompInspectStringExtra()
        {
            string text = OccupiedByPlayer ? "祭坛状态：已占用" : "祭坛状态：无主，需先占用";
            text += "\n供奉营养：" + storedNutrition.ToString("0.##") + " / " + Props.nutritionCapacity.ToString("0.##");
            text += "\n供奉设置：" + (offeringEnabled ? "允许" : "禁止");
            if (AltarMissionWorldComponent.Current?.HasActiveMission == true)
            {
                text += "\n智慧之蛇正在指引另一项任务。";
            }
            return text;
        }

        //函数职责：为可操作的自由殖民者提供祭坛填充与三百Tick祈求任务。
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.CompFloatMenuOptions(selPawn))
            {
                yield return option;
            }
            if (!selPawn.IsColonistPlayerControlled)
            {
                yield break;
            }

            yield return MakeManualFillOption(selPawn, includePawnName: false);
            if (!OccupiedByPlayer)
            {
                yield return new FloatMenuOption("接受智慧之蛇任务（需要先占用）", null);
                yield break;
            }
            if (!Full)
            {
                yield return new FloatMenuOption("接受智慧之蛇任务（供奉尚未充满）", null);
                yield break;
            }
            if (AltarMissionWorldComponent.Current?.HasActiveMission == true)
            {
                yield return new FloatMenuOption("接受智慧之蛇任务（已有进行中的祭坛任务）", null);
                yield break;
            }
            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption("接受智慧之蛇任务", delegate
                {
                    Job job = JobMaker.MakeJob(DefOfRefs.NingshaRace_Job_ConsultWisdomSerpentAltar, parent);
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }), selPawn, parent);
        }

        //函数职责：为多选殖民者逐个提供带姓名的祭坛优先填充选项。
        public override IEnumerable<FloatMenuOption> CompMultiSelectFloatMenuOptions(IEnumerable<Pawn> selPawns)
        {
            foreach (FloatMenuOption option in base.CompMultiSelectFloatMenuOptions(selPawns))
            {
                yield return option;
            }

            foreach (Pawn pawn in selPawns)
            {
                if (pawn?.IsColonistPlayerControlled == true)
                {
                    yield return MakeManualFillOption(pawn, includePawnName: true);
                }
            }
        }

        //函数职责：为玩家提供供奉许可开关，并在上帝模式下提供填满供奉和指定任务的调试命令。
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            if (OccupiedByPlayer)
            {
                yield return new Command_NingshaToggle
                {
                    defaultLabel = "允许供奉",
                    defaultDesc = "关闭后禁止自动搬运和右键优先填充生肉；已经完成的供奉和任务交互不受影响。",
                    icon = parent.def.uiIcon,
                    isActive = delegate { return offeringEnabled; },
                    toggleAction = delegate { offeringEnabled = !offeringEnabled; }
                };
            }
            if (DebugSettings.godMode)
            {
                yield return new Command_NingshaAction
                {
                    defaultLabel = "DEV：填满祭坛供奉",
                    defaultDesc = "不消耗生肉，立即把智慧之蛇祭坛的供奉营养设为上限。",
                    icon = parent.def.uiIcon,
                    action = delegate { storedNutrition = Props.nutritionCapacity; }
                };
                Command_Action chooseMission = new Command_NingshaAction
                {
                    defaultLabel = "DEV：选择祭坛任务",
                    defaultDesc = "直接选择并发布一种智慧之蛇祭坛任务，不要求占用或供奉，也不消耗供奉值。",
                    icon = parent.def.uiIcon,
                    action = OpenDebugMissionMenu
                };
                if (AltarMissionWorldComponent.Current?.HasActiveMission == true)
                {
                    chooseMission.Disable("已有进行中的祭坛任务。完成或失败后才能发布下一项任务。");
                }
                yield return chooseMission;
            }
        }

        //函数职责：打开包含三类祭坛任务的调试选择菜单。
        private void OpenDebugMissionMenu()
        {
            List<NingshaChoice> options = new List<NingshaChoice>
            {
                MakeDebugMissionOption("小型遗迹", AltarMissionType.SmallRuins),
                MakeDebugMissionOption("清剿蚁巢", AltarMissionType.AntNest),
                MakeDebugMissionOption("解救同胞", AltarMissionType.RescueKinsfolk)
            };
            Find.WindowStack.Add(new Dialog_NingshaChoices("智慧之蛇 · 选择指引", options));
        }

        //函数职责：为指定任务类型创建直接发布任务且不消耗供奉的调试菜单项。
        private NingshaChoice MakeDebugMissionOption(string label, AltarMissionType missionType)
        {
            return new NingshaChoice(label, delegate
            {
                if (!AltarMissionGenerator.TryGenerateMission(missionType, null))
                {
                    Messages.Message("已有进行中的祭坛任务，无法发布新的任务。", MessageTypeDefOf.RejectInput, false);
                }
            });
        }

        //函数职责：按原料实际营养消耗生肉并把供奉值钳制到容量上限。
        public int ConsumeRawMeat(Thing meat)
        {
            if (!CanAcceptOffering || !AltarOfferingJobUtility.IsAcceptedRawMeat(meat))
            {
                return 0;
            }
            float nutritionPerItem = meat.GetStatValue(StatDefOf.Nutrition);
            int count = Mathf.Min(meat.stackCount, Mathf.CeilToInt(MissingNutrition / nutritionPerItem));
            storedNutrition = Mathf.Min(Props.nutritionCapacity, storedNutrition + nutritionPerItem * count);
            meat.SplitOff(count).Destroy(DestroyMode.Vanish);
            return count;
        }

        //函数职责：尝试生成一项等概率祭坛任务，并且仅在任务成功登记后扣除一百供奉值。
        public bool TryIssueMission(Pawn pawn)
        {
            if (!OccupiedByPlayer || !Full || !AltarMissionGenerator.TryGenerateRandomMission(pawn))
            {
                return false;
            }
            storedNutrition = Mathf.Max(0f, storedNutrition - Props.nutritionCapacity);
            return true;
        }

        //函数职责：按共享判定结果创建可执行或带明确禁用原因的祭坛填充选项。
        private FloatMenuOption MakeManualFillOption(Pawn pawn, bool includePawnName)
        {
            string label = "优先填充智慧之蛇祭坛";
            if (includePawnName)
            {
                label += "（" + pawn.LabelShortCap + "）";
            }

            if (!AltarOfferingJobUtility.TryMakeManualFillJob(pawn, parent, out Job job, out string rejectReason))
            {
                return new FloatMenuOption(label + "（" + rejectReason + "）", null);
            }

            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(label, delegate
                {
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }), pawn, parent);
        }
    }
}
