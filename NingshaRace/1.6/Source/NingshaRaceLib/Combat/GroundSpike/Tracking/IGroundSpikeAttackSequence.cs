namespace NingshaRaceLib.Combat.GroundSpike.Tracking
{
    //接口职责：统一直线与环形地刺攻击在游戏组件中的逐 Tick 推进协议。
    public interface IGroundSpikeAttackSequence
    {
        //函数职责：推进攻击状态并返回当前序列是否已完成全部伤害结算。
        bool Tick(int currentTick);
    }
}
