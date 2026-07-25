using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Core.Effects;
using NingshaRaceLib.Petrification.Health;
using NingshaRaceLib.Petrification.Patches;
using NingshaRaceLib.Petrification.Utility;
using NingshaRaceLib.SandGolem.Utility;

namespace NingshaRaceLib.Petrification.Rendering
{
    //类职责：按原始 Pawn 材质缓存使用石化 ShaderPro 生成的对应材质。
    public static class PetrificationMaterialPool
    {
        //类职责：按托管引用比较 Unity 对象，避免工作线程触发 UnityEngine.Object 的重载比较逻辑。
        private sealed class ManagedReferenceComparer<T> : IEqualityComparer<T> where T : class
        {
            //属性职责：提供当前引用类型比较器的唯一共享实例。
            public static readonly ManagedReferenceComparer<T> Instance = new ManagedReferenceComparer<T>();

            //函数职责：只按托管对象引用判断两个对象是否为同一实例。
            public bool Equals(T left, T right)
            {
                return ReferenceEquals(left, right);
            }

            //函数职责：取得不依赖 Unity 原生对象状态的托管引用哈希值。
            public int GetHashCode(T value)
            {
                return RuntimeHelpers.GetHashCode(value);
            }
        }

        //字段职责：保存已经在主线程创建完成的石化材质，允许并行预绘制线程只读查询。
        private static readonly ConcurrentDictionary<Material, Material> Materials =
            new ConcurrentDictionary<Material, Material>(ManagedReferenceComparer<Material>.Instance);

        //函数职责：在渲染树主线程初始化阶段预先创建图形四个朝向及其隐身派生材质对应的石化材质。
        public static void PrewarmGraphic(Graphic graphic, Pawn pawn)
        {
            if (!UnityData.IsInMainThread)
            {
                throw new InvalidOperationException("石化材质只能在游戏主线程预热。");
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
                if (!ReferenceEquals(source, null))
                {
                    CreatePetrifiedMaterial(source);
                    Material invisibleSource = InvisibilityMatPool.GetInvisibleMat(source);
                    if (!ReferenceEquals(invisibleSource, null))
                    {
                        CreatePetrifiedMaterial(invisibleSource);
                    }
                }
            }
        }

        //函数职责：供并行预绘制线程只读取得已预热材质，未命中时保留原材质且不创建 Unity 对象。
        public static Material GetPetrifiedMaterial(Material source)
        {
            if (ReferenceEquals(source, null))
            {
                return null;
            }
            if (Materials.TryGetValue(source, out Material cachedMaterial))
            {
                return cachedMaterial;
            }

            return source;
        }

        //函数职责：在游戏主线程保留原贴图、颜色、渲染队列和贴图变换并创建石化材质。
        private static Material CreatePetrifiedMaterial(Material source)
        {
            if (!UnityData.IsInMainThread)
            {
                throw new InvalidOperationException("石化材质只能在游戏主线程创建。");
            }
            if (Materials.TryGetValue(source, out Material cachedMaterial))
            {
                return cachedMaterial;
            }

            MaterialRequest request = new MaterialRequest(
                source.mainTexture,
                DefOfRefs.NingshaRace_PawnPetrify_ShaderPro.Shader,
                source.color)
            {
                renderQueue = source.renderQueue,
                needsMainTex = source.mainTexture != null
            };
            Material shaderProMaterial = MaterialPool.MatFrom(request);
            if (shaderProMaterial == null)
            {
                throw new InvalidOperationException("无法从 NingshaRace_PawnPetrify_ShaderPro 创建石化材质。");
            }

            Material petrifiedMaterial = new Material(shaderProMaterial);
            petrifiedMaterial.name = source.name + "_Petrified";
            if (source.mainTexture != null)
            {
                petrifiedMaterial.mainTextureScale = source.mainTextureScale;
                petrifiedMaterial.mainTextureOffset = source.mainTextureOffset;
            }
            Materials[source] = petrifiedMaterial;
            return petrifiedMaterial;
        }
    }
}
