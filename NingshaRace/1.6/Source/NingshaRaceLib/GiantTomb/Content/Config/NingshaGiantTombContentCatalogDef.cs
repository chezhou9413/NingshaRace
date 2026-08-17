using System.Collections.Generic;
using ChezhouLib.CustomMission.MapTemplates;
using NingshaRaceLib.GiantTomb.Config;
using Verse;

namespace NingshaRaceLib.GiantTomb.Content.Config
{
    //类职责：汇总五类房间内容Def并保证布局中的十九种模板各覆盖一次。
    public sealed class NingshaGiantTombContentCatalogDef : Def
    {
        public NingshaGiantTombLayoutDef layoutDef;
        public List<NingshaGiantTombRoomContentDef> roomContents = new List<NingshaGiantTombRoomContentDef>();

        //函数职责：验证内容分类数量以及模板覆盖完整性与唯一性。
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }
            if (layoutDef == null)
            {
                yield return defName + ": layoutDef不能为空";
                yield break;
            }
            if (roomContents == null || roomContents.Count != 5)
            {
                yield return defName + ": roomContents必须包含墓室、大墓室、大厅、储藏室和走廊五项";
                yield break;
            }

            if (layoutDef.modules == null)
            {
                yield return defName + ": layoutDef.modules不能为空";
                yield break;
            }
            HashSet<ClMapTemplateDef> expected = new HashSet<ClMapTemplateDef>(layoutDef.modules);
            HashSet<ClMapTemplateDef> covered = new HashSet<ClMapTemplateDef>();
            for (int i = 0; i < roomContents.Count; i++)
            {
                NingshaGiantTombRoomContentDef content = roomContents[i];
                if (content == null)
                {
                    yield return defName + ": roomContents[" + i + "]不能为空";
                    continue;
                }
                if (content.rooms == null)
                {
                    yield return defName + ": " + content.defName + ".rooms不能为空";
                    continue;
                }
                for (int j = 0; j < content.rooms.Count; j++)
                {
                    ClMapTemplateDef template = content.rooms[j]?.template;
                    if (template == null)
                    {
                        continue;
                    }
                    if (!expected.Contains(template))
                    {
                        yield return defName + ": 内容表引用了布局外模板: " + template.defName;
                    }
                    else if (!covered.Add(template))
                    {
                        yield return defName + ": 模板被重复覆盖: " + template.defName;
                    }
                }
            }
            foreach (ClMapTemplateDef template in expected)
            {
                if (!covered.Contains(template))
                {
                    yield return defName + ": 模板缺少内容配置: " + template.defName;
                }
            }
        }
    }
}
