using System.Linq;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //每个 Pawn 独立保存地图和编辑器时钟，运行时接触点与原版攻击释放同步。
    internal sealed class MeleeAnimationPlayback
    {
        internal MeleeAnimationMove Move;
        internal Rot4 Facing;
        internal float Elapsed, Duration, Stop, AimAngle;
        internal int Sequence;
        private float started;
        private string lastMoveId, previousMoveId;
        internal bool Active => Move != null && Elapsed <= Duration + Stop + Mathf.Max(Move.trailLifetime, Move.sparkLifetime);
        internal float MotionTime => MotionAt(Elapsed);
        internal float VisualTime => Elapsed - Mathf.Clamp(Elapsed - Move.contact * Duration, 0, Stop);
        internal MeleeAnimationFacing Direction => Move.For(Facing);
        internal MeleeAnimationPose Pose => Active ? Direction.Evaluate(MotionTime / Duration) : default;

        //动作充足时避开最近两招，让不同轨迹轮流出现。
        internal void Attack(CompMeleeAnimation owner, float aimAngle, float cooldown, bool contact)
        {
            var available = owner.Definition.moves.Where(m => m.weight > 0).ToList();
            if (available.Count > 2) available.RemoveAll(m => m.id == lastMoveId || m.id == previousMoveId);
            else if (available.Count > 1) available.RemoveAll(m => m.id == lastMoveId);
            MeleeAnimationMove move;
            //外观选招不消耗游戏随机序列，避免影响后续战斗判定。
            Rand.PushState(Gen.HashCombineInt(owner.Pawn.thingIDNumber, Find.TickManager.TicksGame));
            try { move = available.RandomElementByWeight(m => m.weight); }
            finally { Rand.PopState(); }
            previousMoveId = lastMoveId; lastMoveId = move.id;
            owner.Trail.PreserveTail(this);
            Start(move, owner.Pawn.Rotation, Find.TickManager.TicksGame / 60f, contact, cooldown);
            AimAngle = aimAngle;
        }

        //单次攻击适配原版冷却；连击保留配置的前摇与出刺间隔，不被攻速压缩反应时间。
        internal void Start(MeleeAnimationMove move, Rot4 facing, float now, bool contact, float cooldown = 10)
        {
            Move = move; Facing = facing; started = now; Elapsed = 0; Sequence++;
            float budget = Mathf.Max(0.1f, cooldown);
            Stop = contact && move.strikeCount == 1 ? Mathf.Min(move.hitstop, budget * 0.35f) : 0;
            Duration = move.strikeCount > 1 ? move.duration : Mathf.Min(move.duration, budget - Stop);
            AimAngle = facing.AsAngle;
        }

        //时钟只在游戏 Tick 或窗口更新时推进，渲染树并行准备只读取姿态。
        internal void Advance(float now) { if (Move != null) Elapsed = Mathf.Max(0, now - started); }

        //命中时短暂停住姿态和刀光采样，随后接回原曲线。
        internal float MotionAt(float elapsed)
        {
            float impact = Move.contact * Duration;
            return Mathf.Clamp(elapsed - Mathf.Clamp(elapsed - impact, 0, Stop), 0, Duration);
        }

        //拖动预览时间轴后仍可从该位置继续播放。
        internal void Seek(float time, float now) { Elapsed = time; started = now - time; }

        //上一招只保留特效所需的时间和方向，不继续影响当前武器或身体。
        internal void CopyVisualFrom(MeleeAnimationPlayback source)
        {
            Move = source.Move; Facing = source.Facing; Elapsed = source.Elapsed; Duration = source.Duration;
            Stop = source.Stop; AimAngle = source.AimAngle; Sequence = source.Sequence; started = source.started;
        }

        //失去装备、转向或离图后不保留上一次动作。
        internal void Clear() { Move = null; Elapsed = 0; }
    }
}
