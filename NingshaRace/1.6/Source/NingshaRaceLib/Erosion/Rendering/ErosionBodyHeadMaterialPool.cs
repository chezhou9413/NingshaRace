using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Erosion.Rendering
{
    //类职责：在主线程创建并缓存侵蚀体头部的 CL 黑雾材质，供并行预绘制线程只读使用。
    public static class ErosionBodyHeadMaterialPool
    {
        //类职责：按托管引用比较 Unity 材质，避免后台线程调用 UnityEngine.Object 重载逻辑。
        private sealed class ManagedReferenceComparer<T> : IEqualityComparer<T> where T : class
        {
            //属性职责：提供当前引用类型比较器的共享实例。
            public static readonly ManagedReferenceComparer<T> Instance = new ManagedReferenceComparer<T>();

            //函数职责：只按托管对象引用判断两个对象是否为同一实例。
            public bool Equals(T left, T right)
            {
                return ReferenceEquals(left, right);
            }

            //函数职责：取得不触发 Unity 原生对象访问的托管引用哈希值。
            public int GetHashCode(T value)
            {
                return RuntimeHelpers.GetHashCode(value);
            }
        }

        //字段职责：保存原始头部材质到侵蚀黑雾材质的线程安全只读映射。
        private static readonly ConcurrentDictionary<Material, Material> Materials =
            new ConcurrentDictionary<Material, Material>(ManagedReferenceComparer<Material>.Instance);

        //函数职责：在渲染树主线程初始化阶段预先创建头部四个朝向及隐身变体的黑雾材质。
        public static void PrewarmGraphic(Graphic graphic, Pawn pawn)
        {
            if (!UnityData.IsInMainThread)
            {
                throw new InvalidOperationException("侵蚀体头部材质只能在游戏主线程预热。");
            }
            if (graphic == null)
            {
                throw new ArgumentNullException(nameof(graphic));
            }
            if (pawn == null)
            {
                throw new ArgumentNullException(nameof(pawn));
            }

            foreach (Rot4 rotation in Rot4.AllRotations)
            {
                Material source = graphic.NodeGetMat(new PawnDrawParms
                {
                    pawn = pawn,
                    facing = rotation
                });
                if (ReferenceEquals(source, null))
                {
                    continue;
                }

                GetOrCreateMaterial(source);
                Material invisibleSource = InvisibilityMatPool.GetInvisibleMat(source);
                if (!ReferenceEquals(invisibleSource, null))
                {
                    GetOrCreateMaterial(invisibleSource);
                }
            }
        }

        //函数职责：供并行预绘制线程只读取得已经预热的黑雾材质。
        public static Material GetMaterial(Material source)
        {
            if (ReferenceEquals(source, null))
            {
                return null;
            }

            return Materials.TryGetValue(source, out Material cachedMaterial)
                ? cachedMaterial
                : source;
        }

        //函数职责：在主线程保留原头部贴图、颜色和贴图变换并创建 CL 黑雾材质。
        public static Material GetOrCreateMaterial(Material source)
        {
            if (!UnityData.IsInMainThread)
            {
                throw new InvalidOperationException("侵蚀体头部材质只能在游戏主线程创建。");
            }
            if (ReferenceEquals(source, null))
            {
                return null;
            }
            if (Materials.TryGetValue(source, out Material cachedMaterial))
            {
                return cachedMaterial;
            }

            MaterialRequest request = new MaterialRequest(
                source.mainTexture,
                DefOfRefs.NingshaRace_UpperErosionBlackFog_ShaderPro.Shader,
                source.color)
            {
                needsMainTex = source.mainTexture != null
            };
            Material shaderProMaterial = MaterialPool.MatFrom(request);
            if (ReferenceEquals(shaderProMaterial, null))
            {
                throw new InvalidOperationException("无法从 NingshaRace_UpperErosionBlackFog_ShaderPro 创建侵蚀体头部材质。");
            }

            Material erosionMaterial = new Material(shaderProMaterial)
            {
                name = source.name + "_UpperErosionBlackFog",
                mainTextureScale = source.mainTextureScale,
                mainTextureOffset = source.mainTextureOffset
            };
            Materials[source] = erosionMaterial;
            return erosionMaterial;
        }
    }
}
