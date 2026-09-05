namespace NingshaRaceLib.GiantTomb.Layout
{
    //结构职责：保存本层候选评分，避免给每个临时候选单独分配对象。
    internal readonly struct GiantTombScoredCandidate
    {
        public readonly GiantTombSpatialCandidate Spatial;
        public readonly float Score;
        public readonly int Order;

        //职责：记录候选几何、纵深偏好与同分时使用的稳定顺序。
        public GiantTombScoredCandidate(GiantTombSpatialCandidate spatial, float score, int order)
        {
            Spatial = spatial;
            Score = score;
            Order = order;
        }

        //职责：优先排列高分候选，分数相同时按原始枚举顺序决胜。
        public static int Compare(GiantTombScoredCandidate left, GiantTombScoredCandidate right)
        {
            int score = right.Score.CompareTo(left.Score);
            return score != 0 ? score : left.Order.CompareTo(right.Order);
        }
    }
}
