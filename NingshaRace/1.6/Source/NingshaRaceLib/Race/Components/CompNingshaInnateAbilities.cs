using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Petrification.Abilities.Utility;
using NingshaRaceLib.Race.Rendering.BodyAddons;
using NingshaRaceLib.SandGolem.Abilities.Utility;

namespace NingshaRaceLib.Race.Components
{
    //类职责：仅在凝砂族 Pawn 进入地图时补齐该种族拥有的固有能力。
    public sealed class CompNingshaInnateAbilities : ThingComp
    {
        //函数职责：只在新 Pawn 首次生成时授予固有能力，避免读档生成阶段修改能力集合。
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            if (respawningAfterLoad)
            {
                return;
            }

            Pawn pawn = parent as Pawn;
            if (pawn == null)
            {
                return;
            }

            SandGolemAbilityUtility.EnsureAbility(pawn);
            PetrifyingSandwaveAbilityUtility.EnsureAbility(pawn);
        }
    }
}
