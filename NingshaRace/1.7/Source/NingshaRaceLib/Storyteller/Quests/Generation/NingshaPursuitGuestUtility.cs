using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace NingshaRaceLib.Storyteller.Quests.Generation
{
    //类职责：沿用原版临时访客的附加阵营机制，让被追杀者在任务期间接受玩家控制。
    internal static class NingshaPursuitGuestUtility
    {
        //函数职责：建立隐藏临时阵营并登记访客身份，任务结束后解除临时身份。
        public static void Register(Quest quest, Pawn pawn)
        {
            var relations = new List<FactionRelation>();
            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
            {
                if (!faction.def.PermanentlyHostileTo(FactionDefOf.OutlanderRefugee))
                    relations.Add(new FactionRelation { other = faction, kind = FactionRelationKind.Neutral });
            }
            var parms = new FactionGeneratorParms(FactionDefOf.OutlanderRefugee, default(IdeoGenerationParms), true);
            Faction guestFaction = FactionGenerator.NewGeneratedFactionWithRelations(parms, relations);
            guestFaction.temporary = true;
            guestFaction.leader = pawn;
            Find.FactionManager.Add(guestFaction);
            pawn.SetFaction(guestFaction);
            quest.ExtraFaction(guestFaction, Gen.YieldSingle(pawn), ExtraFactionType.MiniFaction);
        }
    }
}
