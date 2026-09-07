using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Antlion.Components;
using NingshaRaceLib.DesertPit.Antlion.State;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Antlion.Effects
{
    //类职责：分开表现潜伏流沙、出土喷沙和下潜卷沙，以明暗沙粒增强辨识度并保持局部短寿命。
    internal static class AntlionSandEffects
    {
        //属性职责：读取独立视觉配置，不缓存跨存档的地图、材质或渲染资源。
        private static DefModExtension_AntlionSandEffects Settings
            => DefOfRefs.NingshaRace_AntlionSandDust.GetModExtension<DefModExtension_AntlionSandEffects>();

        //函数职责：错峰播放持续可辨的局部沙粒扰动，粒子位置随机但不随机丢弃整次提示。
        public static void TickBuried(Map map, IntVec3 cell, int id)
        {
            if ((Find.TickManager.TicksGame + id) % Settings.buriedIntervalTicks == 0)
                Emit(map, cell, id, AntlionPhase.Buried, false);
        }

        //函数职责：沿阶段时间发射方向一致的沙流，出土向外喷散，下潜向内收拢。
        public static void TickTransition(CompAntlionAmbush comp)
        {
            int elapsed = Find.TickManager.TicksGame - comp.PhaseStartTick;
            if (comp.Transitioning && elapsed > 0 && elapsed % Settings.transitionIntervalTicks == 0)
                Emit(comp.Pawn.Map, comp.Pawn.Position, comp.Pawn.thingIDNumber, comp.Phase, false);
        }

        //函数职责：在出土瞬间产生一次更醒目的喷沙，不延长出土硬直或加速时间。
        public static void Erupt(Map map, IntVec3 cell, int id)
        {
            Emit(map, cell, id, AntlionPhase.Emerging, true);
        }

        //函数职责：在可见区域创建有数量上限的沙流，完整隔离粒子初始化消耗的视觉随机数。
        private static void Emit(Map map, IntVec3 cell, int id, AntlionPhase phase, bool eruption)
        {
            if (!Visible(map, cell)) return;
            int seed = Gen.HashCombineInt(Gen.HashCombineInt(id, Find.TickManager.TicksGame), (int)phase);
            Rand.PushState(seed);
            try
            {
                DefModExtension_AntlionSandEffects settings = Settings;
                bool buried = phase == AntlionPhase.Buried;
                bool outward = phase == AntlionPhase.Emerging;
                Vector3 center = cell.ToVector3Shifted();
                //每次扰动的中心轻微偏移，不形成规则圆圈或固定颗粒排列。
                center += new Vector3(Rand.Range(-0.10f, 0.10f), 0f, Rand.Range(-0.10f, 0.10f));
                int count = eruption ? settings.eruptionGrains : buried ? settings.buriedGrains : settings.transitionGrains;
                for (int i = 0; i < count; i++) EmitGrain(map, center, settings, buried, outward, eruption);
                int clouds = eruption ? 5 : buried ? 2 : 1;
                for (int i = 0; i < clouds; i++) EmitDust(map, center, settings, buried, outward, eruption);
            }
            finally
            {
                Rand.PopState();
            }
        }

        //函数职责：交错明暗粒子并赋予切向与径向速度，使浅沙地上也能辨认沙流的方向。
        private static void EmitGrain(Map map, Vector3 center, DefModExtension_AntlionSandEffects settings,
            bool buried, bool outward, bool eruption)
        {
            float angle = Rand.Range(0f, 360f);
            Vector3 radial = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
            Vector3 tangent = new Vector3(radial.z, 0f, -radial.x);
            float radius = buried ? settings.buriedRadius * Rand.Range(0.2f, 1f)
                : outward ? Rand.Range(0.10f, 0.40f) : Rand.Range(0.35f, 0.75f);
            Vector3 position = center + radial * radius;
            if (!Visible(map, position.ToIntVec3())) return;
            FleckDef def = Rand.Value < 0.38f ? DefOfRefs.NingshaRace_AntlionSandGrainDark : DefOfRefs.NingshaRace_AntlionSandGrain;
            float scale = (buried ? settings.buriedGrainScale : settings.movingGrainScale).RandomInRange;
            FleckCreationData grain = FleckMaker.GetDataStatic(position, map, def, scale);
            grain.exactScale = new Vector3(scale * Rand.Range(0.7f, 1f), 1f, scale * Rand.Range(1f, 1.65f));
            float radialSpeed = buried ? -Rand.Range(0.12f, 0.28f)
                : outward ? Rand.Range(eruption ? 1.3f : 0.7f, eruption ? 2.3f : 1.3f) : -Rand.Range(0.5f, 0.8f);
            grain.velocity = radial * radialSpeed + tangent * Rand.Range(buried ? 0.22f : 0.08f, buried ? 0.42f : 0.22f);
            grain.rotation = angle + Rand.Range(-35f, 35f);
            grain.rotationRate = Rand.Range(-100f, 100f);
            map.flecks.CreateFleck(grain);
        }

        //函数职责：以偏心椭圆沙尘托住沙粒，喷散后迅速淡出，避免连续堆叠高不透明度烟团。
        private static void EmitDust(Map map, Vector3 center, DefModExtension_AntlionSandEffects settings,
            bool buried, bool outward, bool eruption)
        {
            Vector3 radial = Quaternion.AngleAxis(Rand.Range(0f, 360f), Vector3.up) * Vector3.forward;
            Vector3 position = center + radial * Rand.Range(0.08f, buried ? 0.3f : 0.42f);
            if (!Visible(map, position.ToIntVec3())) return;
            float size = (buried ? settings.buriedDustScale : settings.movingDustScale).RandomInRange;
            FleckCreationData dust = FleckMaker.GetDataStatic(position, map, DefOfRefs.NingshaRace_AntlionSandDust, size);
            dust.exactScale = new Vector3(size * Rand.Range(0.85f, 1.1f), 1f, size * Rand.Range(0.55f, 0.85f));
            dust.rotation = Rand.Range(0f, 360f);
            dust.rotationRate = Rand.Range(-45f, 45f);
            dust.velocity = radial * (buried ? -0.12f : outward ? eruption ? 0.8f : 0.4f : -0.35f);
            dust.instanceColor = new Color(1f, 1f, 1f, eruption ? 1f : buried ? 0.8f : 0.75f);
            dust.solidTimeOverride = buried ? 0.20f : 0.07f;
            map.flecks.CreateFleck(dust);
        }

        //函数职责：限制特效到已揭雾的镜头附近可通行位置，避免沙粒穿墙或泄露未探索区域。
        private static bool Visible(Map map, IntVec3 cell)
        {
            return map == Find.CurrentMap && cell.InBounds(map) && !cell.Fogged(map) && !cell.Filled(map)
                && Find.CameraDriver.CurrentViewRect.ExpandedBy(1).Contains(cell);
        }
    }
}
