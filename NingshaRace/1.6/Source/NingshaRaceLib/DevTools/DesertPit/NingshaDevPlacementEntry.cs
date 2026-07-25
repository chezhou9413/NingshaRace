using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Buildings;
using NingshaRaceLib.DesertPit.Generation.Caves;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Landmarks;
using NingshaRaceLib.DesertPit.Generation.Steps;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DevTools.DesertPit
{
    //类职责：描述凝砂开发者摆放工具中的一个可摆放地形或物件条目。
    public class NingshaDevPlacementEntry
    {
        //字段职责：记录工具窗口中显示的分类名称。
        public readonly string Category;

        //字段职责：记录工具窗口中显示的按钮名称。
        public readonly string Label;

        //字段职责：记录要从 DefDatabase 读取的 DefName。
        public readonly string DefName;

        //字段职责：标记当前条目是否为地形 Def。
        public readonly bool IsTerrain;

        //构造函数职责：初始化一个开发者摆放条目的分类、显示名、DefName 和 Def 类型。
        public NingshaDevPlacementEntry(string category, string label, string defName, bool isTerrain)
        {
            Category = category;
            Label = label;
            DefName = defName;
            IsTerrain = isTerrain;
        }

        //函数职责：读取当前条目对应的可摆放 Def。
        public BuildableDef ResolveDef()
        {
            if (IsTerrain)
            {
                return DefDatabase<TerrainDef>.GetNamed(DefName);
            }

            return DefDatabase<ThingDef>.GetNamed(DefName);
        }
    }
}
