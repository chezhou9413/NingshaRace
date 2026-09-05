using System;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Race.Generation
{
    //类职责：为凝砂专用儿童种类提供相容的发育阶段和有界年龄，不改变全种族年龄曲线。
    internal static class NingshaChildGenerationUtility
    {
        //职责：协调儿童种类的默认成年请求，保留孵化固定年龄与明确的婴儿、新生儿请求。
        public static void PrepareRequest(ref PawnGenerationRequest request)
        {
            if (request.KindDef == null || request.KindDef != DefOfRefs.NingshaRace_Child) return;
            if (request.AllowedDevelopmentalStages.Newborn() || request.AllowedDevelopmentalStages.Baby()) return;
            if (request.AllowedDevelopmentalStages == DevelopmentalStage.Adult)
                request.AllowedDevelopmentalStages = DevelopmentalStage.Child;
            if (request.FixedBiologicalAge.HasValue) return;

            request.FixedBiologicalAge = ChooseAge(request);
            //区间限制已参与抽样，转为固定年龄后清除区间，满足原版请求校验的互斥约束。
            request.BiologicalAgeRange = null;
            request.ExcludeBiologicalAgeRange = null;
        }

        //职责：在种类年龄范围与调用方限制的交集中直接抽样，避免成年曲线的无效重试。
        private static float ChooseAge(PawnGenerationRequest request)
        {
            float min = request.KindDef.minGenerationAge;
            float max = request.KindDef.maxGenerationAge;
            if (request.BiologicalAgeRange.HasValue)
            {
                min = Mathf.Max(min, request.BiologicalAgeRange.Value.min);
                max = Mathf.Min(max, request.BiologicalAgeRange.Value.max);
            }
            if (min > max)
                throw new InvalidOperationException("凝砂儿童的请求年龄与种类年龄范围没有交集。请求：" + request);
            if (!request.ExcludeBiologicalAgeRange.HasValue) return Rand.Range(min, max);

            FloatRange excluded = request.ExcludeBiologicalAgeRange.Value;
            if (excluded.max < min || excluded.min > max) return Rand.Range(min, max);
            //扣除禁止区间后，按左右剩余区间的长度选择年龄，不通过循环反复碰运气。
            float leftLength = Mathf.Max(0f, Mathf.Min(max, excluded.min) - min);
            float rightLength = Mathf.Max(0f, max - Mathf.Max(min, excluded.max));
            if (leftLength + rightLength <= 0f)
                throw new InvalidOperationException("凝砂儿童的可用年龄已全部被请求排除。请求：" + request);
            float choice = Rand.Range(0f, leftLength + rightLength);
            if (choice < leftLength || rightLength == 0f)
            {
                float age = min + Mathf.Min(choice, leftLength);
                //原版浮点随机包含端点，舍入恰好命中禁止边界时使用同一区间的合法外端点。
                return age < excluded.min ? age : min;
            }
            float rightAge = max - Mathf.Min(choice - leftLength, rightLength);
            return rightAge > excluded.max ? rightAge : max;
        }
    }
}
