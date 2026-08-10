using Verse;

using NingshaRaceLib.DesertPit.AntColony.Components;

namespace NingshaRaceLib.DesertPit.AntColony.Buildings
{
    //类职责：作为沙漠巨坑蚁群的实体核心，并把受击和摧毁事件通知地图组件。
    public class Building_DesertPitAntNest : Building
    {
        private int colonyId;

        public int ColonyId => colonyId;

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
