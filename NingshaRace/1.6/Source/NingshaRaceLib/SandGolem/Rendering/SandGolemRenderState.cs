using System;
using ChezhouLib.LibDef;
using ChezhouLib.ObjectPool;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.SandGolem.Health;
using NingshaRaceLib.SandGolem.Lifecycle;
using NingshaRaceLib.SandGolem.Tracking;
using NingshaRaceLib.SandGolem.Utility;

namespace NingshaRaceLib.SandGolem.Rendering
{
    //类职责：保存单个沙傀的运行时渲染纹理、动画阶段和召唤者引用。
    public class SandGolemRenderState : IExposable
    {
        //字段职责：记录召唤该沙傀的 Pawn。
        public Pawn caster;

        //字段职责：记录当前沙傀 Pawn。
        public Pawn golem;

        //字段职责：记录沙傀运行时四方向截图纹理，读档后会重新捕获。
        public Texture2D[] textures;

        //字段职责：记录主线程创建好的四方向沙偶材质，渲染线程只读取不创建。
        public Material[] materials;

        //字段职责：记录当前动画开始的游戏 Tick。
        public int phaseStartTick;

        //字段职责：记录沙傀稳定存在阶段结束的绝对游戏 Tick。
        public int expireTick;

        //字段职责：记录沙傀当前生命周期阶段。
        public SandGolemPhase phase;

        //字段职责：记录消散完成后是否销毁 Pawn。
        public bool destroyAfterDissolve;

        //构造函数职责：为 Scribe 反序列化提供空实例。
        public SandGolemRenderState()
        {
        }

        //构造函数职责：创建指定召唤者和沙傀的运行时状态。
        public SandGolemRenderState(Pawn caster, Pawn golem, Texture2D[] textures)
        {
            this.caster = caster;
            this.golem = golem;
            this.textures = textures;
            phase = SandGolemPhase.Gathering;
            phaseStartTick = Find.TickManager.TicksGame;
            expireTick = phaseStartTick + SandGolemUtility.AnimationTicks + SandGolemUtility.LifetimeTicks;
        }

        //函数职责：保存和读取沙傀状态中可持久化的引用和阶段数据。
        public void ExposeData()
        {
            Scribe_References.Look(ref caster, "caster");
            Scribe_References.Look(ref golem, "golem");
            Scribe_Values.Look(ref phaseStartTick, "phaseStartTick");
            Scribe_Values.Look(ref expireTick, "expireTick", -1);
            Scribe_Values.Look(ref phase, "phase", SandGolemPhase.Gathering);
            Scribe_Values.Look(ref destroyAfterDissolve, "destroyAfterDissolve");
        }

        //函数职责：按当前朝向返回沙傀截图纹理。
        public Texture2D TextureFor(Rot4 facing)
        {
            if (textures == null || facing.AsInt < 0 || facing.AsInt >= textures.Length)
            {
                return BaseContent.WhiteTex;
            }

            return textures[facing.AsInt] ?? BaseContent.WhiteTex;
        }

        //函数职责：按当前朝向返回主线程预建的沙偶材质。
        public Material MaterialFor(Rot4 facing)
        {
            if (materials == null || facing.AsInt < 0 || facing.AsInt >= materials.Length)
            {
                return null;
            }

            return materials[facing.AsInt];
        }

        //函数职责：判断指定朝向是否已经有可绘制材质。
        public bool HasMaterialFor(Rot4 facing)
        {
            return MaterialFor(facing) != null;
        }

        //函数职责：判断当前阶段是否需要锁定朝向和移动。
        public bool LocksFacingAndMovement()
        {
            return phase == SandGolemPhase.Gathering || phase == SandGolemPhase.Dissolving;
        }

        //函数职责：根据阶段返回实际绘制使用的朝向。
        public Rot4 DrawFacingFor(Rot4 facing)
        {
            return LocksFacingAndMovement() ? Rot4.South : facing;
        }

