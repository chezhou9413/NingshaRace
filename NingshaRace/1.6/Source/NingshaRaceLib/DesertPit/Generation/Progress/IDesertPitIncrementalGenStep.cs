using NingshaRaceLib.PocketMaps.Generation;

namespace NingshaRaceLib.DesertPit.Generation.Progress
{
    //接口职责：允许耗时较长的沙漠巨坑生成步骤把工作拆成多个主线程帧执行。
    public interface IDesertPitIncrementalGenStep : INingshaIncrementalGenStep
    {
    }
}
