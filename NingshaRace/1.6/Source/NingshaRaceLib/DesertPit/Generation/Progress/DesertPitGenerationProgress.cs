using UnityEngine;

namespace NingshaRaceLib.DesertPit.Generation.Progress
{
    //类职责：保存沙漠巨坑分帧生成时的阶段、百分比和当前场景进度窗口显示状态。
    internal static class DesertPitGenerationProgress
    {
        //字段职责：记录当前是否存在正在执行的沙漠巨坑生成流程。
        private static bool active;

        //字段职责：记录当前生成阶段的中文名称。
        private static string stage = "准备地图";

        //字段职责：记录零到一之间的总生成进度。
        private static float progress;

        //字段职责：记录当前生成步骤在总进度中的起始位置。
        private static float stepStart;

        //字段职责：记录当前生成步骤在总进度中的结束位置。
        private static float stepEnd;

        //属性职责：向入口与进度窗口提供当前生成状态。
        public static bool Active => active;

        //属性职责：向进度窗口提供当前生成阶段的中文名称。
        public static string Stage => stage;

        //属性职责：向进度窗口提供限制在有效范围内的生成进度。
        public static float Progress => Mathf.Clamp01(progress);

        //函数职责：开始一次沙漠巨坑生成并初始化阶段信息。
        public static void Begin()
        {
            active = true;
            stage = "准备地图";
            progress = 0f;
            stepStart = 0f;
            stepEnd = 0f;
        }

        //函数职责：更新当前阶段名称并保留已有总进度。
        public static void SetStage(string newStage)
        {
            stage = newStage;
        }

        //函数职责：更新总生成进度并限制在有效范围内。
        public static void SetProgress(float value)
        {
            progress = Mathf.Clamp01(value);
        }

        //函数职责：设置当前生成步骤占用的总进度区间。
        public static void SetStepRange(float start, float end)
        {
            stepStart = Mathf.Clamp01(start);
            stepEnd = Mathf.Clamp01(end);
        }

        //函数职责：把步骤内部进度换算为总进度。
        public static void SetStepFraction(float fraction)
        {
            SetProgress(Mathf.Lerp(stepStart, stepEnd, Mathf.Clamp01(fraction)));
        }

        //函数职责：同时更新生成阶段和总进度。
        public static void Report(string newStage, float value)
        {
            stage = newStage;
            progress = Mathf.Clamp01(value);
        }

        //函数职责：结束进度显示并清理本次生成状态。
        public static void End()
        {
            active = false;
            stage = "准备地图";
            progress = 0f;
            stepStart = 0f;
            stepEnd = 0f;
        }
    }
}
