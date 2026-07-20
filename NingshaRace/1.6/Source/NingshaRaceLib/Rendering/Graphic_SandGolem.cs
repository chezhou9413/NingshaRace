using NingshaRaceLib.SandGolem;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Rendering
{
    //类职责：根据沙傀当前朝向返回对应截图材质。
    public class Graphic_SandGolem : Graphic_Single
    {
        //函数职责：初始化沙傀截图 Graphic 的基础字段和占位材质。
        public override void Init(GraphicRequest req)
        {
            base.Init(req);
        }

        //函数职责：根据 Pawn 朝向和沙傀状态取得动态材质。
        public override Material NodeGetMat(PawnDrawParms parms)
        {
            GameComponent_SandGolemTracker tracker = GameComponent_SandGolemTracker.Current;
            if (tracker == null || !tracker.TryGetState(parms.pawn, out SandGolemRenderState state))
            {
                return null;
            }

            return state.MaterialFor(state.DrawFacingFor(parms.facing));
        }

        //函数职责：沙傀运行时材质必须通过 NodeGetMat 取得。
        public override Material MatAt(Rot4 rot, Thing thing = null)
        {
            return null;
        }

        //函数职责：沙傀截图本身已经是方向图，不需要按朝向旋转网格。
        public override bool ShouldDrawRotated
        {
            get
            {
                return false;
            }
        }
    }
}
