using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //集中检查磁盘导入与 Def 加载的实际输入。
    internal static class BladebearerValidation
    {
        internal static readonly FieldInfo[] Fields = typeof(BladebearerPart).GetFields(BindingFlags.Public | BindingFlags.Instance);

        //返回带部件和字段名称的错误，不静默截断参数。
        public static IEnumerable<string> Errors(List<BladebearerPart> parts)
        {
            if (parts == null || parts.Count == 0) { yield return "parts：至少需要一个部件。"; yield break; }
            var ids = new HashSet<string>();
            foreach (BladebearerPart p in parts)
            {
                if (p == null) { yield return "parts：存在空部件。"; continue; }
                string prefix = "部件「" + (p.label ?? p.id) + "」";
                if (p.facing != "All" && p.facing != "South" && p.facing != "East" && p.facing != "West" && p.facing != "North")
                    yield return prefix + " facing：必须是 All、South、East、West 或 North。";
                if (p.attachment != "Body" && p.attachment != "Weapon")
                    yield return prefix + " attachment：必须是 Body 或 Weapon。";
                if (p.flame && p.particle) yield return prefix + " flame/particle：火焰与粒子不能同时启用。";
                if (p.particle && p.writeDepth) yield return prefix + " writeDepth：粒子保留深度测试，但不能写入实体深度。";
                if (p.particle && !string.IsNullOrEmpty(p.particleFlame)
                    && !parts.Exists(source => source != null && source.id == p.particleFlame && source.flame && !source.particle))
                    yield return prefix + " particleFlame：找不到火焰部件「" + p.particleFlame + "」。";
                if (p.particle && !string.IsNullOrEmpty(p.particleFlame)
                    && parts.Exists(source => source != null && source.id == p.particleFlame && source.attachment != p.attachment))
                    yield return prefix + " particleFlame：粒子和跟随火焰必须绑定同一身体或武器。";
                if (p.particle && !string.IsNullOrEmpty(p.particleFlame)
                    && parts.Exists(source => source != null && source.id == p.particleFlame && source.facing != "All" && source.facing != p.facing))
                    yield return prefix + " particleFlame：跟随火焰的适用朝向必须包含当前粒子朝向。";
                if (string.IsNullOrWhiteSpace(p.id) || !ids.Add(p.id)) yield return prefix + " id：为空或重复。";
                if (string.IsNullOrWhiteSpace(p.label)) yield return prefix + " label：不能为空。";
                if (string.IsNullOrWhiteSpace(p.texture)) yield return prefix + " texture：不能为空。";
                if (string.IsNullOrWhiteSpace(p.displacementTexture)) yield return prefix + " displacementTexture：不能为空。";
                foreach (FieldInfo field in Fields)
                {
                    object value = field.GetValue(p);
                    if (value is float f && !Finite(f)) yield return prefix + " " + field.Name + "：必须是有限数字。";
                    if (value is Vector2 v && (!Finite(v.x) || !Finite(v.y))) yield return prefix + " " + field.Name + "：必须是有限坐标。";
                    if (value is Color c && (!Unit(c.r) || !Unit(c.g) || !Unit(c.b) || !Unit(c.a))) yield return prefix + " " + field.Name + "：颜色通道必须在 0 到 1 之间。";
                }
                if (p.size.x <= 0 || p.size.y <= 0 || p.size.x > 10 || p.size.y > 10) yield return prefix + " size：必须大于 0 且不超过 10 格。";
                if (!Range(p.position.x, -5, 5) || !Range(p.position.y, -5, 5)) yield return prefix + " position：范围为 -5 到 5 格。";
                if (!Range(p.angle, -180, 180)) yield return prefix + " angle：范围为 -180 到 180 度。";
                if (!Range(p.phase, -100, 100)) yield return prefix + " phase：范围为 -100 到 100。";
                if (p.layer < -10 || p.layer > 100) yield return prefix + " layer：必须在 -10 到 100 之间。";
                if (p.attachment == "Weapon" && !Range(p.layer, 0, 20)) yield return prefix + " layer：武器局部层级范围为 0 到 20。";
                if (!Unit(p.root.x) || !Unit(p.root.y)) yield return prefix + " root：必须在贴图 UV 0 到 1 之内。";
                if (!Unit(p.tip.x) || !Unit(p.tip.y)) yield return prefix + " tip：必须在贴图 UV 0 到 1 之内。";
                if ((p.tip - p.root).sqrMagnitude < 0.0001f) yield return prefix + " tip：根部与尾端不能重合。";
                if (!Range(p.rootLock, 0, 0.8f)) yield return prefix + " rootLock：范围为 0 到 0.8。";
                if (!Range(p.bendPower, 0.5f, 4)) yield return prefix + " bendPower：范围为 0.5 到 4。";
                if (!Range(p.speed, 0, 6)) yield return prefix + " speed：范围为 0 到 6。";
                if (!Range(p.flowAngle, -180, 180)) yield return prefix + " flowAngle：范围为 -180 到 180 度。";
                if (!Range(p.breakup, 0, 2)) yield return prefix + " breakup：范围为 0 到 2。";
                if (!Range(p.displacement.x, -0.4f, 0.4f) || !Range(p.displacement.y, -0.4f, 0.4f)) yield return prefix + " displacement：范围为 -0.4 到 0.4。";
                if (!Range(p.noiseScale.x, 0.01f, 12) || !Range(p.noiseScale.y, 0.01f, 12)) yield return prefix + " noiseScale：范围为 0.01 到 12。";
                if (!Range(p.breakupScale.x, 0.01f, 12) || !Range(p.breakupScale.y, 0.01f, 12)) yield return prefix + " breakupScale：范围为 0.01 到 12。";
                if (!Unit(p.detailStrength) || !Unit(p.additive)) yield return prefix + " detailStrength/additive：范围为 0 到 1。";
                if (!Range(p.bloomIntensity, 0, 2)) yield return prefix + " bloomIntensity：范围为 0 到 2。";
                if (!Unit(p.bloomThreshold)) yield return prefix + " bloomThreshold：范围为 0 到 1。";
                if (!Range(p.lightIntensity, 0, 4)) yield return prefix + " lightIntensity：范围为 0 到 4。";
                if (!Range(p.lightFlickerStrength, 0, 0.5f)) yield return prefix + " lightFlickerStrength：范围为 0 到 0.5。";
                if (!Range(p.lightFlickerFrequency, 0.1f, 5)) yield return prefix + " lightFlickerFrequency：范围为每秒 0.1 到 5 次。";
                if (!Range(p.particleRate, 0, 60)) yield return prefix + " particleRate：范围为每秒 0 到 60 个。";
                if (!Range(p.particleLifetime, 0.1f, 3)) yield return prefix + " particleLifetime：范围为 0.1 到 3 秒。";
                if (!Range(p.particleSpeed, 0, 0.8f)) yield return prefix + " particleSpeed：范围为 0 到 0.8。";
                if (!Range(p.particleSize, 0.001f, 0.08f)) yield return prefix + " particleSize：范围为 0.001 到 0.08。";
                if (!Range(p.particleSpread, 0, 180)) yield return prefix + " particleSpread：范围为 0 到 180 度。";
                if (!Range(p.particleRadius, 0, 0.2f)) yield return prefix + " particleRadius：范围为 0 到 0.2。";
                if (!Range(p.particleSway, 0, 0.1f)) yield return prefix + " particleSway：范围为 0 到 0.1。";
            }
        }

        //拒绝 NaN 与无穷值，避免产生无效渲染矩阵。
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        //检查闭区间。
        private static bool Range(float value, float min, float max) => Finite(value) && value >= min && value <= max;
        //检查归一化通道。
        private static bool Unit(float value) => Range(value, 0, 1);
    }
}
