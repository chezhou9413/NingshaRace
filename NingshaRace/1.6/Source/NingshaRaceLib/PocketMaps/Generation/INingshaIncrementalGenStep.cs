using System.Collections;
using Verse;

namespace NingshaRaceLib.PocketMaps.Generation
{
    //接口职责：允许耗时地图生成步骤在主线程安全批次之间交还画面帧。
    public interface INingshaIncrementalGenStep
    {
        //函数职责：分批修改地图，并在每个完整批次结束后交还当前画面帧。
        IEnumerable GenerateIncrementally(Map map, GenStepParams parms);
    }
}
