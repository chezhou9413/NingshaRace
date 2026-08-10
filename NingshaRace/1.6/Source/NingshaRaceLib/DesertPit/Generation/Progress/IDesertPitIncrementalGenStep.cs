using System.Collections;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Progress
{
    //接口职责：允许耗时较长的沙漠巨坑生成步骤把工作拆成多个主线程帧执行。
    public interface IDesertPitIncrementalGenStep
    {
        //函数职责：分批修改地图，并在安全批次边界交还长事件更新帧。
        IEnumerable GenerateIncrementally(Map map, GenStepParams parms);
    }
}
