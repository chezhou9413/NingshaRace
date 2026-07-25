using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Race.Components;

namespace NingshaRaceLib.Race.Rendering.BodyAddons
{
    //枚举职责：用可读名称表示 BodyAddon 纹理的四个绘制朝向。
    public enum BodyAddonTextureDirection
    {
        North,
        East,
        South,
        West
    }

    //类职责：描述一个 BodyAddon 在指定贴图变体和朝向下使用的缩放倍率与绘制偏移。
    public sealed class BodyAddonTextureScaleRule
    {
        //字段职责：限定规则作用的 BodyAddon 名称。
        public string bodyAddonName;

        //字段职责：限定规则作用的最终 Graphic 基础路径，不包含方向后缀和扩展名。
        public string texturePath;

        //字段职责：限定规则作用的绘制朝向。
        public BodyAddonTextureDirection direction;

        //字段职责：保存横向和纵向缩放倍率。
        public Vector2 scale = Vector2.one;

        //字段职责：保存最终绘制坐标中的横向和纵向偏移。
        public Vector2 offset = Vector2.zero;

        //字段职责：保存相对于 HAR 原始绘制层级的前后偏移。
        public float layerOffset;

        //函数职责：判断当前 BodyAddon、最终贴图路径和绘制朝向是否命中本规则。
        public bool Matches(string addonName, string graphicPath, Rot4 facing)
        {
            return string.Equals(bodyAddonName, addonName, StringComparison.Ordinal)
                && string.Equals(texturePath, graphicPath, StringComparison.Ordinal)
                && MatchesDirection(facing);
        }

        //函数职责：生成用于识别重复规则的稳定键。
        public string BuildKey()
        {
            return (bodyAddonName ?? string.Empty) + "\n" + (texturePath ?? string.Empty) + "\n" + direction;
        }

        //函数职责：检查单条规则的名称、贴图路径和缩放倍率是否合法。
        public IEnumerable<string> ConfigErrors(string ownerDefName, int index)
        {
            if (bodyAddonName.NullOrEmpty())
            {
                yield return ownerDefName + ".rules[" + index + "].bodyAddonName 不能为空";
            }
            if (texturePath.NullOrEmpty())
            {
                yield return ownerDefName + ".rules[" + index + "].texturePath 不能为空";
            }
            if (scale.x <= 0f || scale.y <= 0f)
            {
                yield return ownerDefName + ".rules[" + index + "].scale 必须大于零";
            }
        }

        //函数职责：把 XML 中的可读朝向转换为与 HAR 绘制参数一致的 Rot4 判断。
        private bool MatchesDirection(Rot4 facing)
        {
            switch (direction)
            {
                case BodyAddonTextureDirection.North:
                    return facing == Rot4.North;
                case BodyAddonTextureDirection.East:
                    return facing == Rot4.East;
                case BodyAddonTextureDirection.South:
                    return facing == Rot4.South;
                case BodyAddonTextureDirection.West:
                    return facing == Rot4.West;
                default:
                    return false;
            }
        }
    }
}
