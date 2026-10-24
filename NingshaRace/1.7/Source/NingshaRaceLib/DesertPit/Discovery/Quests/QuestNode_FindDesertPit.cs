using NingshaRaceLib.DesertPit.Discovery.World;
using RimWorld.QuestGen;

namespace NingshaRaceLib.DesertPit.Discovery.Quests
{
    //类职责：把物品提供的巨坑地点包装为自动接受的寻找任务。
    public class QuestNode_FindDesertPit : QuestNode
    {
        //函数职责：确认任务已有明确的巨坑目标，禁止随机生成无目标任务。
        protected override bool TestRunInt(Slate slate)
        {
            return slate.Get<WorldObject_DesertPitDiscovery>("desertPitDestination") != null;
        }

        //函数职责：绑定永久地点并设置任务名称、目标和说明。
        protected override void RunInt()
        {
            WorldObject_DesertPitDiscovery destination = QuestGen.slate.Get<WorldObject_DesertPitDiscovery>("desertPitDestination");
            QuestGen.quest.AddPart(new QuestPart_FindDesertPit { destination = destination });
            QuestGen.slate.Set("resolvedQuestName", "寻找巨坑");
            QuestGen.slate.Set("resolvedQuestDescription", "你从巨坑坐标地图上辨认出一处沙漠巨坑的位置。派出远行队前往标记地点，抵达地表即可完成寻找。\n\n"
                + "地表地图尺寸：" + destination.surfaceSize.x + " × " + destination.surfaceSize.z
                + "。通过地表巨坑入口可以继续探索地下洞室。\n\n此任务没有期限。探索地点与已生成的地图将永久保留，可反复前往。");
        }
    }
}
