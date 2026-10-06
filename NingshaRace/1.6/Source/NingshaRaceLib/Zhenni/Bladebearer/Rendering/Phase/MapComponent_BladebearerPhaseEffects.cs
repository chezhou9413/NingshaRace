using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //地图持有短暂残影，死亡和装备销毁不截断消散；不生成任何可交互实体。
    public sealed class MapComponent_BladebearerPhaseEffects : MapComponent
    {
        private readonly List<Effect> effects = new List<Effect>();

        //同一地图的有限寿命效果统一绘制和回收。
        private sealed class Effect
        {
            internal List<BladebearerPhaseSnapshot.Layer> Layers;
            internal float Started, Duration, Lifetime;
            internal bool Assemble;
        }

        //由 RimWorld 为地图创建组件。
        public MapComponent_BladebearerPhaseEffects(Map map) : base(map) { }

        //离开位置或死亡后，把完整轮廓逐渐剥离成紫焰火屑。
        internal void Dissolve(CompBladebearerPhaseStep owner, Vector3 origin, float duration, bool death)
        {
            var layers = owner.Snapshot.Capture(origin);
            if (layers.Count == 0) return;
            effects.Add(new Effect { Layers = layers, Started = Find.TickManager.TicksGame / 60f,
                Duration = duration, Lifetime = death ? 1.5f : 0.95f });
        }

        //以落点实际朝向的部件矩阵聚拢火屑，实体本身由渲染树逐步显现。
        internal void Aggregate(CompBladebearerPhaseStep owner, int started)
        {
            var layers = owner.Snapshot.Capture(owner.Snapshot.Origin);
            if (layers.Count == 0) return;
            effects.Add(new Effect { Layers = layers, Started = started / 60f,
                Duration = owner.Props.arrivalTicks / 60f, Lifetime = 0, Assemble = true });
        }

        //游戏暂停时不推进，过期后释放快照引用；网格和材质由资源类共用。
        public override void MapComponentTick()
        {
            float now = Find.TickManager.TicksGame / 60f;
            for (int i = effects.Count - 1; i >= 0; i--)
                if (now - effects[i].Started > effects[i].Duration + effects[i].Lifetime) effects.RemoveAt(i);
        }

        //透明轮廓保留实体深度裁剪，所有火屑继续测试场景深度。
        public override void MapComponentDraw()
        {
            if (map != Find.CurrentMap) return;
            float now = Find.TickManager.TicksGame / 60f;
            foreach (Effect effect in effects)
            {
                float age = now - effect.Started;
                foreach (BladebearerPhaseSnapshot.Layer layer in effect.Layers)
                {
                    var block = layer.Properties;
                    BladebearerShaderParameters.Apply(layer.Part, layer.Time + age * 0.35f, layer.Tint, block, null);
                    float dissolve = Mathf.Lerp(layer.Dissolve, 1, Mathf.Clamp01(age / effect.Duration));
                    if (!effect.Assemble && age < effect.Duration)
                    {
                        BladebearerShaderParameters.ApplyPhase(dissolve, block);
                        if (layer.Part.writeDepth) Graphics.DrawMesh(BladebearerResources.Mesh, layer.Matrix,
                            BladebearerResources.Material(layer.Part, true, false), 0, null, 0, block);
                        Graphics.DrawMesh(BladebearerResources.Mesh, layer.Matrix,
                            BladebearerResources.PhaseColor, 0, null, 0, block);
                        BladebearerBloomSources.Record(map, layer.Part, BladebearerResources.Mesh,
                            layer.Matrix, layer.Tint, layer.Time + age * 0.35f, dissolve);
                    }
                    block.SetFloat("_PhaseAge", age); block.SetFloat("_PhaseDuration", effect.Duration);
                    block.SetFloat("_PhaseLifetime", effect.Lifetime); block.SetFloat("_PhaseAssemble", effect.Assemble ? 1 : 0);
                    block.SetFloat("_PhaseInitial", layer.Dissolve);
                    Graphics.DrawMesh(BladebearerPhaseResources.Embers, layer.Matrix,
                        BladebearerPhaseResources.EmberMaterial, 0, null, 0, block);
                }
            }
        }

        //地图卸载时不残留角色或贴图引用。
        public override void MapRemoved() { effects.Clear(); }
    }
}
