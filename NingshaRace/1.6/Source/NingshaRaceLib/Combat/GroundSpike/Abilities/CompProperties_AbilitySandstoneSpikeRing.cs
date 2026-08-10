using RimWorld;
using Verse;

namespace NingshaRaceLib.Combat.GroundSpike.Abilities
{
    //类职责：保存砂岩棘环的范围、伤害、逐环推进、动画和击退参数。
    public sealed class CompProperties_AbilitySandstoneSpikeRing : CompProperties_AbilityEffect
    {
        //字段职责：指定环形地刺覆盖的最大半径。
        public float radius = 2f;

        //字段职责：指定地刺使用的伤害类型。
        public DamageDef damageDef = DamageDefOf.Stab;

        //字段职责：指定每个目标受到的固定基础伤害。
        public float damageAmount = 27f;

        //字段职责：指定地刺伤害使用的穿甲比例。
        public float armorPenetration = 0.5f;

        //字段职责：指定受伤 Pawn 沿远离圆心方向飞行的最大格数。
        public int knockbackCells = 1;

        //字段职责：指定相邻圆环之间的启动 Tick 间隔。
        public int ringStepTicks = 4;

        //字段职责：指定地刺图集包含的动画帧数。
        public int animationFrameCount = 20;

        //字段职责：指定地刺逐帧动画完整播放所需的游戏 Tick 数。
        public int animationDurationTicks = 80;

        //字段职责：指定每圈地刺结算伤害的动画帧编号。
        public int impactFrame = 12;

        //字段职责：指定单个地刺 Mote 的绘制缩放。
        public float effectScale = 3.2f;

        //构造函数职责：把砂岩棘环参数绑定到对应能力效果组件。
        public CompProperties_AbilitySandstoneSpikeRing()
        {
            compClass = typeof(CompAbilityEffect_SandstoneSpikeRing);
        }
    }
}
