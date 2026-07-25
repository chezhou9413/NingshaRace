using RimWorld;
using Verse;

using NingshaRaceLib.Combat.GroundSpike.Rendering;
using NingshaRaceLib.Combat.GroundSpike.Tracking;
using NingshaRaceLib.Combat.GroundSpike.Utility;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Combat.GroundSpike.Verbs
{
    //类职责：保存地刺召唤物直线攻击的伤害、范围、动画和击退参数。
    public class VerbProperties_GroundSpikeSummoner : VerbProperties
    {
        //字段职责：指定每个目标受到的固定基础伤害。
        public float damageAmount = 25f;

        //字段职责：指定地刺伤害使用的穿甲比例。
        public float armorPenetration = 0.3f;

        //字段职责：指定地刺伤害类型。
        public DamageDef damageDef = DamageDefOf.Stab;

        //字段职责：指定中心线两侧扩展的伤害格数。
        public int lineHalfWidth = 1;

        //字段职责：指定受伤 Pawn 沿攻击方向飞行的最大格数。
        public int knockbackCells = 1;

        //字段职责：指定相邻地刺横排之间的启动 Tick 间隔。
        public int waveStepTicks = 1;

        //字段职责：指定地刺图集包含的动画帧数。
        public int animationFrameCount = 20;

        //字段职责：指定地刺逐帧动画完整播放所需的游戏 Tick 数。
        public int animationDurationTicks = 80;

        //字段职责：指定每排地刺结算伤害的动画帧编号。
        public int impactFrame = 12;

        //字段职责：指定单个地刺 Mote 的绘制缩放。
        public float effectScale = 3.2f;

        //构造函数职责：指定地刺召唤物使用的自定义远程 Verb。
        public VerbProperties_GroundSpikeSummoner()
        {
            verbClass = typeof(Verb_GroundSpikeSummoner);
        }
    }
}
