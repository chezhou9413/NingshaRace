using System;
using System.Collections.Generic;
using UnityEngine;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //在加载与导入边界阻止缺方向、倒序时间轴和非有限数值。
    internal static class MeleeAnimationValidation
    {
        internal static readonly string[] Facings = { "South", "East", "West", "North" };
        //错误包含动作、方向与字段，美工可定位到同一面板。
        internal static IEnumerable<string> Errors(List<MeleeAnimationMove> moves)
        {
            if (moves == null || moves.Count == 0) { yield return "moves：至少需要一套动作。"; yield break; }
            var ids = new HashSet<string>();
            float weight = 0;
            foreach (MeleeAnimationMove move in moves)
            {
                if (string.IsNullOrWhiteSpace(move.id) || !ids.Add(move.id)) yield return "动作 id 为空或重复：" + move.id;
                string prefix = "动作「" + move.label + "」";
                foreach (var field in typeof(MeleeAnimationMove).GetFields())
                    if (field.GetValue(move) is float f && !Finite(f)) yield return prefix + field.Name + "：必须是有限数字。";
                if (!Range(move.weight, 0, 100)) yield return prefix + "weight：范围 0～100。";
                weight += move.weight;
                if (!Range(move.duration, 0.25f, 2)) yield return prefix + "duration：范围 0.25～2 秒。";
                if (!Range(move.contact, 0.05f, 0.85f)) yield return prefix + "contact：范围 0.05～0.85。";
                if (!Range(move.hitstop, 0, 0.35f)) yield return prefix + "hitstop：范围 0～0.35 秒。";
                if (move.strikeCount < 1 || move.strikeCount > 3) yield return prefix + "strikeCount：范围 1～3 次。";
                if (!Range(move.strikeInterval, 0.06f, 0.5f)) yield return prefix + "strikeInterval：范围 0.06～0.5 秒。";
                if (move.strikeCount > 1 && (!Range(move.LastContact, move.contact, 0.9f) || move.hitstop != 0))
                    yield return prefix + "连击最后一次接触必须在动作进度 0.9 以内，且 hitstop 必须为零。";
                if (!Range(move.effectSpeed, 0, 3)) yield return prefix + "effectSpeed：范围 0～3。";
                if (!Range(move.chargeIntensity, 0, 2) || !Range(move.chargeFlash, 0, 3) || !Range(move.chargeScale, 0.5f, 2))
                    yield return prefix + "蓄力亮度、蓄满闪光或提示大小超出范围。";
                if (!Range(move.chargeReady, 0.01f, 0.85f) || move.chargeIntensity > 0 && move.chargeReady >= move.contact)
                    yield return prefix + "chargeReady：范围 0.01～0.85；启用提示时必须早于首次接触点。";
                if (!Range(move.trailLifetime, 0.04f, 0.6f) || !Range(move.trailWidth, 0.1f, 1.5f)
                    || !Range(move.trailIntensity, 0, 3) || !Range(move.flameDistortion, 0, 2) || !Range(move.bloom, 0, 2))
                    yield return prefix + "刀光寿命、宽度、亮度、扰动或辉光超出范围。";
                if (move.sparks < 0 || move.sparks > 48 || !Range(move.sparkLifetime, 0.1f, 1.5f)
                    || !Range(move.sparkSpeed, 0, 1.5f) || !Range(move.sparkSize, 0.003f, 0.05f))
                    yield return prefix + "火屑数量、寿命、速度或大小超出范围。";
                if (!Range(move.color.r, 0, 1) || !Range(move.color.g, 0, 1) || !Range(move.color.b, 0, 1) || !Range(move.color.a, 0, 1))
                    yield return prefix + "color：颜色通道范围为 0～1。";
                var seen = new HashSet<string>();
                foreach (MeleeAnimationFacing face in move.directions)
                {
                    string where = prefix + " / " + face.facing;
                    if (Array.IndexOf(Facings, face.facing) < 0 || !seen.Add(face.facing)) yield return where + "：方向无效或重复。";
                    if (!VectorRange(face.offset, 0.5f) || !Range(face.angle, -90, 90) || !Range(face.layer, -10, 100))
                        yield return where + "：方向偏移、角度或层级超出范围。";
                    if (face.keys.Count < 3 || face.keys[0].time != 0 || face.keys[face.keys.Count - 1].time != 1)
                        yield return where + "：至少三个关键帧，首尾时间必须为 0 和 1。";
                    float previous = -1;
                    foreach (MeleeAnimationKey key in face.keys)
                    {
                        if (!Range(key.time, 0, 1) || key.time <= previous) yield return where + " / time：关键帧必须严格递增。";
                        previous = key.time;
                        if (!Range(key.angle, -270, 270) || !Range(key.lean, -12, 12) || !Range(key.trail, 0, 1)
                            || !VectorRange(key.position, 0.6f) || !VectorRange(key.body, 0.2f))
                            yield return where + " / " + key.time + "：姿态或刀光参数超出范围。";
                    }
                    if (face.keys.Count > 0 && (!Neutral(face.keys[0]) || !Neutral(face.keys[face.keys.Count - 1])))
                        yield return where + "：首尾姿态必须归零，避免持刀跳变。";
                }
                foreach (string facing in Facings) if (!seen.Contains(facing)) yield return prefix + "缺少方向 " + facing;
            }
            if (weight <= 0) yield return "至少一套动作的权重必须大于零。";
        }
        //首尾回到配置的持刀姿态。
        private static bool Neutral(MeleeAnimationKey k) => k.angle == 0 && k.lean == 0 && k.trail == 0 && k.position == Vector2.zero && k.body == Vector2.zero;
        //统一有限数值和范围检查。
        private static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
        private static bool Range(float f, float min, float max) => Finite(f) && f >= min && f <= max;
        private static bool VectorRange(Vector2 v, float limit) => Range(v.x, -limit, limit) && Range(v.y, -limit, limit);
    }
}
