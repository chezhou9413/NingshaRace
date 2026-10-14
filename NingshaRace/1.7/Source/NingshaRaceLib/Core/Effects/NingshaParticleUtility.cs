using System.Collections.Generic;
using ChezhouLib.Assets;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Core.Effects;

//技能沿用 XML 效果键，通过 CL 挂载的 RimUnity 模板创建实例。
public static class NingshaParticleUtility
{
    //单次播放保留方向、缩放与技能配置的最长寿命。
    public static void Spawn(string effectKey, Map map, Vector3 position, Quaternion rotation, float scale, float lifetime)
    {
        var instance = Create(effectKey, position, rotation, scale);
        map.GetComponent<MapComponent_NingshaEffects>().Track(instance, lifetime);
    }

    //根变换由技能或头部姿态决定，子级保留标准导出的原始数据。
    internal static RimUnity.GameObject Create(string key, Vector3 position, Quaternion rotation, float scale)
    {
        var instance = ClPrefab.Instantiate(ClPrefab.Load(key), position, rotation);
        instance.SetActive(false);
        instance.transform.localScale = RimUnity.Vector3.one * scale;
        var particles = instance.GetComponentsInChildren<RimUnity.ParticleSystem>(true);
        foreach (var system in particles) system.Stop(false, RimUnity.ParticleSystemStopBehavior.StopEmittingAndClear);
        if (key == "NingshaRace_SandThrowBurst") SandBottleRimUnityPresentation.Configure(particles);
        instance.SetActive(true);
        //部分预制体的根只是容器；分别启动顶层粒子，事件子发射器交给框架驱动。
        var subEmitters = new HashSet<RimUnity.ParticleSystem>();
        foreach (var system in particles)
            foreach (var entry in system.subEmitters.entries)
                if (entry.system != null) subEmitters.Add(entry.system);
        foreach (var system in particles)
        {
            if (subEmitters.Contains(system)) continue;
            bool hasParticleParent = false;
            for (var parent = system.transform.parent; parent != null; parent = parent.parent)
                if (parent.GetComponent<RimUnity.ParticleSystem>() != null) { hasParticleParent = true; break; }
            if (!hasParticleParent) system.Play(true);
        }
        return instance;
    }
}
