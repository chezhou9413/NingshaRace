using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.AltarMissions.Map
{
    //类职责：保存祭坛任务地图的指定蚁穴、待救者和侵蚀体，并发送精确成败信号。
    public sealed class MissionMapComponent : MapComponent
    {
        //字段职责：记录任务蚁穴是否曾经成功生成，避免地图初始化期间错误结算。
        private bool antNestSeen;

        //字段职责：记录玩家是否已经进入解救任务地图并激活侵蚀体。
        private bool playerEntered;

        //字段职责：记录当前任务地图是否已经发送过一次成败信号。
        private bool outcomeSent;

        //字段职责：保存地下任务主洞室内供商队安全生成的入场格。
        private IntVec3 undergroundEntryCell = IntVec3.Invalid;

        //字段职责：保存解救任务中需要保护的凝砂族引用。
        private List<Pawn> rescuees = new List<Pawn>();

        //字段职责：保存解救任务中必须消灭的侵蚀体引用。
        private List<Pawn> erosionBodies = new List<Pawn>();

        //构造函数职责：把任务目标追踪状态绑定到指定地图。
        public MissionMapComponent(Verse.Map map) : base(map)
        {
        }

        //函数职责：登记地下任务的主洞室安全入场格并供商队到达逻辑跨存档读取。
        public void SetUndergroundEntryCell(IntVec3 cell)
        {
            undergroundEntryCell = cell;
        }

        //函数职责：在入场格仍位于地图内且可站立时提供给地下任务商队生成逻辑。
        public bool TryGetUndergroundEntryCell(out IntVec3 cell)
        {
            cell = undergroundEntryCell;
            return cell.IsValid && cell.InBounds(map) && cell.Standable(map);
        }

        //函数职责：登记解救任务生成的全部指定Pawn并跨存档保持其引用。
        public void InitializeRescueTargets(List<Pawn> friendlyPawns, List<Pawn> hostilePawns)
        {
            rescuees = new List<Pawn>(friendlyPawns);
            erosionBodies = new List<Pawn>(hostilePawns);
        }

        //函数职责：保存任务目标、入场状态与是否已经发送结算信号。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref antNestSeen, "altarAntNestSeen", false);
            Scribe_Values.Look(ref playerEntered, "altarMissionPlayerEntered", false);
            Scribe_Values.Look(ref outcomeSent, "altarMissionOutcomeSent", false);
            Scribe_Values.Look(ref undergroundEntryCell, "altarMissionUndergroundEntryCell", IntVec3.Invalid);
            Scribe_Collections.Look(ref rescuees, "altarMissionRescuees", LookMode.Reference);
            Scribe_Collections.Look(ref erosionBodies, "altarMissionErosionBodies", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                rescuees = rescuees ?? new List<Pawn>();
                erosionBodies = erosionBodies ?? new List<Pawn>();
            }
        }

        //函数职责：每六十Tick按当前世界地点类型检查固定蚁穴或解救目标状态。
        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (outcomeSent || !map.IsHashIntervalTick(60))
            {
                return;
            }
            string defName = map.Parent?.def?.defName;
            if (defName == "NingshaRace_AltarAntNestSite")
            {
                CheckAntNest();
            }
            else if (defName == "NingshaRace_AltarRescueSurfaceSite" || defName == "NingshaRace_AltarRescueUndergroundSite")
            {
                CheckRescueMission();
            }
        }

        //函数职责：在目标蚁穴生成后监视其销毁，并且不要求清理逃散成员。
        private void CheckAntNest()
        {
            bool exists = map.listerThings.ThingsOfDef(DefOfRefs.NingshaRace_DesertPitAntNest).Any(thing => !thing.Destroyed);
            antNestSeen |= exists;
            if (antNestSeen && !exists)
            {
                SendSiteSignal("AntNestDestroyed");
            }
        }

        //函数职责：等待玩家真正入场后解除侵蚀体待机，并按指定目标存活情况结算任务。
        private void CheckRescueMission()
        {
            if (!playerEntered && map.mapPawns.FreeColonistsSpawned.Any())
            {
                playerEntered = true;
                ActivateRescueAttackers();
            }
            if (!playerEntered)
            {
                return;
            }
            List<Pawn> survivors = rescuees.Where(pawn => pawn != null && !pawn.Dead && !pawn.Destroyed).ToList();
            if (survivors.Count == 0)
            {
                SendSiteSignal("RescueFailed");
                return;
            }
            if (erosionBodies.All(pawn => pawn == null || pawn.Dead || pawn.Destroyed))
            {
                for (int i = 0; i < survivors.Count; i++)
                {
                    survivors[i].SetFaction(Faction.OfPlayer);
                }
                SendSiteSignal("RescueSucceeded");
            }
        }

        //函数职责：把全部存活侵蚀体纳入正式攻击编组，强制其从四角向玩家殖民者推进且不会撤退、绑架或偷窃。
        private void ActivateRescueAttackers()
        {
            List<Pawn> attackers = erosionBodies
                .Where(pawn => pawn != null && !pawn.Dead && !pawn.Destroyed && pawn.Spawned && pawn.Map == map)
                .ToList();
            if (attackers.Count == 0)
            {
                return;
            }

            LordJob_AssaultColony assaultJob = new LordJob_AssaultColony(
                Faction.OfEntities,
                canKidnap: false,
                canTimeoutOrFlee: false,
                sappers: false,
                useAvoidGridSmart: false,
                canSteal: false);
            LordMaker.MakeNewLord(Faction.OfEntities, assaultJob, map, attackers);
        }

        //函数职责：通过地图父级地点的Quest标签发送一次任务专用结算信号。
        private void SendSiteSignal(string suffix)
        {
            Site site = map.Parent as Site;
            if (site == null)
            {
                throw new System.InvalidOperationException("祭坛任务地图父级不是原版Site。" + map.Parent);
            }
            QuestUtility.SendQuestTargetSignals(site.questTags, suffix);
            outcomeSent = true;
        }
    }
}
