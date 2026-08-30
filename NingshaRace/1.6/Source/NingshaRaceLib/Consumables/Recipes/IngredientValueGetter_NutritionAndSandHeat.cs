using System.Linq;
using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Consumables.Recipes
{
    //类职责：让炙热地煲配方同时按生菌营养和沙之热实体数量结算原料。
    public sealed class IngredientValueGetter_NutritionAndSandHeat : IngredientValueGetter_Nutrition
    {
        //函数职责：沙之热每个计为一单位，其余合法食材继续使用原版营养值。
        public override float ValuePerUnitOf(ThingDef thingDef)
        {
            return thingDef == DefOfRefs.NingshaRace_SandHeat ? 1f : base.ValuePerUnitOf(thingDef);
        }

        //函数职责：对沙之热过滤项显示数量要求，对生菌过滤项显示营养要求。
        public override string BillRequirementsDescription(RecipeDef recipe, IngredientCount ingredient)
        {
            if (ingredient.filter.AllowedThingDefs.Contains(DefOfRefs.NingshaRace_SandHeat))
            {
                return ingredient.GetBaseCount().ToString("0.##") + "个（" + ingredient.filter.Summary + "）";
            }
            return base.BillRequirementsDescription(recipe, ingredient);
        }
    }
}
