namespace NingshaRaceLib.DesertPit.Generation.CaveBase;

//实验台 v14 的三个固定预设；宽度使用地图格，几何使用归一化坐标。
internal sealed class CaveSettings
{
    internal readonly string Seed, Template;
    internal readonly int Size, Chambers, Pillars;
    internal readonly double Loops, WaterWidth, Banks;
    internal double TunnelWidth = 3, Meander = 1.2;
    internal const double Roughness = 1, TunnelVariation = 1.2, RiverVariation = 1.1;

    //保留三个模板各自的洞室数量、水宽和岩柱密度。
    internal CaveSettings(string seed, int size, string template)
    {
        Seed = seed; Size = size; Template = template;
        Chambers = template == "confluence" ? 30 : 25;
        Pillars = template == "lake" ? 11 : template == "confluence" ? 17 : 12;
        Loops = template == "lake" ? .12 : template == "confluence" ? .2 : .16;
        WaterWidth = template == "braided" ? 6 : 5;
        Banks = template == "lake" ? 4 : 3;
    }
}
