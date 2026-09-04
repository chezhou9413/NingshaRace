using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Scenarios.Parts
{
    //类职责：在地图生成前为三名凝砂族开局成员按顺序装备指定的专属武器。
    public sealed class ScenPart_NingshaStartingEquipment : ScenPart
    {
        //字段职责：保存与开局成员顺序一一对应的武器定义。
        public List<ThingDef> weapons = new List<ThingDef>();

        //函数职责：序列化场景中的凝砂族开局武器清单。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref weapons, "weapons", LookMode.Def);
        }

        //函数职责：校验三名凝砂族开局成员，并为每人直接装备一件普通品质专属武器。
        public override void PreMapGenerate()
        {
            List<Pawn> pawns = Find.GameInitData.startingAndOptionalPawns;
            if (pawns.Count != weapons.Count)
            {
                throw new InvalidOperationException($"凝砂族开局成员数量为 {pawns.Count}，但装备数量为 {weapons.Count}。");
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn.def != DefOfRefs.NingshaRace)
                {
                    throw new InvalidOperationException($"开局成员 {pawn.LabelShort} 不是凝砂族，无法应用专属装备。");
                }

                EquipWeapon(pawn, weapons[i]);
            }
        }

        //函数职责：清理成员随机生成的武器，并装备一件普通品质的指定武器。
        private static void EquipWeapon(Pawn pawn, ThingDef weaponDef)
        {
            if (weaponDef == null)
            {
                throw new InvalidOperationException("凝砂族开局装备清单包含空武器定义。");
            }

            pawn.equipment.DestroyAllEquipment();
            ThingWithComps weapon = ThingMaker.MakeThing(weaponDef) as ThingWithComps;
            if (weapon == null)
            {
                throw new InvalidOperationException($"{weaponDef.defName} 不是可装备的武器物品。");
            }

            weapon.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
            pawn.equipment.AddEquipment(weapon);
        }

        //函数职责：向场景信息面板列出三名凝砂族携带的专属武器。
        public override string Summary(Scenario scen)
        {
            return "三名凝砂族分别携带蛇腹剑、飞针和沙瓶。";
        }

        //函数职责：报告装备数量、空定义或非武器定义配置错误。
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (weapons == null || weapons.Count != 3)
            {
                yield return "凝砂族开局必须配置三件专属武器。";
                yield break;
            }

            for (int i = 0; i < weapons.Count; i++)
            {
                ThingDef weapon = weapons[i];
                if (weapon == null)
                {
                    yield return $"凝砂族开局第 {i + 1} 件武器定义为空。";
                }
                else if (weapon.equipmentType == EquipmentType.None)
                {
                    yield return $"{weapon.defName} 不是可装备的武器。";
                }
            }
        }
    }
}
