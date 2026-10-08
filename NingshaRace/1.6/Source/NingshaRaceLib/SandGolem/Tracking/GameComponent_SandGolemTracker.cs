using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using NingshaRaceLib.SandGolem.Automation;
using NingshaRaceLib.SandGolem.Lifecycle;
using NingshaRaceLib.SandGolem.Rendering;
using NingshaRaceLib.SandGolem.Utility;

namespace NingshaRaceLib.SandGolem.Tracking
{
    //维护召唤者、稳定沙傀和不依赖实体的聚散动画。
    public class GameComponent_SandGolemTracker : GameComponent
    {
        private List<SandGolemRenderState> states = new List<SandGolemRenderState>();
        private List<PendingSandGolemSummon> pendingSummons = new List<PendingSandGolemSummon>();

        //供游戏创建跟踪组件。
        public GameComponent_SandGolemTracker(Game game) { }

        //取得当前游戏中的跟踪组件。
        public static GameComponent_SandGolemTracker Current => Verse.Current.Game?.GetComponent<GameComponent_SandGolemTracker>();

        //保存动画位置、截图、实体引用和延迟召唤。
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref states, "states", LookMode.Deep);
            Scribe_Collections.Look(ref pendingSummons, "pendingSummons", LookMode.Deep);
            if (Scribe.mode != LoadSaveMode.PostLoadInit) return;
            if (states == null) states = new List<SandGolemRenderState>();
            if (pendingSummons == null) pendingSummons = new List<PendingSandGolemSummon>();
        }

        //在读档长事件结束后的主线程重建 Unity 资源。
        public override void LoadedGame() => LongEventHandler.ExecuteWhenFinished(RebuildRuntimeTextures);

        //纯动画逐帧绘制，游戏暂停时保持当前进度。
        public override void GameComponentUpdate()
        {
            int tick = Find.TickManager.TicksGame;
            foreach (SandGolemRenderState state in states) SandGolemAnimationRenderer.Draw(state, tick);
        }

        //聚拢完成才生成实体，消散阶段只推进画面。
        public override void GameComponentTick()
        {
            int tick = Find.TickManager.TicksGame;
            for (int i = states.Count - 1; i >= 0; i--)
            {
                SandGolemRenderState state = states[i];
                if (!IsStateValid(state))
                {
                    if (state.phase == SandGolemPhase.Stable && state.caster != null)
                        GameComponent_SandGolemAutoSummon.Current.NotifyLost(state.caster);
                    RemoveState(i);
                    continue;
                }
                if (state.phase == SandGolemPhase.Gathering && state.PhaseFinished(tick))
                {
                    if (!CompleteGathering(state)) RemoveState(i);
                }
                else if (state.phase == SandGolemPhase.Dissolving && state.PhaseFinished(tick))
                    RemoveState(i);
                else if (state.phase == SandGolemPhase.Stable)
                {
                    SandGolemUtility.MaintainIdentity(state.golem, tick);
                    if (state.LifetimeExpiredAt(tick)) BeginDissolve(state.golem, notifyCaster: true);
                }
            }
            TickPendingSummons(tick);
        }

        //实体与动画采用不同的有效性条件，动画不需要保留 Pawn。
        private static bool IsStateValid(SandGolemRenderState state)
        {
            if (state.phase == SandGolemPhase.Stable) return state.golem != null && !state.golem.Destroyed;
            return state.animationMap != null && Find.Maps.Contains(state.animationMap)
                && (state.phase == SandGolemPhase.Dissolving || IsCasterValid(state.caster));
        }

        //召唤者死亡或被销毁时停止未完成的召唤。
        private static bool IsCasterValid(Pawn caster) => caster != null && !caster.Destroyed && !caster.Dead;

        //查询稳定实体对应的渲染状态。
        public bool TryGetState(Pawn golem, out SandGolemRenderState state)
        {
            state = golem == null ? null : states.Find(item => item.golem == golem);
            return state != null;
        }

        //返回召唤者当前可控制的实体。
        public Pawn GolemForCaster(Pawn caster)
        {
            return states.Find(state => state.caster == caster && state.golem != null && !state.golem.Destroyed)?.golem;
        }

        //自动召唤同时识别聚散动画和等待请求，避免重复施法。
        public bool HasSummonForCaster(Pawn caster)
        {
            return states.Exists(state => state.caster == caster)
                || pendingSummons.Exists(pending => pending.caster == caster);
        }

        //收回实体，或取消尚未成形的召唤。
        public void RecallGolemForCaster(Pawn caster)
        {
            GameComponent_SandGolemAutoSummon.Current.ClearRequest(caster);
            pendingSummons.RemoveAll(pending => pending.caster == caster);
            int index = states.FindIndex(state => state.caster == caster);
            if (index < 0) return;
            if (states[index].golem != null) BeginDissolve(states[index].golem);
            else if (states[index].phase == SandGolemPhase.Gathering) RemoveState(index);
        }

        //替换召唤先等待旧沙傀消散，目标地图固定在施法时。
        public void RecallThenSummon(Pawn caster, IntVec3 targetCell)
        {
            GameComponent_SandGolemAutoSummon.Current.ClearRequest(caster);
            pendingSummons.RemoveAll(pending => pending.caster == caster);
            int index = states.FindIndex(state => state.caster == caster);
            if (index >= 0 && states[index].phase == SandGolemPhase.Gathering)
            {
                RemoveState(index);
                index = -1;
            }
            if (index < 0)
            {
                StartGathering(caster, caster.Map, targetCell);
                return;
            }
            SandGolemRenderState old = states[index];
            if (old.golem != null) BeginDissolve(old.golem);
            pendingSummons.Add(new PendingSandGolemSummon(caster, targetCell,
                old.phaseStartTick + SandGolemUtility.AnimationTicks));
        }

        //先保存原位置，再立即销毁实体；后续动画不会被选取或受击。
        public void BeginDissolve(Pawn golem, bool notifyCaster = false)
        {
            if (!TryGetState(golem, out SandGolemRenderState state)) return;
            state.BeginDissolve();
            state.golem = null;
            if (state.caster != null)
            {
                if (notifyCaster) GameComponent_SandGolemAutoSummon.Current.NotifyLost(state.caster);
                else GameComponent_SandGolemAutoSummon.Current.ClearRequest(state.caster);
            }
            golem.Destroy(DestroyMode.Vanish);
        }

        //截图由状态持有，动画阶段不生成 Pawn。
        private void StartGathering(Pawn caster, Map map, IntVec3 targetCell)
        {
            if (!IsCasterValid(caster) || map == null || !Find.Maps.Contains(map)
                || !SandGolemUtility.IsValidSandCell(targetCell, map, out _)) return;
            Texture2D[] textures = null;
            SandGolemRenderState state = null;
            try
            {
                textures = SandGolemPawnCapture.CapturePawn(caster);
                state = new SandGolemRenderState(caster, map, targetCell, textures);
                state.RebuildMaterials();
                states.Add(state);
            }
            catch (Exception ex)
            {
                if (state != null) state.DestroyRuntimeResources();
                else if (textures != null)
                    foreach (Texture2D texture in textures) UnityEngine.Object.Destroy(texture);
                Log.Error("沙傀聚拢失败，已终止本次召唤请求: " + ex);
            }
        }

        //动画结束时复查地格，在原地图生成唯一实体并交接已有材质。
        private static bool CompleteGathering(SandGolemRenderState state)
        {
            IntVec3 cell = state.animationPosition.ToIntVec3();
            if (!SandGolemUtility.IsValidSandCell(cell, state.animationMap, out string reason))
            {
                Messages.Message("沙傀未能成形：" + reason, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            try
            {
                state.golem = SandGolemFactory.SpawnGolem(state.caster, state.animationMap, cell);
                state.MarkStable();
                SandGolemUtility.RestoreControlAfterMovementLock(state.golem);
                return true;
            }
            catch (Exception ex)
            {
                if (state.golem != null && !state.golem.Destroyed) state.golem.Destroy(DestroyMode.Vanish);
                Log.Error("沙傀生成失败，已终止本次召唤请求: " + ex);
                return false;
            }
        }

        //等待旧沙傀消散后在原先指定的地图开始聚拢。
        private void TickPendingSummons(int tick)
        {
            for (int i = pendingSummons.Count - 1; i >= 0; i--)
            {
                PendingSandGolemSummon pending = pendingSummons[i];
                if (IsCasterValid(pending.caster) && tick < pending.executeTick) continue;
                pendingSummons.RemoveAt(i);
                StartGathering(pending.caster, pending.map, pending.targetCell);
            }
        }

        //读档时分别恢复纯动画与稳定实体，不把无实体动画当作失效记录。
        private void RebuildRuntimeTextures()
        {
            if (!UnityData.IsInMainThread) throw new InvalidOperationException("沙傀读档贴图只能在游戏主线程重建。");
            if (!ReferenceEquals(Current, this)) return;
            for (int i = states.Count - 1; i >= 0; i--)
            {
                SandGolemRenderState state = states[i];
                if (!IsStateValid(state)) { RemoveState(i); continue; }
                state.ReplaceTextures(SandGolemSnapshotStorage.Decode(state.snapshotImages));
                if (state.golem == null) continue;
                SandGolemUtility.StripNeedsAndRelations(state.golem);
                SandGolemIdentityCleaner.Clean(state.golem);
                SandGolemUtility.EnsurePlayerControlComponents(state.golem);
                state.golem.Drawer?.renderer?.SetAllGraphicsDirty();
            }
        }

        //移除记录并释放它独占的截图和材质。
        private void RemoveState(int index)
        {
            states[index].DestroyRuntimeResources();
            states.RemoveAt(index);
        }

        //切换游戏前在主线程释放全部运行时资源。
        public void ReleaseRuntimeResources()
        {
            if (!UnityData.IsInMainThread) throw new InvalidOperationException("沙傀运行时资源只能在游戏主线程清理。");
            foreach (SandGolemRenderState state in states) state.DestroyRuntimeResources();
        }
    }
}
