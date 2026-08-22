using System.Collections.Generic;
using RimWorld;
using Verse;

using NingshaRaceLib.DesertPit.AntColony.Components;
using NingshaRaceLib.DesertPit.AntColony.State;

namespace NingshaRaceLib.DesertPit.AntColony.Buildings
{
    //类职责：作为沙漠巨坑蚁群的实体核心，并把受击和摧毁事件通知地图组件。
    public class Building_DesertPitAntNest : Building
    {
        //字段职责：记录蚁穴所属的地图内唯一巢群编号。
        private int colonyId;

        //属性职责：向成员与地图组件提供蚁穴所属巢群编号。
        public int ColonyId => colonyId;

        //函数职责：沿用原版建筑命令，并仅在上帝模式为可升级且未满级的沙坑蚁巢提供强制升级按钮。
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            if (!DebugSettings.godMode || !Spawned || colonyId <= 0)
            {
                yield break;
            }

            MapComponent_DesertPitAntColonies manager = Map.GetComponent<MapComponent_DesertPitAntColonies>();
            AntColonyState state;
            if (!manager.TryGetColony(this, out state) || !state.LevelingEnabled || state.CurrentLevel >= state.MaxLevel)
            {
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = "强制升级蚁巢",
                defaultDesc = "上帝模式：无视储藏营养、蚁后状态和升级冷却，将该蚁巢强制提升一级。",
                icon = TexCommand.DesirePower,
                action = delegate
                {
                    manager.DebugForceUpgrade(state);
                }
            };
        }

        //函数职责：在原版建筑信息后追加当前蚁巢的等级、营养与行为状态。
        public override string GetInspectString()
        {
            string original = base.GetInspectString();
            if (!Spawned || colonyId <= 0)
            {
                return original;
            }

            AntColonyState state;
            MapComponent_DesertPitAntColonies manager = Map.GetComponent<MapComponent_DesertPitAntColonies>();
            if (!manager.TryGetColony(this, out state))
            {
                return original;
            }

            string colonyInfo = manager.GetColonyInspectString(state);
            return original.NullOrEmpty() ? colonyInfo : original + "\n" + colonyInfo;
        }

        //函数职责：把生成阶段分配的巢群编号写入蚁穴。
        public void AssignColony(int id)
        {
            colonyId = id;
        }

        //函数职责：保存蚁穴所属巢群编号。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref colonyId, "colonyId");
        }

        //函数职责：蚁穴进入地图时恢复它与地图组件中巢群状态的绑定。
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (colonyId > 0)
            {
                map.GetComponent<MapComponent_DesertPitAntColonies>().NotifyNestSpawned(this, colonyId);
            }
        }

        //函数职责：蚁穴实际承受伤害后触发巢群完整警报。
        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostApplyDamage(dinfo, totalDamageDealt);
            if (totalDamageDealt > 0f && Spawned && colonyId > 0)
            {
                Map.GetComponent<MapComponent_DesertPitAntColonies>().NotifyNestDamaged(this, colonyId, dinfo.Instigator as Pawn);
            }
        }

        //函数职责：在致命伤害销毁蚁穴前记录最后攻击者并确保首轮爆浆蚁已经触发。
        public override void Kill(DamageInfo? dinfo = null, Hediff exactCulprit = null)
        {
            if (Spawned && colonyId > 0)
            {
                Map.GetComponent<MapComponent_DesertPitAntColonies>().NotifyNestDamaged(this, colonyId, dinfo.HasValue ? dinfo.Value.Instigator as Pawn : null);
            }

            base.Kill(dinfo, exactCulprit);
        }

        //函数职责：蚁穴因致命伤害被摧毁时触发最后一波爆浆蚁和成员狂暴。
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (Spawned && colonyId > 0 && (mode == DestroyMode.KillFinalize || mode == DestroyMode.KillFinalizeLeavingsOnly))
            {
                Map.GetComponent<MapComponent_DesertPitAntColonies>().NotifyNestDestroyed(this, colonyId);
            }

            base.Destroy(mode);
        }
    }
}
