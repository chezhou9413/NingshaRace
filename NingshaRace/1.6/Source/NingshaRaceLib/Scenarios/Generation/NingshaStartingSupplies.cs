using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Scenarios.Generation
{
    //类职责：在原版开局生成上下文有效时建立物资与动物清单，交给地下安置阶段使用。
    internal static class NingshaStartingSupplies
    {
        //函数职责：完整执行场景和个人物资生成器，保留随机动物的驯养、信仰权重及羁绊规则。
        public static List<Thing> Generate()
        {
            GameInitData initData = Find.GameInitData;
            if (initData == null || initData.playerFaction == null)
                throw new InvalidOperationException("深砂遗民的开局物资必须在临时玩家派系仍有效的地图生成阶段准备。");

            List<Thing> supplies = new List<Thing>();
            //立即枚举生成结果，不能把依赖临时派系的动物生成器延迟到开局完成阶段。
            foreach (ScenPart part in Find.Scenario.AllParts)
                supplies.AddRange(part.PlayerStartingThings());
            foreach (Pawn pawn in initData.startingAndOptionalPawns)
                foreach (ThingDefCount possession in initData.startingPossessions[pawn])
                    supplies.Add(StartingPawnUtility.GenerateStartingPossession(possession));
            return supplies;
        }
    }
}
