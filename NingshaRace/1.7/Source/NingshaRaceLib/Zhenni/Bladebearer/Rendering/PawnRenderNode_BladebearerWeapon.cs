using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //跟踪装备与收容状态，让原版渲染请求缓存及时移除失效的握持节点。
    public sealed class PawnRenderNode_BladebearerWeapon : PawnRenderNode_Parent
    {
        internal readonly CompBladebearerAppearance Owner;
        internal List<PawnRenderNode_BladebearerPart> Parts;
        private bool lastReady;
        internal bool Ready => !tree.pawn.Dead && !tree.pawn.Downed && tree.pawn.Spawned
            && !(tree.pawn.ParentHolder is Building_HoldingPlatform)
            && tree.pawn.equipment.Primary?.def == Owner.WeaponDef;

        //只保存本 Pawn 的外观组件，不创建共享的可变材质。
        public PawnRenderNode_BladebearerWeapon(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree) { Owner = pawn.GetComp<CompBladebearerAppearance>(); }

        //原版装备移除不一定重建渲染树，状态变化时仅刷新绘制请求。
        public override bool RecacheRequested
        {
            get
            {
                bool ready = Ready;
                if (ready != lastReady) { lastReady = ready; return true; }
                return base.RecacheRequested;
            }
        }
    }
}
