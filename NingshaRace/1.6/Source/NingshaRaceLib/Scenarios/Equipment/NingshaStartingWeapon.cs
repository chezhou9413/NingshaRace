using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.Scenarios.Equipment
{
    //类职责：保存一名开局成员的武器及制作材料，供场景配置、存档和生成前校验使用。
    public sealed class NingshaStartingWeapon : IExposable
    {
        public ThingDef weaponDef;
        public ThingDef stuff;

        //函数职责：保存武器与材料引用，使场景编辑副本和场景存档保留完整配置。
        public void ExposeData()
        {
            Scribe_Defs.Look(ref weaponDef, "weaponDef");
            Scribe_Defs.Look(ref stuff, "stuff");
        }

        //函数职责：报告不可装备的物品或缺失、不适用的材料，阻止生成错误武器。
        public IEnumerable<string> ConfigErrors()
        {
            if (weaponDef == null || !weaponDef.IsWeapon || weaponDef.equipmentType == EquipmentType.None)
            {
                yield return "开局武器必须是可装备的武器定义。";
                yield break;
            }

            if (weaponDef.MadeFromStuff)
            {
                if (stuff?.stuffProps == null || !stuff.stuffProps.CanMake(weaponDef))
                    yield return $"开局武器 {weaponDef.defName} 必须指定可用于制作它的材料。";
            }
            else if (stuff != null)
            {
                yield return $"开局武器 {weaponDef.defName} 不允许指定制作材料。";
            }
        }
    }
}
