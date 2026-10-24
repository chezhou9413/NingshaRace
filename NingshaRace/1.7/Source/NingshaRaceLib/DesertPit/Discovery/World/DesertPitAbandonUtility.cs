using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Discovery.World
{
    //把地表和各层口袋地图视为一个可放弃的巨坑地点。
    [StaticConstructorOnStartup]
    internal static class DesertPitAbandonUtility
    {
        private static readonly Texture2D AbandonIcon = ContentFinder<Texture2D>.Get("UI/Commands/AbandonHome");

        //显示放弃按钮，并按整个巨坑检查是否会失去全部殖民者。
        public static Command CreateCommand(WorldObject_DesertPitDiscovery site)
        {
            Command_Action command = new Command_Action
            {
                defaultLabel = "放弃沙漠巨坑",
                defaultDesc = "放弃此处地表和所有关联地下地图，移除世界标记。留在其中的人员与物资将被遗弃。",
                icon = AbandonIcon,
                Order = 3000f,
                action = () => ConfirmAbandon(site)
            };
            List<Map> maps = CollectMaps(site);
            if (maps.Count > 0 && !CaravanUtility.PlayerHasAnyCaravan()
                && !Find.Maps.Any(map => !maps.Contains(map) && map.mapPawns.FreeColonistsSpawned.Any()))
                command.Disable("CommandAbandonHomeFailAllColonistsThere".Translate());
            return command;
        }

        //确认窗口同时列出地表和深层墓室中将被遗弃的人员。
        private static void ConfirmAbandon(WorldObject_DesertPitDiscovery site)
        {
            List<Pawn> pawns = CollectMaps(site).SelectMany(map => map.mapPawns.AllPawns).Distinct().ToList();
            StringBuilder text = new StringBuilder("确定放弃沙漠巨坑吗？地表、所有关联地下地图及其中的物资会被清除，世界地图上的标记也会消失。");
            List<Pawn> owned = pawns.Where(pawn => pawn.Faction == Faction.OfPlayer || pawn.HostFaction == Faction.OfPlayer).ToList();
            if (owned.Count > 0)
            {
                text.Append("\n\n以下人员或动物仍在巨坑中，将被遗弃：");
                foreach (Pawn pawn in owned) text.Append("\n    " + pawn.LabelCap);
            }
            PawnDiedOrDownedThoughtsUtility.BuildMoodThoughtsListString(pawns,
                PawnDiedOrDownedThoughtsKind.Banished, text, null,
                "\n\n" + "ConfirmAbandonHomeNegativeThoughts_Everyone".Translate(), "ConfirmAbandonHomeNegativeThoughts");
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text.ToString(), () =>
            {
                site.Abandon(false);
                Find.GameEnder.CheckOrUpdateGameOver();
            }));
        }

        //沿来源地图收集各层地下地图，顺序保证父地图排在子地图前。
        private static List<Map> CollectMaps(WorldObject_DesertPitDiscovery site)
        {
            List<Map> maps = new List<Map>();
            if (!site.HasMap) return maps;
            maps.Add(site.Map);
            for (int i = 0; i < maps.Count; i++)
                foreach (PocketMapParent parent in Find.World.pocketMaps)
                    if (parent.sourceMap == maps[i] && parent.HasMap && !maps.Contains(parent.Map))
                        maps.Add(parent.Map);
            return maps;
        }

        //从最深层开始移除，避免原版父地图清理再次处理子地图。
        public static void RemoveUndergroundMaps(WorldObject_DesertPitDiscovery site)
        {
            List<Map> maps = CollectMaps(site);
            for (int i = maps.Count - 1; i > 0; i--)
            {
                Map map = maps[i];
                foreach (Thing thing in map.listerThings.AllThings.ToList()) thing.Notify_LeftBehind();
                PocketMapUtility.DestroyPocketMap(map);
            }
            Find.World.renderer.wantedMode = WorldRenderMode.Planet;
        }
    }
}
