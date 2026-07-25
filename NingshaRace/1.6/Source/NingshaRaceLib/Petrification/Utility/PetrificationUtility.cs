using System;
using System.Runtime.CompilerServices;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Core.Effects;
using NingshaRaceLib.Petrification.Health;
using NingshaRaceLib.Petrification.Patches;
using NingshaRaceLib.Petrification.Rendering;
using NingshaRaceLib.SandGolem.Utility;

namespace NingshaRaceLib.Petrification.Utility
{
    //类职责：为外部战斗和能力系统提供石化累计、查询与完全石化状态缓存。
    public static class PetrificationUtility
    {
        //字段职责：以弱引用缓存完全石化 Pawn 对应的 Hediff，避免全局热路径反复扫描健康列表。
        private static readonly ConditionalWeakTable<Pawn, Hediff_Petrification> FullyPetrifiedPawns =
            new ConditionalWeakTable<Pawn, Hediff_Petrification>();

        //函数职责：为存活血肉 Pawn 创建或取得石化 Hediff，并累计指定正严重度。
        public static Hediff_Petrification AddSeverity(Pawn pawn, float amount)
        {
            if (pawn == null)
            {
                throw new ArgumentNullException(nameof(pawn));
            }
            if (pawn.Dead)
            {
                throw new InvalidOperationException("不能给死亡 Pawn 累计石化严重度。");
            }
            if (!pawn.RaceProps.IsFlesh)
            {
                throw new InvalidOperationException("石化只能施加给血肉 Pawn。");
            }
            if (amount <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "石化严重度增量必须大于零。");
            }

            Hediff_Petrification petrification = GetPetrification(pawn);
            if (petrification == null)
            {
                petrification = (Hediff_Petrification)HediffMaker.MakeHediff(DefOfRefs.NingshaRace_Petrification, pawn);
                pawn.health.AddHediff(petrification);
            }
            petrification.Severity += amount;
            return petrification;
        }

        //函数职责：通过运行期登记表判断 Pawn 当前是否处于完全石化锁定期，避免渲染线程读取健康状态。
        public static bool IsFullyPetrified(Pawn pawn)
        {
            return pawn != null
                && FullyPetrifiedPawns.TryGetValue(pawn, out _);
        }

        //函数职责：取得 Pawn 身上的石化实例，不存在时返回空值。
        public static Hediff_Petrification GetPetrification(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetFirstHediffOfDef(DefOfRefs.NingshaRace_Petrification)
                as Hediff_Petrification;
        }

        //函数职责：登记进入完全石化的 Pawn，并替换同一 Pawn 可能残留的旧缓存项。
        internal static void RegisterFullyPetrified(Pawn pawn, Hediff_Petrification petrification)
        {
            if (pawn == null || petrification == null)
            {
                return;
            }

            FullyPetrifiedPawns.Remove(pawn);
            FullyPetrifiedPawns.Add(pawn, petrification);
        }

        //函数职责：在石化解除或 Pawn 死亡时移除完全石化缓存。
        internal static void UnregisterFullyPetrified(Pawn pawn)
        {
            if (pawn != null)
            {
                FullyPetrifiedPawns.Remove(pawn);
            }
        }
    }
}
