using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

using NingshaRaceLib.Scenarios.Generation;

namespace NingshaRaceLib.Scenarios.Parts
{
    //类职责：在地表生成期间准备开局物资，地图彻底完成后建立地下家园并直接安置队伍。
    public sealed class ScenPart_NingshaUndergroundArrival : ScenPart
    {
        //字段职责：仅在本次开局的两个生成阶段之间持有未落地物资，不作为场景配置或存档数据。
        private List<Thing> startingSupplies;

        //函数职责：开始一次开局时清空临时清单，防止同一场景对象复用上次开局的物资引用。
        public override void PreMapGenerate()
        {
            startingSupplies = null;
        }

        //函数职责：在原版场景生成阶段准备一次物资和动物，不提前投放到地表或创建地下地图。
        public override void GenerateIntoMap(Map map)
        {
            if (Find.GameInitData == null || map.IsPocketMap) return;
            if (startingSupplies != null)
                throw new InvalidOperationException("深砂遗民的开局物资不能重复准备。");
            startingSupplies = NingshaStartingSupplies.Generate();
        }

        //函数职责：串行生成连接地表的巨坑并安置开局队伍，避免嵌套生成破坏原版临时地图数据。
        public override void PostGameStart()
        {
            if (startingSupplies == null)
                throw new InvalidOperationException("深砂遗民缺少已准备的开局物资，请确认地表生成器包含原版场景生成步骤。");
            NingshaDesertPitStartUtility.CreateHome(Find.CurrentMap, startingSupplies);
            startingSupplies = null;
        }
    }
}
