using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //地图和即时预览持有各自的粒子网格，避免预览覆写已提交到地图的顶点。
    internal sealed class BladebearerParticleEmitters : IDisposable
    {
        private readonly Dictionary<string, BladebearerParticleEmitter> map = new Dictionary<string, BladebearerParticleEmitter>();
        private readonly Dictionary<string, BladebearerParticleEmitter> preview = new Dictionary<string, BladebearerParticleEmitter>();

        //在渲染主线程推进粒子并直接绘制，原静态火星图层不再提交。
        internal Mesh Draw(PawnRenderNode_BladebearerPart node, PawnDrawParms parms, Matrix4x4 matrix)
        {
            Dictionary<string, BladebearerParticleEmitter> emitters = parms.DrawNow ? preview : map;
            float time = node.Owner.AnimationTime;
            if (!emitters.TryGetValue(node.Part.id, out BladebearerParticleEmitter emitter))
            {
                emitter = new BladebearerParticleEmitter(node.Owner.Pawn.thingIDNumber ^ node.Part.id.GetHashCode(), time);
                emitters.Add(node.Part.id, emitter);
            }
            emitter.Update(node.Part, node.Owner.ParticleFlame(node.Part), time);
            if (emitter.Mesh.vertexCount == 0) return null;
            Material material = BladebearerResources.Material(node.Part, false, parms.DrawNow);
            Color tint = parms.tint;
            if (!parms.DrawNow) tint.a *= 1 - (node.Owner.Phase?.Arrival ?? 0);
            BladebearerShaderParameters.ApplyParticles(node.Part, tint, emitter.Properties, parms.DrawNow ? material : null);
            GenDraw.DrawMeshNowOrLater(emitter.Mesh, matrix, material, parms.DrawNow, emitter.Properties);
            return emitter.Mesh;
        }

        //结构、地图或编辑会话变化时销毁旧发射器。
        public void Dispose()
        {
            foreach (BladebearerParticleEmitter emitter in map.Values) emitter.Dispose();
            foreach (BladebearerParticleEmitter emitter in preview.Values) emitter.Dispose();
            map.Clear(); preview.Clear();
        }
    }
}
