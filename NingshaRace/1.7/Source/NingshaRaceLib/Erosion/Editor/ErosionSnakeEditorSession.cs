using System;
using System.Collections.Generic;
using System.Linq;
using NingshaRaceLib.Erosion.Rendering;
using NingshaRaceLib.Erosion.Utility;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Erosion.Editor
{
    //编辑期间只替换选中角色的蛇头节点，关闭后释放全部临时状态。
    internal sealed class ErosionSnakeEditorSession
    {
        internal static ErosionSnakeEditorSession Active { get; private set; }
        internal readonly Pawn Pawn;
        internal readonly List<ErosionSnakeDraft> Snakes;
        internal ErosionSnakePreview Preview;
        internal bool RenderingPreview, Paused;
        internal float Clock;
        private float lastTime;

        //从当前侵蚀体的定义创建草稿，不更改共享 Def。
        internal ErosionSnakeEditorSession(Pawn pawn)
        {
            if (!ErosionPawnUtility.IsNingshaErosionBody(pawn))
                throw new InvalidOperationException("只有凝砂族侵蚀体拥有可编辑的蛇头。");
            Pawn = pawn;
            Snakes = pawn.mutant.Def.RenderNodeProperties.OfType<PawnRenderNodeProperties_ErosionSnake>()
                .Select(props => new ErosionSnakeDraft(props)).ToList();
            if (Snakes.Count == 0) throw new InvalidOperationException("当前侵蚀体没有可编辑的蛇头节点。");
            Clock = Find.TickManager.TicksGame / 60f;
        }

        //正式打开窗口时才安装草稿，并刷新当前角色。
        internal void Begin()
        {
            Active = this; lastTime = Time.realtimeSinceStartup;
            Pawn.Drawer.renderer.SetAllGraphicsDirty();
        }

        //使用真实时间播放，地图保持暂停。
        internal void UpdateClock()
        {
            float now = Time.realtimeSinceStartup;
            if (!Paused) Clock += now - lastTime;
            lastTime = now;
        }

        //节点重建时仅选中角色取得独立属性，其他侵蚀体继续使用 Def。
        internal static PawnRenderNodeProperties PropertiesFor(Pawn pawn, PawnRenderNodeProperties props)
        {
            if (Active == null || Active.Pawn != pawn) return props;
            return Active.Snakes.First(snake => snake.Source.texPath == props.texPath).Props;
        }

        //显隐和排序变化只刷新绘制请求，无需重新创建贴图和整个渲染树。
        internal void Changed()
        {
            foreach (ErosionSnakeDraft snake in Snakes) snake.Apply();
            PawnRenderNode root = Pawn.Drawer.renderer.renderTree.rootNode;
            if (root != null) root.requestRecache = true;
        }

        //清除静态引用并让角色重新使用原始定义。
        internal void End()
        {
            if (Active != this) return;
            Active = null;
            if (!Pawn.Destroyed) Pawn.Drawer.renderer.SetAllGraphicsDirty();
        }
    }
}
