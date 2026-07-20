using LudeonTK;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DevTools
{
    //类职责：向 RimWorld 开发者菜单注册凝砂地图摆放工具入口。
    public static class NingshaDevPlacementActions
    {
        //函数职责：打开凝砂开发者摆放工具窗口。
        [DebugAction("NingshaRace", "打开凝砂摆放工具", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void OpenPlacementDialog()
        {
            if (Find.CurrentMap == null)
            {
                Messages.Message("当前没有可摆放的地图。", MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            Find.WindowStack.Add(new Dialog_NingshaDevPlacement());
        }
    }
}
