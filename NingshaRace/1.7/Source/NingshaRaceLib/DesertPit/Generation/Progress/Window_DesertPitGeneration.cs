using System;
using System.Collections;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.PocketMaps.Buildings;
using NingshaRaceLib.UI.Windows;
using NingshaRaceLib.UI.Panels;
using NingshaRaceLib.UI.Foundation;

namespace NingshaRaceLib.DesertPit.Generation.Progress
{
    //类职责：在当前地图画面上暂停游戏并按帧推进凝砂口袋地图生成，同时显示实际阶段与进度。
    internal sealed class Window_DesertPitGeneration : NingshaWindow
    {
        //字段职责：限制单帧用于地图生成的最长实时秒数，避免长时间阻塞界面刷新。
        private const float FrameBudgetSeconds = 0.025f;

        //字段职责：限制单帧最多推进的批次数，避免极短步骤形成无界循环。
        private const int MaxBatchesPerFrame = 128;

        //字段职责：保存正在生成口袋地图的凝砂入口。
        private readonly Building_NingshaPocketMapPortal gate;

        //字段职责：保存分批地图生成器，使窗口能够在相邻画面帧之间继续执行。
        private IEnumerator generator;

        //字段职责：记录生成流程是否已经结束，仅在结束后允许窗口关闭。
        private bool finished;

        //字段职责：记录玩家是否展开生成规则说明。
        private bool showExplanation;

        //属性职责：提供适合阶段文字和进度条的居中窗口尺寸。
        public override Vector2 InitialSize => new Vector2(Mathf.Min(560f, Verse.UI.screenWidth), Mathf.Min(340f, Verse.UI.screenHeight));

        //函数职责：初始化不可手动关闭的暂停窗口与对应入口的地图生成器。
        public Window_DesertPitGeneration(Building_NingshaPocketMapPortal gate)
        {
            this.gate = gate;
            generator = DesertPitPocketMapGeneration.Generate(gate).GetEnumerator();
            forcePause = true;
            absorbInputAroundWindow = true;
            preventCameraMotion = true;
            preventSave = true;
            closeOnAccept = false;
            closeOnCancel = false;
            closeOnClickedOutside = false;
            doCloseButton = false;
            doCloseX = false;
            draggable = false;
            soundAppear = null;
            soundClose = null;
        }

        //函数职责：在每个界面刷新帧内按时间预算推进生成，并在成功或异常时结束窗口。
        public override void WindowUpdate()
        {
            base.WindowUpdate();
            if (finished)
            {
                return;
            }

            float deadline = Time.realtimeSinceStartup + FrameBudgetSeconds;
            int batches = 0;
            try
            {
                do
                {
                    if (!MoveGeneratorNext())
                    {
                        CompleteGeneration();
                        return;
                    }

                    batches++;
                }
                while (batches < MaxBatchesPerFrame && Time.realtimeSinceStartup < deadline);
            }
            catch (Exception exception)
            {
                FailGeneration(exception);
            }
        }

        //函数职责：临时切换到地图初始化状态推进一个批次，并在返回当前画面前恢复游戏状态。
        private bool MoveGeneratorNext()
        {
            ProgramState previousState = Current.ProgramState;
            Current.ProgramState = ProgramState.MapInitializing;
            try
            {
                return generator.MoveNext();
            }
            finally
            {
                Current.ProgramState = previousState;
            }
        }

        //函数职责：把窗口安全区交给统一生成面板，业务迭代器与关闭限制保持独立。
        public override void DoWindowContents(Rect inRect)
        {
            using (new NingshaGuiScope(GameFont.Small))
            {
                Rect body = DrawShell(inRect, "正在准备目的地", canClose: false);
                NingshaGenerationPanel.Draw(body, DesertPitGenerationProgress.Stage,
                    DesertPitGenerationProgress.Progress, ref showExplanation);
            }
        }

        //函数职责：阻止生成过程中通过快捷键或外部请求关闭窗口。
        public override bool OnCloseRequest()
        {
            return finished;
        }

        //函数职责：释放生成器并通知入口继续完成原版传送作业。
        private void CompleteGeneration()
        {
            finished = true;
            DisposeGenerator();
            DesertPitGenerationProgress.End();
            gate.NotifyGenerationSucceeded();
            Close(doCloseSound: false);
        }

        //函数职责：释放失败生成的临时状态、终止等待作业并向玩家报告日志位置。
        private void FailGeneration(Exception exception)
        {
            finished = true;
            DisposeGenerator();
            DesertPitGenerationProgress.End();
            gate.NotifyGenerationFailed();
            Messages.Message(gate.LabelCap + "准备失败，请查看错误记录后重试。", gate, MessageTypeDefOf.NegativeEvent);
            Log.Error(gate.LabelCap + "分帧生成失败：" + exception);
            Close(doCloseSound: false);
        }

        //函数职责：释放支持清理接口的迭代器，使中断时也会执行地图生成器的收尾逻辑。
        private void DisposeGenerator()
        {
            IDisposable disposable = generator as IDisposable;
            disposable?.Dispose();
            generator = null;
        }
    }
}
