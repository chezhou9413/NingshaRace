using System;
using NingshaRaceLib.DesertPit.AntColony.Config;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.State
{
    //类职责：保存单个蚁群经过规模倍率结算后的补员上限与储藏格数量。
    public sealed class AntColonyPopulationSettings : IExposable
    {
        public float Scale = 1f;
        public int WorkerTarget;
        public int SoldierTarget;
        public int RegularAntCap;
        public int BoomAntCap;
        public int StorageCellCount;

        //函数职责：根据蚁穴基础配置和房间倍率计算可持久化的有效规模。
        public static AntColonyPopulationSettings Create(DefModExtension_AntColony settings, float scale)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (scale <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(scale), "蚁群规模必须大于零。");
            }

            int workers = Math.Max(0, (int)Math.Round(settings.workerTarget * scale, MidpointRounding.AwayFromZero));
            int soldiers = Math.Max(0, (int)Math.Round(settings.soldierTarget * scale, MidpointRounding.AwayFromZero));
            int regularCap = Math.Max(1, (int)Math.Round(settings.regularAntCap * scale, MidpointRounding.AwayFromZero));
            return new AntColonyPopulationSettings
            {
                Scale = scale,
                WorkerTarget = workers,
                SoldierTarget = soldiers,
                RegularAntCap = regularCap,
                BoomAntCap = Math.Max(0, (int)Math.Round(settings.boomAntCap * scale, MidpointRounding.AwayFromZero)),
                StorageCellCount = Math.Max(1, (int)Math.Round(settings.storageCellCount * scale, MidpointRounding.AwayFromZero))
            };
        }

        //函数职责：把蚁群有效规模写入地图存档并在读取时恢复。
        public void ExposeData()
        {
            Scribe_Values.Look(ref Scale, "scale", 1f);
            Scribe_Values.Look(ref WorkerTarget, "workerTarget");
            Scribe_Values.Look(ref SoldierTarget, "soldierTarget");
            Scribe_Values.Look(ref RegularAntCap, "regularAntCap");
            Scribe_Values.Look(ref BoomAntCap, "boomAntCap");
            Scribe_Values.Look(ref StorageCellCount, "storageCellCount");
        }
    }
}
