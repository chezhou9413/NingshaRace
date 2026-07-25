using ChezhouLib.ALLmap;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Core.Effects
{
    //类职责：从 ChezhouLib 普通预制体表直接创建一次性粒子实例并管理其自然结束后的销毁。
    public static class DirectPrefabEffectUtility
    {
        //函数职责：按显式资源 key 创建预制体，重置粒子后播放，并在生命周期结束时销毁实例。
        public static void Spawn(
            string prefabKey,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            float lifetime)
        {
            if (!abDatabase.prefabDataBase.TryGetValue(prefabKey, out GameObject prefab) || prefab == null)
            {
                Log.Error("[NingshaRace] ChezhouLib 普通预制体未挂载：" + prefabKey);
                return;
            }

            GameObject instance = Object.Instantiate(prefab, position, rotation);
            instance.name = prefabKey + "_Instance";
            instance.SetActive(false);
            instance.transform.localScale = scale;

            ParticleSystem[] particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                particleSystems[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            instance.SetActive(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                particleSystems[i].Play(false);
            }

            Object.Destroy(instance, Mathf.Max(0.1f, lifetime));
        }
    }
}
