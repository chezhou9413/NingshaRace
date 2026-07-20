using System.Collections.Generic;
using HarmonyLib;
using NingshaRaceLib.SandGolem;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Patches
{
    //类职责：把玩家沙傀追加进原版工作面板，让沙傀使用原版工作优先级 UI。
    public static class Patch_SandGolemWorkTab
    {
        //类职责：扩展原版工作窗口的 Pawn 来源。
        [HarmonyPatch(typeof(MainTabWindow_Work), "Pawns", MethodType.Getter)]
        public static class Patch_MainTabWindow_Work_Pawns
        {
            //函数职责：在原版自由殖民者后追加当前地图可工作的玩家沙傀。
            public static void Postfix(ref IEnumerable<Pawn> __result)
            {
                __result = WithSandGolems(__result);
            }
        }

        //函数职责：合并原版 Pawn 枚举和当前地图玩家沙傀。
        private static IEnumerable<Pawn> WithSandGolems(IEnumerable<Pawn> original)
        {
            HashSet<Pawn> yielded = new HashSet<Pawn>();
            if (original != null)
            {
                foreach (Pawn pawn in original)
                {
                    if (pawn == null || !yielded.Add(pawn))
                    {
                        continue;
                    }

                    yield return pawn;
                }
            }

            Map map = Find.CurrentMap;
            if (map == null)
            {
                yield break;
            }

            List<Pawn> playerPawns = map.mapPawns.PawnsInFaction(Faction.OfPlayer);
            for (int i = 0; i < playerPawns.Count; i++)
            {
                Pawn pawn = playerPawns[i];
                if (!IsPlayerSandGolem(pawn) || !yielded.Add(pawn))
                {
                    continue;
                }

                SandGolemUtility.EnsurePlayerControlComponents(pawn);
                if (CanShowInWorkTab(pawn))
                {
                    yield return pawn;
                }
            }
        }

        //函数职责：判断 Pawn 是否是当前地图玩家阵营沙傀。
        private static bool IsPlayerSandGolem(Pawn pawn)
        {
            return SandGolemUtility.IsSandGolem(pawn)
                && pawn.Faction == Faction.OfPlayer
                && !pawn.Dead
                && !pawn.DevelopmentalStage.Baby();
        }

        //函数职责：判断沙傀是否已经具备原版工作表绘制所需的运行时组件。
        private static bool CanShowInWorkTab(Pawn pawn)
        {
            return pawn.skills != null
                && pawn.workSettings != null
                && pawn.workSettings.EverWork;
        }
    }
}
