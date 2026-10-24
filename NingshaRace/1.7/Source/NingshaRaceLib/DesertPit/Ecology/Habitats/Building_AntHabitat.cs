using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Habitats
{
    //类职责：提供生态建筑的地图登记、选择范围和轻量孢子表现。
    public abstract class Building_AntHabitat : Building
    {
        public abstract float Radius { get; }
        protected abstract bool EmitsSpores { get; }

        //函数职责：生成及读档时建立地图内生态建筑索引。
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            map.GetComponent<MapComponent_AntHabitats>().Register(this);
        }

        //函数职责：搬起或销毁前移除旧位置的生态覆盖。
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map.GetComponent<MapComponent_AntHabitats>().Unregister(this);
            base.DeSpawn(mode);
        }

        //函数职责：仅在玩家正在查看且已揭开迷雾的建筑附近释放少量绿色孢子。
        protected override void Tick()
        {
            base.Tick();
            if (!Spawned || !this.IsHashIntervalTick(90) || !EmitsSpores || Map != Find.CurrentMap
                || Position.Fogged(Map) || !Find.CameraDriver.CurrentViewRect.Contains(Position)) return;
            Rand.PushState(Gen.HashCombineInt(thingIDNumber, Find.TickManager.TicksGame));
            try
            {
                FleckCreationData data = FleckMaker.GetDataStatic(DrawPos + new Vector3(Rand.Range(-0.8f, 0.8f), 0f, Rand.Range(-0.6f, 0.6f)),
                    Map, FleckDefOf.MicroSparks, 0.35f);
                data.instanceColor = new Color(0.62f, 0.86f, 0.42f, 0.5f);
                data.velocityAngle = Rand.Range(0f, 360f);
                data.velocitySpeed = 0.22f;
                Map.flecks.CreateFleck(data);
            }
            finally { Rand.PopState(); }
        }

        //函数职责：只在选中时显示作用边界，避免常驻范围圈遮挡洞穴。
        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            GenDraw.DrawRadiusRing(Position, Radius);
        }

        //函数职责：保留原版建筑说明且不产生开头空行，让检查面板按实际文本自然排版。
        protected string InspectPrefix()
        {
            string original = base.GetInspectString();
            return original.NullOrEmpty() ? "" : original + "\n";
        }
    }
}
