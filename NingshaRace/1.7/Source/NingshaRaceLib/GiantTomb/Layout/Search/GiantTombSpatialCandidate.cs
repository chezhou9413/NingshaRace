using Verse;

namespace NingshaRaceLib.GiantTomb.Layout
{
    //结构职责：保存出口候选的固定地图几何，避免每层回溯重复平移和构造矩形。
    internal readonly struct GiantTombSpatialCandidate
    {
        public readonly GiantTombPlacementVariant Variant;
        public readonly IntVec3 Origin;
        public readonly CellRect Bounds;
        public readonly CellRect BufferedBounds;

        //职责：一次性计算候选矩形与非父房间必须避让的一格缓冲矩形。
        public GiantTombSpatialCandidate(GiantTombPlacementVariant variant, IntVec3 origin)
        {
            Variant = variant;
            Origin = origin;
            Bounds = new CellRect(origin.x, origin.z, variant.Prototype.Size.x, variant.Prototype.Size.z);
            BufferedBounds = Bounds.ExpandedBy(1);
        }
    }
}
