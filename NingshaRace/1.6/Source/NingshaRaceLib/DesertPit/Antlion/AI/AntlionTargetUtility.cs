using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Antlion.Components;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.Antlion.AI
{
    //类职责：对地表和地下蚁狮执行一致的局部猎物筛选与不破门可达检查。
    internal static class AntlionTargetUtility
    {
        //函数职责：排除死亡、倒地、异图、机械体和同类，只接纳人类与动物。
        public static bool IsPrey(Pawn prey, Map map)
        {
            return prey != null && prey.Spawned && prey.Map == map && !prey.Dead && !prey.Downed
                && prey.def != DefOfRefs.NingshaRace_Antlion
                && !prey.RaceProps.IsMechanoid && (prey.RaceProps.Humanlike || prey.RaceProps.Animal);
        }

        //函数职责：按距离顺序扫描附近格子，不建立全图 Pawn 列表，不跨墙或关闭的门触发。
        public static Pawn FindNearest(Map map, IntVec3 origin, CompProperties_AntlionAmbush props,
            IntVec3 anchor, Pawn excluded = null)
        {
            int count = GenRadial.NumCellsInRadius(props.triggerRadius);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell = origin + GenRadial.RadialPattern[i];
                if (!cell.InBounds(map) || !WithinLeash(cell, anchor, props.chaseRadius)) continue;
                var things = cell.GetThingList(map);
                for (int j = 0; j < things.Count; j++)
                {
                    Pawn prey = things[j] as Pawn;
                    if (prey == excluded || !IsPrey(prey, map)) continue;
                    if (GenSight.LineOfSight(origin, cell, map, skipFirstCell: true)
                        && CanReach(map, origin, prey, PathEndMode.Touch)) return prey;
                }
            }
            return null;
        }

        //函数职责：检查猎物或下一步移动格是否仍在最初出土位置的追击边界内。
        public static bool WithinLeash(IntVec3 cell, IntVec3 anchor, float radius)
        {
            return anchor.IsValid && cell.DistanceToSquared(anchor) <= radius * radius;
        }

        //函数职责：使用禁止穿越关门与水域的原版连通性查询，不允许破坏障碍。
        public static bool CanReach(Map map, IntVec3 origin, LocalTargetInfo target, PathEndMode endMode)
        {
            return map.reachability.CanReach(origin, target, endMode,
                TraverseParms.For(TraverseMode.NoPassClosedDoorsOrWater, Danger.Deadly));
        }
    }
}