        //函数职责：根据当前截图纹理从 ChezhouLib 模板创建本状态独占的四方向材质。
        public void RebuildMaterials()
        {
            if (!UnityData.IsInMainThread)
            {
                throw new InvalidOperationException("沙傀运行时材质只能在游戏主线程创建。");
            }

            DestroyMaterials();
            materials = new Material[Rot4.RotationCount];
            if (textures == null)
            {
                return;
            }

            ClShaderPro shaderPro = DefOfRefs.NingshaRace_PawnSandify_ShaderPro as ClShaderPro;
            Material template = shaderPro?.ClShaderMaterial == null
                ? null
                : ClMaterialPool.GetByDefName(shaderPro.ClShaderMaterial.defName);
            if (template == null)
            {
                throw new InvalidOperationException("无法取得 NingshaRace_PawnSandify_Material 模板材质。");
            }

            foreach (Rot4 rotation in Rot4.AllRotations)
            {
                Texture2D texture = TextureFor(rotation);
                materials[rotation.AsInt] = new Material(template)
                {
                    name = "NingshaRace_SandGolem_" + rotation,
                    mainTexture = texture,
                    color = Color.white,
                    renderQueue = 3000
                };
            }
        }

        //函数职责：替换运行时截图纹理，并释放旧截图占用的 Unity 资源。
        public void ReplaceTextures(Texture2D[] newTextures)
        {
            DestroyRuntimeResources();
            textures = newTextures;
            RebuildMaterials();
        }

        //函数职责：释放沙傀状态独占的材质和截图纹理。
        public void DestroyRuntimeResources()
        {
            if (!UnityData.IsInMainThread)
            {
                throw new InvalidOperationException("沙傀运行时资源只能在游戏主线程清理。");
            }

            DestroyMaterials();
            if (textures != null)
            {
                for (int i = 0; i < textures.Length; i++)
                {
                    Texture2D texture = textures[i];
                    if (texture != null && texture != BaseContent.WhiteTex)
                    {
                        UnityEngine.Object.Destroy(texture);
                    }
                }
            }

            textures = null;
        }

        //函数职责：销毁当前状态独占的四方向材质并清空引用。
        private void DestroyMaterials()
        {
            if (materials == null)
            {
                return;
            }

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material != null)
                {
                    UnityEngine.Object.Destroy(material);
                }
            }
            materials = null;
        }

        //函数职责：计算当前沙偶 Shader 进度值。
        public float SandProgressAt(int tick)
        {
            float t = Mathf.Clamp01((tick - phaseStartTick) / (float)SandGolemUtility.AnimationTicks);
            if (phase == SandGolemPhase.Gathering)
            {
                return Mathf.Lerp(0f, 0.5f, t);
            }

            if (phase == SandGolemPhase.Dissolving)
            {
                return Mathf.Lerp(0.5f, 1f, t);
            }

            return 0.5f;
        }

        //函数职责：判断当前动画阶段是否已经完成。
        public bool PhaseFinished(int tick)
        {
            return tick - phaseStartTick >= SandGolemUtility.AnimationTicks;
        }

        //函数职责：返回指定 Tick 时沙傀稳定阶段尚可存在的 Tick 数，汇聚阶段显示完整寿命。
        public int RemainingLifetimeTicksAt(int tick)
        {
            if (phase == SandGolemPhase.Gathering)
            {
                return SandGolemUtility.LifetimeTicks;
            }
            return Mathf.Max(0, expireTick - tick);
        }

        //函数职责：返回指定 Tick 时沙傀寿命条使用的零至一比例。
        public float LifetimeRatioAt(int tick)
        {
            return Mathf.Clamp01(RemainingLifetimeTicksAt(tick) / (float)SandGolemUtility.LifetimeTicks);
        }

        //函数职责：判断稳定阶段沙傀是否已经达到绝对到期时间。
        public bool LifetimeExpiredAt(int tick)
        {
            return phase == SandGolemPhase.Stable && tick >= expireTick;
        }

        //函数职责：切换到沙傀稳定存在阶段。
        public void MarkStable()
        {
            phase = SandGolemPhase.Stable;
            phaseStartTick = Find.TickManager.TicksGame;
        }

        //函数职责：切换到沙傀消散阶段。
        public void BeginDissolve(bool destroyPawn)
        {
            if (phase == SandGolemPhase.Dissolving)
            {
                destroyAfterDissolve = destroyAfterDissolve || destroyPawn;
                return;
            }

            phase = SandGolemPhase.Dissolving;
            phaseStartTick = Find.TickManager.TicksGame;
            destroyAfterDissolve = destroyPawn;
        }
    }

    //枚举职责：描述沙傀当前的可视生命周期阶段。
    public enum SandGolemPhase
    {
        Gathering,
        Stable,
        Dissolving
    }
}
