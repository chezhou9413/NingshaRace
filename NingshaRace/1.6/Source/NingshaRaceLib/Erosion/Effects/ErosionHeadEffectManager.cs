using System.Collections.Generic;
using ChezhouLib.ALLmap;
using RimWorld.Planet;
using UnityEngine;
using Verse;

using NingshaRaceLib.Erosion.Utility;

namespace NingshaRaceLib.Erosion.Effects
{
    //类职责：从 ChezhouLib 普通预制体表创建侵蚀烟雾，并按实际头部渲染矩阵维护每个侵蚀体唯一的运行时实例。
    public static class ErosionHeadEffectManager
    {
        //字段职责：保存 ChezhouLib 中侵蚀烟雾预制体的显式挂载键。
        private const string PrefabKey = "NingshaRace_QinshiYanwu";

        //字段职责：让烟雾稍高于头部贴图，避免与头部处于完全相同的高度层。
        private const float HeadAltitudeOffset = 0.01f;

        //字段职责：让烟雾沿头部局部坐标向画面下方偏移少量距离。
        private const float HeadLocalDownOffset = -0.08f;

        //字段职责：把侵蚀烟雾缩放为预制体原始尺寸的百分之十五。
        private const float EffectScaleMultiplier = 0.15f;

        //字段职责：记录每个侵蚀体当前拥有的非存档 Unity 实例。
        private static readonly Dictionary<Pawn, GameObject> Instances = new Dictionary<Pawn, GameObject>();

        //函数职责：在头部完成绘制时创建或更新烟雾实例，使其继承倒地、爬行和动画后的最终头部位置与旋转。
        public static void UpdateForDraw(Pawn pawn, Matrix4x4 headMatrix)
        {
            if (!CanDisplayOnCurrentMap(pawn))
            {
                DestroyFor(pawn);
                return;
            }

            if (!Instances.TryGetValue(pawn, out GameObject instance) || instance == null)
            {
                instance = CreateInstance(pawn);
                if (instance == null)
                {
                    return;
                }
            }

            Vector3 localOffset = new Vector3(0f, 0f, HeadLocalDownOffset);
            Vector3 position = headMatrix.MultiplyPoint3x4(localOffset);
            position.y += HeadAltitudeOffset;
            instance.transform.SetPositionAndRotation(position, headMatrix.rotation);
        }

        //函数职责：在跟随器主动销毁实例时同步清理索引，避免读档或切图后保留旧引用。
        public static void NotifyInstanceDestroyed(Pawn pawn, GameObject instance)
        {
            if (pawn != null
                && Instances.TryGetValue(pawn, out GameObject registered)
                && registered == instance)
            {
                Instances.Remove(pawn);
            }
        }

        //函数职责：判断 Pawn 是否为当前打开地图中可显示特效的存活侵蚀体。
        private static bool CanDisplayOnCurrentMap(Pawn pawn)
        {
            return Current.ProgramState == ProgramState.Playing
                && WorldRendererUtility.DrawingMap
                && pawn != null
                && !pawn.Destroyed
                && !pawn.Dead
                && pawn.Spawned
                && pawn.Map != null
                && Find.CurrentMap == pawn.Map
                && ErosionPawnUtility.IsErosionBody(pawn);
        }

        //函数职责：从 ChezhouLib 资源表实例化烟雾、重置粒子并安装地图生命周期跟随器。
        private static GameObject CreateInstance(Pawn pawn)
        {
            if (!abDatabase.prefabDataBase.TryGetValue(PrefabKey, out GameObject prefab) || prefab == null)
            {
                Log.ErrorOnce("[NingshaRace] ChezhouLib 普通预制体未挂载：" + PrefabKey, PrefabKey.GetHashCode());
                return null;
            }

            GameObject instance = Object.Instantiate(prefab);
            instance.name = PrefabKey + "_" + pawn.thingIDNumber;
            instance.SetActive(false);
            instance.transform.localScale *= EffectScaleMultiplier;

            ParticleSystem[] particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                particleSystems[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            ErosionHeadEffectFollower follower = instance.AddComponent<ErosionHeadEffectFollower>();
            follower.Bind(pawn);
            Instances[pawn] = instance;
            instance.SetActive(true);

            for (int i = 0; i < particleSystems.Length; i++)
            {
                particleSystems[i].Play(false);
            }

            return instance;
        }

        //函数职责：销毁指定 Pawn 的运行时特效并清除索引，存档中不保存任何 Unity 对象。
        private static void DestroyFor(Pawn pawn)
        {
            if (pawn == null || !Instances.TryGetValue(pawn, out GameObject instance))
            {
                return;
            }

            Instances.Remove(pawn);
            if (instance != null)
            {
                instance.SetActive(false);
                Object.Destroy(instance);
            }
        }
    }
}
