using System.Threading;
using HarmonyLib;
using Verse;

using NingshaRaceLib.Erosion.Rendering;
using NingshaRaceLib.Petrification.Rendering;
using NingshaRaceLib.SandGolem.Tracking;

namespace NingshaRaceLib.Core.Rendering
{
    //类职责：在游戏切换和释放阶段统一回收凝砂族拥有的运行时材质与截图纹理。
    public static class NingshaGraphicsLifecycle
    {
        //字段职责：防止同一次后台异常处理重复登记主线程清理任务。
        private static int resetQueued;

        //函数职责：在主线程立即清理资源，后台长事件则登记一次完成后的主线程清理。
        public static void ResetBeforeGameSwap(Game game)
        {
            if (!UnityData.IsInMainThread)
            {
                if (Interlocked.Exchange(ref resetQueued, 1) == 0)
                {
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        Interlocked.Exchange(ref resetQueued, 0);
                        ResetCapturedGame(game);
                    });
                }
                return;
            }

            Interlocked.Exchange(ref resetQueued, 0);
            ResetCapturedGame(game);
        }

        //函数职责：在游戏主线程释放指定游戏的动态资源并清空跨游戏共享的材质缓存。
        private static void ResetCapturedGame(Game game)
        {
            game?.GetComponent<GameComponent_SandGolemTracker>()?.ReleaseRuntimeResources();
            PetrificationMaterialPool.Reset();
            ErosionBodyHeadMaterialPool.Reset();
        }
    }

    //类职责：在当前游戏释放前清理其持有的凝砂族运行时图形资源。
    [HarmonyPatch(typeof(Game), nameof(Game.Dispose))]
    public static class Patch_NingshaGraphicsLifecycleDispose
    {
        //函数职责：在地图和世界销毁前执行统一图形资源清理。
        [HarmonyPrefix]
        public static void Prefix(Game __instance)
        {
            NingshaGraphicsLifecycle.ResetBeforeGameSwap(__instance);
        }
    }
}
