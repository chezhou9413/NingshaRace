using System;
using Verse;

namespace NingshaRaceLib.GiantTomb.Generation
{
    //类职责：在墓葬批量写图期间延迟区域、房间和寻路更新，并在阶段结束时一次性恢复地图状态。
    internal sealed class GiantTombBulkMapUpdateScope : IDisposable
    {
        private readonly Map map;
        private readonly bool restoreRegions;
        private readonly bool restorePathing;
        private bool disposed;

        //函数职责：记录地图原状态并关闭会被逐格重复触发的昂贵更新。
        public GiantTombBulkMapUpdateScope(Map map)
        {
            this.map = map ?? throw new ArgumentNullException(nameof(map));
            restoreRegions = map.regionAndRoomUpdater.Enabled;
            restorePathing = !map.pathing.IncrementalDirtyingDisabled;
            if (restoreRegions) map.regionAndRoomUpdater.Enabled = false;
            if (restorePathing) map.pathing.DisableIncrementalDirtying();
        }

        //函数职责：一次性提交累计的寻路脏格并恢复区域与房间更新开关。
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try
            {
                if (restorePathing && map.pathing.IncrementalDirtyingDisabled) map.pathing.ReEnableIncrementalDirtying();
            }
            finally
            {
                if (restoreRegions) map.regionAndRoomUpdater.Enabled = true;
            }
        }
    }
}
