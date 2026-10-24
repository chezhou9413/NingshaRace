using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Scenarios.Equipment;

namespace NingshaRaceLib.Scenarios.Parts
{
    //类职责：在地图生成前为三名凝砂族开局成员按顺序装备指定材料的基础武器。
    public sealed class ScenPart_NingshaStartingEquipment : ScenPart
    {
        //字段职责：保存与开局成员顺序一一对应的武器和材料配置。
        public List<NingshaStartingWeapon> weapons = new List<NingshaStartingWeapon>();

        //函数职责：序列化场景中的凝砂族开局武器清单。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref weapons, "weapons", LookMode.Deep);
        }

        //函数职责：校验装备配置和三名凝砂族开局成员，并为每人直接装备一件普通品质武器。
        public override void PreMapGenerate()
        {
            foreach (string error in ConfigErrors())
                throw new InvalidOperationException(error);

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
                    throw new InvalidOperationException($"开局成员 {pawn.LabelShort} 不是凝砂族，无法应用开局装备。");
                }

                EquipWeapon(pawn, weapons[i]);
            }
        }

        //函数职责：清理成员随机生成的武器，并装备一件普通品质的指定武器。
        private static void EquipWeapon(Pawn pawn, NingshaStartingWeapon entry)
        {
            pawn.equipment.DestroyAllEquipment();
            ThingWithComps weapon = ThingMaker.MakeThing(entry.weaponDef, entry.stuff) as ThingWithComps;
            if (weapon == null)
            {
                throw new InvalidOperationException($"{entry.weaponDef.defName} 不是可装备的武器物品。");
            }

            weapon.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
            pawn.equipment.AddEquipment(weapon);
        }

        //函数职责：按实际配置向场景信息面板列出成员携带的武器与材料。
        public override string Summary(Scenario scen)
        {
            List<string> labels = new List<string>();
            foreach (NingshaStartingWeapon entry in weapons)
                labels.Add(GenLabel.ThingLabel(entry.weaponDef, entry.stuff));
            return "三名凝砂族分别携带" + string.Join("、", labels) + "。";
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
                yield return "凝砂族开局必须配置三件武器。";
                yield break;
            }

            for (int i = 0; i < weapons.Count; i++)
            {
                NingshaStartingWeapon weapon = weapons[i];
                if (weapon == null)
                {
                    yield return $"凝砂族开局第 {i + 1} 件武器定义为空。";
                }
                else
                {
                    foreach (string error in weapon.ConfigErrors())
                        yield return $"凝砂族开局第 {i + 1} 件武器：{error}";
                }
            }
        }
    }
}
