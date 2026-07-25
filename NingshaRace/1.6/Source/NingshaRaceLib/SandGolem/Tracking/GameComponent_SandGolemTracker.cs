using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.SandGolem.Health;
using NingshaRaceLib.SandGolem.Lifecycle;
using NingshaRaceLib.SandGolem.Rendering;
using NingshaRaceLib.SandGolem.Utility;

namespace NingshaRaceLib.SandGolem.Tracking
{
    //类职责：维护所有沙傀与召唤者的映射、动画推进和读档后的运行时贴图重建。
    public class GameComponent_SandGolemTracker : GameComponent
    {
        //字段职责：保存当前所有沙傀状态。
        private List<SandGolemRenderState> states = new List<SandGolemRenderState>();

        //字段职责：保存等待旧沙傀消散后执行的新召唤请求。
        private List<PendingSandGolemSummon> pendingSummons = new List<PendingSandGolemSummon>();

        //构造函数职责：让 RimWorld 创建游戏组件。
        public GameComponent_SandGolemTracker(Game game)
        {
        }

        //函数职责：获取当前游戏里的沙傀跟踪组件。
        public static GameComponent_SandGolemTracker Current
        {
            get
            {
                return Verse.Current.Game?.GetComponent<GameComponent_SandGolemTracker>();
            }
        }

