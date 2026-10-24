using System.Reflection;
using HarmonyLib;
using RimWorld.QuestGen;
using Verse;

namespace NingshaRaceLib.DesertPit.Quests
{
    //类职责：允许任务选择仅有地下殖民者的基地所绑定的地表地图。
    [HarmonyPatch(typeof(QuestNode_GetMap), "TryFindMap")]
    public static class DesertPitQuestMapSelectionPatch
    {
        private static readonly MethodInfo IsAcceptableMap = AccessTools.Method(typeof(QuestNode_GetMap), "IsAcceptableMap");

        //函数职责：当原版没有找到常驻殖民者地图时，仍按节点原有约束选择绑定地表。
        private static void Postfix(QuestNode_GetMap __instance, Slate slate, ref Map map, ref bool __result)
        {
            if (__result) return;
            foreach (Map home in Find.Maps)
            {
                if (!home.IsPlayerHome || home.Parent.def.defName != "NingshaRace_DesertPitHome"
                    || home.mapPawns.FreeColonists.Count == 0) continue;
                Map surface = DesertPitQuestDelivery.SurfaceFor(home);
                if (!(bool)IsAcceptableMap.Invoke(__instance, new object[] { surface, slate })) continue;
                map = surface;
                __result = true;
                return;
            }
        }
    }
}
