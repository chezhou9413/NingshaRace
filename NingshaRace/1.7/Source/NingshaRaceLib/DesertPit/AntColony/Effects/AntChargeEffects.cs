using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Effects
{
    //蓄力范围复用原版建筑边框，冲锋与撞墙使用少量速度线及原版粒子。
    [StaticConstructorOnStartup]
    internal static class AntChargeEffects
    {
        private static readonly Material Edge = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 0.08f, 0.1f));
        private static readonly Material Bright = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 0.65f, 0.55f));
        private static readonly Material Wake = SolidColorMaterials.SimpleSolidColorMaterial(new Color(1f, 0.2f, 0.08f, 0.18f));

        //路线只在锁定或位移时生成；绘制时直接复用列表，不逐帧扫描地图或构建网格。
        public static void DrawIndicator(Pawn pawn, List<IntVec3> route)
        {
            if (!Visible(pawn.Map, pawn.Position) || route[route.Count - 1].Fogged(pawn.Map)) return;
            GenDraw.DrawFieldEdges(route, Color.red);
        }

        //尾迹和侧面速度线紧跟平滑位置，长度不超过已经走过的距离。
        public static void DrawRush(Pawn pawn, IntVec3 origin, IntVec3 destination)
        {
            if (!Visible(pawn.Map, pawn.Position)) return;
            Vector3 direction = (destination - origin).ToVector3().normalized;
            Vector3 head = pawn.DrawPos;
            head.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            float length = Mathf.Min(2.5f, (pawn.DrawPos - origin.ToVector3Shifted()).MagnitudeHorizontal());
            if (length < 0.05f) return;
            Vector3 tail = head - direction * length;
            Vector3 side = new Vector3(-direction.z, 0f, direction.x);
            DrawStrip(tail, head, 0.65f, Wake);
            GenDraw.DrawLineBetween(tail, head - direction * 0.2f, Bright, 0.09f);
            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 offset = side * (i * 0.48f);
                GenDraw.DrawLineBetween(tail + direction * 0.25f + offset, head + offset, Edge, 0.055f);
            }
        }

        //速度尾迹共用单位网格与固定材质。
        private static void DrawStrip(Vector3 start, Vector3 end, float width, Material material)
        {
            Vector3 delta = end - start;
            float length = delta.magnitude;
            if (length < 0.001f) return;
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS((start + end) * 0.5f,
                Quaternion.LookRotation(delta), new Vector3(width, 1f, length)), material, 0);
        }

        //蓄力脚下少量扬尘，不影响战斗随机数。
        public static void Windup(Pawn pawn, float progress) => Dust(pawn.Map, pawn.Position.ToVector3Shifted(),
            pawn.thingIDNumber, 1, 0.2f + progress * 0.25f);

        //起冲与沿途的尘土留在原位置，形成短尾迹。
        public static void Trail(Pawn pawn, IntVec3 cell, bool launch = false)
        {
            Dust(pawn.Map, cell.ToVector3Shifted(), pawn.thingIDNumber, launch ? 7 : 3, launch ? 1f : 0.7f);
            if (!launch || !Visible(pawn.Map, cell)) return;
            FleckCreationData ring = FleckMaker.GetDataStatic(cell.ToVector3Shifted(), pawn.Map, FleckDefOf.FlashHollow, 1.6f);
            ring.instanceColor = new Color(1f, 0.3f, 0.12f);
            pawn.Map.flecks.CreateFleck(ring);
        }

        //命中或撞墙时给出短闪与碎屑，伤害结算仍由工作执行。
        public static void Impact(Map map, IntVec3 cell, int seed)
        {
            if (!Visible(map, cell)) return;
            Dust(map, cell.ToVector3Shifted(), seed, 4, 0.65f);
            FleckCreationData flash = FleckMaker.GetDataStatic(cell.ToVector3Shifted(), map, FleckDefOf.FlashHollow, 0.8f);
            flash.instanceColor = new Color(1f, 0.65f, 0.25f);
            map.flecks.CreateFleck(flash);
        }

        //撞墙采用更大的冲击环和碎尘，只在撞击瞬间生成一次。
        public static void WallImpact(Map map, IntVec3 cell, int seed)
        {
            if (!Visible(map, cell)) return;
            Dust(map, cell.ToVector3Shifted(), seed, 9, 1.1f);
            FleckCreationData flash = FleckMaker.GetDataStatic(cell.ToVector3Shifted(), map, FleckDefOf.FlashHollow, 1.7f);
            flash.instanceColor = new Color(1f, 0.8f, 0.5f);
            map.flecks.CreateFleck(flash);
        }

        //粒子只在当前可见地图生成，暂停后不会继续产生。
        private static void Dust(Map map, Vector3 position, int seed, int count, float scale)
        {
            if (!Visible(map, position.ToIntVec3())) return;
            Rand.PushState(Gen.HashCombineInt(seed, Find.TickManager.TicksGame));
            try
            {
                for (int i = 0; i < count; i++)
                {
                    FleckCreationData dust = FleckMaker.GetDataStatic(position, map, FleckDefOf.DustPuff,
                        scale * Rand.Range(0.8f, 1.2f));
                    dust.instanceColor = new Color(0.7f, 0.59f, 0.42f);
                    dust.velocityAngle = Rand.Range(0f, 360f);
                    dust.velocitySpeed = 0.6f;
                    map.flecks.CreateFleck(dust);
                }
            }
            finally { Rand.PopState(); }
        }

        //仅处理画面内且已经揭开的格子。
        private static bool Visible(Map map, IntVec3 cell) => map == Find.CurrentMap && cell.InBounds(map)
            && !cell.Fogged(map) && Find.CameraDriver.CurrentViewRect.Contains(cell);
    }
}
