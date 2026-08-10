using RimWorld;
using Verse;

using NingshaRaceLib.DesertPit.Generation.Progress;

namespace NingshaRaceLib.DesertPit.Buildings
{
    //类职责：作为凝砂族沙漠巨坑入口，沿用原版口袋地图入口交互并指向自定义地下沙岩洞穴。
    public class Building_DesertPitGate : MapPortal
    {
        //字段职责：记录当前入口是否正在生成口袋地图，避免多个进入者重复启动生成流程。
        private bool generationActive;

        //字段职责：记录最近一次生成是否失败，让等待进入的作业能够结束并允许玩家重试。
        private bool generationFailed;

        //属性职责：向进入作业报告该入口当前是否正在生成地图。
        public bool GenerationInProgress => generationActive;

        //属性职责：向进入作业报告最近一次地图生成是否失败。
        public bool GenerationFailed => generationFailed;

        //函数职责：在进入者抵达入口并完成前摇后启动当前场景内的分帧地图生成窗口。
        public void BeginPocketMapGeneration()
        {
            if (PocketMapExists || generationActive || DesertPitGenerationProgress.Active)
            {
                return;
            }

            generationActive = true;
            generationFailed = false;
            DesertPitGenerationProgress.Begin();
            Find.WindowStack.Add(new Window_DesertPitGeneration(this));
        }

        //函数职责：接收分帧生成完成的口袋地图并交由原版传送门逻辑管理。
        internal void AssignPocketMap(Map map)
        {
            pocketMap = map;
        }

        //函数职责：结束成功的地图生成状态，使等待中的进入作业继续传送。
        internal void NotifyGenerationSucceeded()
        {
            generationActive = false;
            generationFailed = false;
        }

        //函数职责：结束失败的地图生成状态，使等待中的进入作业终止并允许下次重试。
        internal void NotifyGenerationFailed()
        {
            generationActive = false;
            generationFailed = true;
        }
    }
}
