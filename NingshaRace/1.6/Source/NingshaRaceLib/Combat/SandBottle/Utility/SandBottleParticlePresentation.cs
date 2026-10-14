using UnityEngine;

namespace NingshaRaceLib.Combat.SandBottle.Utility
{
    //类职责：把沙瓶粒子实例调整为稀疏、透明且贴合地图的短促沙尘。
    public static class SandBottleParticlePresentation
    {
        //函数职责：仅调整本次特效实例，保留资源包中其他使用者的共享预制体和材质。
        public static void Configure(GameObject instance)
        {
#if RW_1_7
            SandBottleParticleMaterials ownedMaterials = instance.AddComponent<SandBottleParticleMaterials>();
#endif
            foreach (ParticleSystem particles in instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = particles.main;
                main.loop = false;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.startSizeMultiplier *= 0.7f;
                ParticleSystem.EmissionModule emission = particles.emission;
                emission.rateOverTimeMultiplier *= 0.55f;
                emission.rateOverDistanceMultiplier *= 0.55f;
                for (int i = 0; i < emission.burstCount; i++)
                {
                    ParticleSystem.Burst burst = emission.GetBurst(i);
                    ParticleSystem.MinMaxCurve count = burst.count;
                    count.constantMin *= 0.65f;
                    count.constantMax *= 0.65f;
                    burst.count = count;
                    emission.SetBurst(i, burst);
                }
                ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
                if (renderer == null) continue;
                //贴地面片不再沿粒子速度拉成长矩形，也不通过压扁根节点实现二维效果。
                renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
                renderer.sortingOrder = 0;
                Material material = renderer.sharedMaterial;
                if (material != null && material.HasProperty("_Alpha"))
                {
#if RW_1_7
                    //粒子 Renderer 不支持属性块，为此实例保留独立材质并随特效释放。
                    Material local = new Material(material);
                    local.SetFloat("_Alpha", material.GetFloat("_Alpha") * 0.45f);
                    ownedMaterials.Add(local);
                    renderer.sharedMaterial = local;
#else
                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);
                    block.SetFloat("_Alpha", material.GetFloat("_Alpha") * 0.45f);
                    renderer.SetPropertyBlock(block);
#endif
                }
            }
        }
    }
}
