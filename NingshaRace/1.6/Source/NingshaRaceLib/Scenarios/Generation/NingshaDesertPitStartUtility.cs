using System;
using RimWorld;
using RimWorld.Planet;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Buildings;

namespace NingshaRaceLib.Scenarios.Generation
{
    //类职责：协调地表巨坑入口、地下家园与开局安置，确保两张地图拥有真实的双向连接。
    internal static class NingshaDesertPitStartUtility
    {
        //函数职责：地表生成结束后创建地下地图，完成队伍安置并把镜头定位到成员身边。
        public static void CreateHome(Map surface)
        {
            if (surface == null || surface.IsPocketMap || surface.Biome != BiomeDefOf.Desert)
                throw new InvalidOperationException("深砂遗民开局缺少有效的地表沙漠地图。");
            if (Find.GameInitData == null || Find.GameInitData.startingAndOptionalPawns.Count != 3)
                throw new InvalidOperationException("深砂遗民开局必须有三名待安置成员。");
            if (MapGenerator.mapBeingGenerated != null || PocketMapUtility.currentlyGeneratingPortal != null)
                throw new InvalidOperationException("深砂遗民的地下家园不能嵌套在其他地图生成流程中创建。");

            Building_DesertPitGate gate = NingshaStartingGatePlacement.Spawn(surface);
            PocketMapParent parent = (PocketMapParent)WorldObjectMaker.MakeWorldObject(DefOfRefs.NingshaRace_DesertPitHome);
            parent.sourceMap = surface;
            parent.Tile = surface.Tile;
            parent.mapGenerator = DefOfRefs.NingshaRace_DesertPitStartingMap;
            parent.SetFaction(Faction.OfPlayer);
            Map underground;
            PocketMapUtility.currentlyGeneratingPortal = gate;
            try
            {
                //与地表先后生成，地下只读取自己的生态与恒温配置，不继承地表地貌生成步骤。
                underground = MapGenerator.GenerateMap(surface.Size, parent, parent.mapGenerator, isPocketMap: true);
            }
            finally
            {
                PocketMapUtility.currentlyGeneratingPortal = null;
            }

            Find.World.pocketMaps.Add(parent);
            if (gate.exit == null || gate.exit.Map != underground || gate.exit.entrance != gate)
                throw new InvalidOperationException("深砂遗民开局未生成正确绑定的离洞绳。");
            gate.BindStartingHome(underground);
            NingshaStartingPartyPlacement.Place(underground, gate.exit);
            Current.Game.CurrentMap = underground;
            Find.CameraDriver.JumpToCurrentMapLoc(Find.GameInitData.startingAndOptionalPawns[0].Position);
            Find.CameraDriver.ResetSize();
            Find.ColonistBar.MarkColonistsDirty();
            Log.Message("[NingshaRace] 深砂遗民开局完成：三名成员已安置在沙漠巨坑内部，离洞绳与地表入口双向连接。");
        }
    }
}
