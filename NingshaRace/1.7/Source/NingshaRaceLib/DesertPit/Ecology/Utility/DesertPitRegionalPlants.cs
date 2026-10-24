using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Utility;

//集中定义各群落的物种和相对数量，生成与补生共用同一份边界规则。
internal static class DesertPitRegionalPlants
{
    private const string Pit = "NingshaRace_DesertPit";
    private static readonly string[] Names = { Pit + "PlantA", Pit + "PlantB", Pit + "PlantC", Pit + "PlantD", Pit + "PlantE",
        "Glowstool", "Bryolux", "Agarilux", "NingshaRace_UndergroundGrass", Pit + "Cecropia", Pit + "Palm", "Plant_Bush",
        "NingshaRace_GiantTimberMushroom", "NingshaRace_DroopingGlowMushroom" };
    //依次为深暗、灰暗、灰暗外缘、亮区、亮区边缘、绿洲腹地、湿岸、疏草边缘、绿洲背光地。
    private static readonly int[][] Weights = {
        new int[14],
        new[] { 30, 6, 16, 0, 0, 20, 24, 4, 0, 0, 0, 0, 3, 1 },
        new[] { 7, 12, 10, 42, 2, 5, 3, 19, 0, 0, 0, 0, 5, 3 },
        new[] { 20, 12, 18, 26, 0, 8, 8, 8, 0, 0, 0, 0, 4, 2 },
        new[] { 0, 10, 10, 0, 20, 0, 0, 0, 40, 4, 3, 13, 2, 3 },
        new[] { 5, 14, 14, 4, 20, 0, 0, 0, 33, 2, 2, 6, 3, 3 },
        new[] { 0, 2, 8, 0, 14, 0, 0, 0, 49, 5, 5, 17, 3, 4 },
        new[] { 3, 2, 18, 2, 12, 0, 0, 0, 48, 7, 2, 6, 2, 4 },
        new[] { 3, 5, 7, 0, 14, 0, 0, 0, 59, 1, 2, 9, 2, 3 },
        new[] { 24, 12, 32, 10, 16, 3, 1, 2, 0, 0, 0, 0, 5, 6 }
    };
    private static ThingDef[] definitions;
    internal static IReadOnlyList<ThingDef> Definitions => definitions ??= Names.Select(name => DefDatabase<ThingDef>.GetNamed(name)).ToArray();

    //森林密度通过较低树木权重和单独间距控制，常规植被使用连续噪声形成斑块。
    internal static float Density(byte region) => region switch
    {
        1 => .13f, 2 => .29f, 3 => .20f, 4 => .32f, 5 => .23f,
        6 => .50f, 7 => .58f, 8 => .24f, 9 => .30f, _ => 0f
    };

    //同一群落仅允许其物种表内的植物补生。
    internal static bool BelongsTo(ThingDef plant, byte region)
    {
        if (region == 0) return false;
        for (int i = 0; i < Definitions.Count; i++)
            if (Definitions[i] == plant) return Weights[region][i] > 0;
        return false;
    }

    //从当前群落中筛选能实际生长的物种后抽取，避免暗处抽到树木便留下大量空洞。
    internal static ThingDef Choose(byte region, bool trees, Func<ThingDef, bool> canGrow)
    {
        var choices = new List<(ThingDef Def, int Weight)>();
        for (int i = 0; i < Definitions.Count; i++)
            if (Weights[region][i] > 0 && Definitions[i].plant.IsTree == trees && canGrow(Definitions[i]))
                choices.Add((Definitions[i], Weights[region][i]));
        if (choices.Count == 0) return null;
        int roll = Rand.Range(0, choices.Sum(c => c.Weight));
        foreach (var choice in choices)
        {
            roll -= choice.Weight;
            if (roll < 0) return choice.Def;
        }
        throw new InvalidOperationException("地底植物权重抽取越界。");
    }
}
