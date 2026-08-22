using System;
using NingshaRaceLib.DesertPit.AntColony.Config;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.State
{
    //类职责：保存单个蚁群经过规模倍率结算后的补员上限与储藏格数量。
    public sealed class AntColonyPopulationSettings : IExposable
    {
        //字段职责：保留固定规模场景使用的原始倍率或可升级蚁巢的当前等级。
        public float Scale = 1f;

        //字段职责：记录当前规模需要维持的工蚁数量。
        public int WorkerTarget;

        //字段职责：记录当前规模需要维持的兵蚁数量。
        public int SoldierTarget;

        //字段职责：记录完整警报允许存在的爆浆蚁上限。
        public int BoomAntCap;

        //字段职责：记录巢群使用的实体储藏格数量。
        public int StorageCellCount;

        //属性职责：始终从工蚁与兵蚁目标之和得到常规蚁上限，避免配置彼此矛盾。
        public int RegularAntCap => WorkerTarget + SoldierTarget;

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
            return new AntColonyPopulationSettings
            {
                Scale = scale,
                WorkerTarget = workers,
                SoldierTarget = soldiers,
                BoomAntCap = Math.Max(0, (int)Math.Round(settings.boomAntCap * scale, MidpointRounding.AwayFromZero)),
                StorageCellCount = Math.Max(1, (int)Math.Round(settings.storageCellCount * scale, MidpointRounding.AwayFromZero))
            };
        }

        //函数职责：按蚁巢等级建立四倍工蚁、三倍兵蚁且固定爆浆蚁和储藏格的规模配置。
        public static AntColonyPopulationSettings CreateForLevel(DefModExtension_AntColony settings, int level)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level), "蚁巢等级必须至少为一级。");
            }

            return new AntColonyPopulationSettings
            {
                Scale = level,
                WorkerTarget = settings.workerTarget * level,
                SoldierTarget = settings.soldierTarget * level,
                BoomAntCap = settings.boomAntCap,
                StorageCellCount = settings.storageCellCount
            };
        }

        //函数职责：把蚁群有效规模写入地图存档并在读取时恢复。
        public void ExposeData()
        {
            Scribe_Values.Look(ref Scale, "scale", 1f);
            Scribe_Values.Look(ref WorkerTarget, "workerTarget");
            Scribe_Values.Look(ref SoldierTarget, "soldierTarget");
            Scribe_Values.Look(ref BoomAntCap, "boomAntCap");
            Scribe_Values.Look(ref StorageCellCount, "storageCellCount");
        }
    }
}