        //函数职责：保存和读取沙傀状态列表。
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref states, "states", LookMode.Deep);
            Scribe_Collections.Look(ref pendingSummons, "pendingSummons", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && states == null)
            {
                states = new List<SandGolemRenderState>();
            }
            if (Scribe.mode == LoadSaveMode.PostLoadInit && pendingSummons == null)
            {
                pendingSummons = new List<PendingSandGolemSummon>();
            }
        }

        //函数职责：读档后清理失效引用并重建可见沙傀贴图。
        public override void LoadedGame()
        {
            RebuildRuntimeTextures();
        }

        //函数职责：新游戏开始后确保状态列表存在。
        public override void StartedNewGame()
        {
            if (states == null)
            {
                states = new List<SandGolemRenderState>();
            }
            if (pendingSummons == null)
            {
                pendingSummons = new List<PendingSandGolemSummon>();
            }
        }

        //函数职责：每 Tick 推进沙傀汇聚和消散阶段。
        public override void GameComponentTick()
        {
            int tick = Find.TickManager.TicksGame;
            TickPendingSummons(tick);

            for (int i = states.Count - 1; i >= 0; i--)
            {
                SandGolemRenderState state = states[i];
                if (state == null || state.golem == null || state.golem.Destroyed)
                {
                    state?.DestroyRuntimeResources();
                    states.RemoveAt(i);
                    continue;
                }

                SandGolemUtility.MaintainIdentity(state.golem, tick);
                if (state.LocksFacingAndMovement())
                {
                    SandGolemUtility.LockFacingAndMovement(state.golem, state.phase == SandGolemPhase.Dissolving);
                }
                else if (state.golem.pather?.debugDisabled == true)
                {
                    SandGolemUtility.RestoreControlAfterMovementLock(state.golem);
                }

                if (state.phase == SandGolemPhase.Gathering && state.PhaseFinished(tick))
                {
                    state.MarkStable();
                    SandGolemUtility.RestoreControlAfterMovementLock(state.golem);
                    continue;
                }

                if (state.phase == SandGolemPhase.Dissolving && state.PhaseFinished(tick))
                {
                    FinishDissolve(state);
                    states.RemoveAt(i);
                }
            }
        }

        //函数职责：注册新沙傀并替换旧状态。
        public void Register(Pawn caster, Pawn golem, Texture2D[] textures)
        {
            RemoveStateForGolem(golem);
            SandGolemRenderState state = new SandGolemRenderState(caster, golem, textures);
            state.RebuildMaterials(DefOfRefs.NingshaRace_PawnSandify_ShaderPro.Shader);
            states.Add(state);
            SandGolemUtility.LockFacingAndMovement(golem, stopJobs: true);
        }

        //函数职责：尝试获取指定 Pawn 的沙傀渲染状态。
        public bool TryGetState(Pawn golem, out SandGolemRenderState state)
        {
            state = null;
            if (golem == null)
            {
                return false;
            }

            for (int i = 0; i < states.Count; i++)
            {
                if (states[i]?.golem == golem)
                {
                    state = states[i];
                    return true;
                }
            }

            return false;
        }

        //函数职责：获取召唤者当前维持的沙傀。
        public Pawn GolemForCaster(Pawn caster)
        {
            if (caster == null)
            {
                return null;
            }

            for (int i = 0; i < states.Count; i++)
            {
                SandGolemRenderState state = states[i];
                if (state?.caster == caster && state.golem != null && !state.golem.Destroyed)
                {
                    return state.golem;
                }
            }

            return null;
        }

        //函数职责：开始收回召唤者当前沙傀。
        public void RecallGolemForCaster(Pawn caster)
        {
            Pawn golem = GolemForCaster(caster);
            if (golem != null)
            {
                BeginDissolve(golem, destroyPawn: true);
            }
        }

        //函数职责：如果召唤者已有沙傀则先收回旧沙傀，再延迟执行新召唤。
        public void RecallThenSummon(Pawn caster, IntVec3 targetCell)
        {
            Pawn oldGolem = GolemForCaster(caster);
            if (oldGolem == null)
            {
                TrySpawnGolemLogged(caster, targetCell);
                return;
            }

            BeginDissolve(oldGolem, destroyPawn: true);
            RemovePendingForCaster(caster);
            pendingSummons.Add(new PendingSandGolemSummon(caster, targetCell, Find.TickManager.TicksGame + SandGolemUtility.AnimationTicks));
        }

        //函数职责：开始指定沙傀的消散动画。
        public void BeginDissolve(Pawn golem, bool destroyPawn)
        {
            if (!TryGetState(golem, out SandGolemRenderState state))
            {
                Texture2D[] textures = SandGolemPawnCapture.CapturePawn(golem);
                state = new SandGolemRenderState(null, golem, textures);
                state.RebuildMaterials(DefOfRefs.NingshaRace_PawnSandify_ShaderPro.Shader);
                states.Add(state);
            }

            golem.jobs?.StopAll();
            SandGolemUtility.SetMovementDisabled(golem, true);
            golem.Rotation = Rot4.South;
            state.BeginDissolve(destroyPawn);
            golem.Drawer?.renderer?.SetAllGraphicsDirty();
        }

        //函数职责：在读档后根据沙傀自身或召唤者重建运行时截图。
        private void RebuildRuntimeTextures()
        {
            if (states == null)
            {
                states = new List<SandGolemRenderState>();
            }
            if (pendingSummons == null)
            {
                pendingSummons = new List<PendingSandGolemSummon>();
            }
            for (int i = states.Count - 1; i >= 0; i--)
            {
                SandGolemRenderState state = states[i];
                if (state?.golem == null || state.golem.Destroyed)
                {
                    state?.DestroyRuntimeResources();
                    states.RemoveAt(i);
                    continue;
                }

                Pawn captureSource = state.caster != null && !state.caster.Destroyed ? state.caster : state.golem;
                state.ReplaceTextures(SandGolemPawnCapture.CapturePawn(captureSource), DefOfRefs.NingshaRace_PawnSandify_ShaderPro.Shader);
                SandGolemUtility.StripNeedsAndRelations(state.golem);
                SandGolemIdentityCleaner.Clean(state.golem);
                SandGolemUtility.EnsurePlayerControlComponents(state.golem);
                SandGolemUtility.SetMovementDisabled(state.golem, state.LocksFacingAndMovement());
                state.golem.Drawer?.renderer?.SetAllGraphicsDirty();
            }
        }

        //函数职责：完成消散并销毁沙傀 Pawn。
        private static void FinishDissolve(SandGolemRenderState state)
        {
            Pawn golem = state.golem;
            if (golem == null || golem.Destroyed)
            {
                state.DestroyRuntimeResources();
                return;
            }

            SandGolemUtility.SetMovementDisabled(golem, false);
            if (state.destroyAfterDissolve)
            {
                if (golem.Spawned)
                {
                    golem.DeSpawn(DestroyMode.Vanish);
                }

                golem.Destroy(DestroyMode.Vanish);
            }

            state.DestroyRuntimeResources();
        }

        //函数职责：移除指定沙傀的旧状态。
        private void RemoveStateForGolem(Pawn golem)
        {
            for (int i = states.Count - 1; i >= 0; i--)
            {
                if (states[i]?.golem == golem)
                {
                    states[i]?.DestroyRuntimeResources();
                    states.RemoveAt(i);
                }
            }
        }

        //函数职责：执行已经到期的延迟召唤请求。
        private void TickPendingSummons(int tick)
        {
            if (pendingSummons == null)
            {
                pendingSummons = new List<PendingSandGolemSummon>();
            }

            for (int i = pendingSummons.Count - 1; i >= 0; i--)
            {
                PendingSandGolemSummon pending = pendingSummons[i];
                if (pending == null || pending.caster == null || pending.caster.Destroyed)
                {
                    pendingSummons.RemoveAt(i);
                    continue;
                }

                if (tick < pending.executeTick)
                {
                    continue;
                }

                if (pending.caster.Map != null && SandGolemUtility.IsValidSandCell(pending.targetCell, pending.caster.Map, out _))
                {
                    TrySpawnGolemLogged(pending.caster, pending.targetCell);
                }

                pendingSummons.RemoveAt(i);
            }
        }

        //函数职责：执行一次沙傀生成并把异常记录为单条错误，避免失败请求每 Tick 重复抛出。
        private static Pawn TrySpawnGolemLogged(Pawn caster, IntVec3 targetCell)
        {
            try
            {
                return SandGolemFactory.SpawnGolem(caster, targetCell);
            }
            catch (System.Exception ex)
            {
                Log.Error("沙傀召唤失败，已终止本次召唤请求: " + ex);
                return null;
            }
        }

        //函数职责：移除召唤者尚未执行的旧召唤请求。
        private void RemovePendingForCaster(Pawn caster)
        {
            for (int i = pendingSummons.Count - 1; i >= 0; i--)
            {
                if (pendingSummons[i]?.caster == caster)
                {
                    pendingSummons.RemoveAt(i);
                }
            }
        }
    }
}
