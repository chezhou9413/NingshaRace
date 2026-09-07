using NingshaRaceLib.Core.Defs;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Effects
{
    //类职责：沿酸液弹丸实际经过的可见路径生成短寿命绿色酸雾与液滴，不参与伤害结算。
    internal static class AntAcidTrailUtility
    {
        private const float ParticleSpacing = 0.22f;
        private const int MaxSamplesPerStep = 6;

        //函数职责：在当前地图已揭雾的可见弹道内有限采样，并隔离粒子系统消耗的视觉随机数。
        public static void EmitSegment(Map map, Vector3 from, Vector3 to, int projectileId)
        {
            if (map != Find.CurrentMap) return;
            Vector3 segment = (to - from).Yto0();
            float distance = segment.magnitude;
            if (distance <= 0.001f) return;
            Vector3 direction = segment / distance;
            Vector3 sideways = new Vector3(direction.z, 0f, -direction.x);
            int samples = Mathf.Min(MaxSamplesPerStep, Mathf.CeilToInt(distance / ParticleSpacing));
            CellRect view = Find.CameraDriver.CurrentViewRect;

            //粒子初始化也会消耗原版随机数，将完整创建过程包在独立状态中。
            Rand.PushState(Gen.HashCombineInt(projectileId, Find.TickManager.TicksGame));
            try
            {
                for (int i = 0; i < samples; i++)
                {
                    Vector3 position = Vector3.Lerp(from, to, (i + 0.5f) / samples);
                    IntVec3 cell = position.ToIntVec3();
                    if (!cell.InBounds(map) || !view.Contains(cell) || cell.Fogged(map)) continue;
                    EmitMist(map, position, direction.AngleFlat());
                    if ((i & 1) == 0) EmitDroplet(map, position, direction, sideways);
                }
            }
            finally
            {
                Rand.PopState();
            }
        }

        //函数职责：生成沿飞行方向拉伸的柔边酸雾，使相邻采样点形成连续绿色尾迹。
        private static void EmitMist(Map map, Vector3 position, float angle)
        {
            FleckCreationData data = FleckMaker.GetDataStatic(position, map, DefOfRefs.NingshaRace_AntAcidTrailMist);
            data.rotation = angle;
            data.exactScale = new Vector3(Rand.Range(0.28f, 0.36f), 1f, Rand.Range(0.48f, 0.58f));
            map.flecks.CreateFleck(data);
        }

        //函数职责：在酸雾内部点缀少量缓慢飘散的酸滴，增强液体喷射感而不扩大影响区域。
        private static void EmitDroplet(Map map, Vector3 position, Vector3 direction, Vector3 sideways)
        {
            FleckCreationData data = FleckMaker.GetDataStatic(position, map, DefOfRefs.NingshaRace_AntAcidTrailDroplet,
                Rand.Range(0.16f, 0.25f));
            data.rotation = Rand.Range(0f, 360f);
            data.velocity = direction * Rand.Range(0.15f, 0.4f) + sideways * Rand.Range(-0.25f, 0.25f);
            data.rotationRate = Rand.Range(-50f, 50f);
            map.flecks.CreateFleck(data);
        }
    }
}
