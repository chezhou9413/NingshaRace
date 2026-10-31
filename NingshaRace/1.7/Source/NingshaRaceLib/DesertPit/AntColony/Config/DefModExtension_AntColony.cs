using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Config
{
    //类职责：承载单个沙漠巨坑蚁巢的数量、活动范围、繁殖和自爆配置。
    public class DefModExtension_AntColony : DefModExtension
    {
        public float flyingNutritionCost = 1f;
        public float breakerNutritionCost = 3f;
        public float upgradeWork = 1200f;
        public int chargeWindup = 180;
        public float chargeRange = 9f;
        public float chargeSpeedFactor = 2f;
        public int chargeAccelerationTicks = 45;
        public int chargeWallStunTicks = 60;
        public int chargeDamage = 40;
        public int chargeKnockback = 2;
        public int chargeCooldown = 20000;
        public float jumpRange = 7f;
        public int jumpCooldown = 15000;
        //字段职责：控制吐酸蚁补员营养、保留口粮和有限受击反击距离。
        public float acidNutritionCost = 2f;
        public float foodReserveDays = 1f;
        public int foodStorageCells = 4;
        public float retaliationRadius = 45f;
        public int retaliationDurationTicks = 2500;
        //字段职责：固定规模蚁群和一级可升级蚁群的工蚁目标数量。
        public int workerTarget = 4;

        //字段职责：固定规模蚁群和一级可升级蚁群的兵蚁目标数量。
        public int soldierTarget = 3;

        //字段职责：限定沙漠巨坑蚁巢的随机初始等级下界。
        public int initialLevelMin = 1;

        //字段职责：限定沙漠巨坑蚁巢的随机初始等级上界。
        public int initialLevelMax = 3;

        //字段职责：限定沙漠巨坑蚁巢的随机最高等级下界。
        public int maximumLevelMin = 3;

        //字段职责：限定沙漠巨坑蚁巢的随机最高等级上界。
        public int maximumLevelMax = 5;

        //字段职责：规定一级升级到二级需要消耗的储藏营养。
        public float level2Nutrition = 10f;

        //字段职责：规定二级升级到三级需要消耗的储藏营养。
        public float level3Nutrition = 50f;

        //字段职责：规定三级升级到四级需要消耗的储藏营养。
        public float level4Nutrition = 100f;

        //字段职责：规定四级升级到五级需要消耗的储藏营养。
        public float level5Nutrition = 500f;

        //字段职责：规定每次升级后的七天冷却。
        public int upgradeCooldownTicks = 420000;

        //蚁穴受击后开始修补施工前需要等待的时间。
        public int repairDelayAfterDamageTicks = 2500;

        //蚁穴相邻两次修补施工之间的间隔。
        public int repairIntervalTicks = 2500;

        //完整修补批次需要消耗的实体储藏营养。
        public float repairNutritionCost = 1f;

        //完整修补批次能够恢复的蚁穴耐久。
        public int repairHitPoints = 100;

        //字段职责：限定完整警报和受击时可补充的爆浆蚁数量。
        public int boomAntCap = 3;

        //字段职责：规定每个蚁巢使用的实体储藏格数量。
        public int storageCellCount = 12;

        //字段职责：规定日常领地警戒半径。
        public float alertRadius = 20f;

        //字段职责：规定兵蚁日常巡逻半径。
        public float soldierPatrolRadius = 12f;

        //字段职责：规定工蚁日常游荡半径。
        public float workerWanderRadius = 9f;

        //字段职责：规定蚁后离开蚁穴的最大活动半径。
        public float queenLeashRadius = 9f;

        //字段职责：规定工蚁遭遇近距离威胁时的撤回半径。
        public float workerRetreatRadius = 8f;

        //撤退期间只防御蚁穴十五格内可见的敌人。
        public float retreatDefenseRadius = 15f;

        //字段职责：规定常规成员伤亡统计的四小时窗口。
        public int retreatLossWindowTicks = 10000;

        //字段职责：规定伤亡阈值触发后的四小时撤退持续时间。
        public int retreatDurationTicks = 10000;

        //字段职责：规定撤退触发所需的常规蚂蚁伤亡比例。
        public float retreatLossFraction = 0.5f;

        //字段职责：规定死亡热点统计的一天时间窗口。
        public int investigationLossWindowTicks = 60000;

        //字段职责：规定死亡热点的聚类半径。
        public float investigationHotspotRadius = 10f;

        //字段职责：规定当前常规上限换算热点门槛的比例。
        public float investigationLossFraction = 0.2f;

        //字段职责：规定死亡热点无论规模至少需要的伤亡数。
        public int investigationMinimumDeaths = 3;

        //字段职责：规定调查队完成派遣后的冷却时间。
        public int investigationCooldownTicks = 60000;

        //字段职责：规定调查队在蚁穴附近集结的最长时间。
        public int investigationRallyTimeoutTicks = 2500;

        //字段职责：规定调查队单程移动的最长时间。
        public int investigationTravelTimeoutTicks = 10000;

        //字段职责：规定调查队在热点防御观察的持续时间。
        public int investigationDefendTicks = 2500;

        //字段职责：规定调查队人数的绝对上限。
        public int investigationMaxSquadSize = 6;

        //字段职责：规定工蚁每批次允许完成的搬运次数。
        public int workerHaulLimit = 5;

        //工蚁完成三趟搬运后休整半天。
        public int workerHaulCooldownTicks = 30000;

        //工蚁每半天挖出一批物资，产物与概率由蚁穴定义提供。
        public int workerResourceIntervalTicks = 30000;
        public System.Collections.Generic.List<AntWorkerResourceYield> workerResourceYields;

        //字段职责：规定蚁后单次繁殖工作的持续时间。
        public int reproductionWorkTicks = 600;

        //字段职责：规定蚁后每次补员后的繁殖冷却。
        public int reproductionCooldownTicks = 2500;

        //字段职责：规定补充一只工蚁需要消耗的营养。
        public float workerNutritionCost = 1f;

        //字段职责：规定补充一只兵蚁需要消耗的营养。
        public float soldierNutritionCost = 1.5f;

        //字段职责：规定蚁穴受击后的完整警报持续时间。
        public int fullAlarmDurationTicks = 2500;

        //字段职责：规定完整警报期间补充爆浆蚁波次的冷却。
        public int boomWaveCooldownTicks = 1200;

        //字段职责：规定爆浆蚁与目标之间的自爆触发距离。
        public float boomTriggerDistance = 1.5f;

        //字段职责：规定爆浆蚁酸液爆炸半径。
        public float boomExplosionRadius = 2.5f;

        //字段职责：规定爆浆蚁酸液爆炸基础伤害。
        public int boomExplosionDamage = 25;

        //函数职责：校验局部采收范围与营养门槛，避免非法半径进入径向搜索。
        public override System.Collections.Generic.IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors()) yield return error;
            if (foodStorageCells < 4 || storageCellCount < foodStorageCells) yield return "蚁巢至少预留四个地面食物格。";
            if (workerHaulLimit <= 0 || reproductionWorkTicks <= 0 || reproductionCooldownTicks <= 0 || upgradeWork <= 0)
                yield return "蚁群工作次数、工作量和冷却必须大于零。";
            if (workerNutritionCost <= 0 || soldierNutritionCost <= 0 || acidNutritionCost <= 0
                || flyingNutritionCost <= 0 || breakerNutritionCost <= 0 || repairNutritionCost <= 0 || foodReserveDays <= 0)
                yield return "蚁群营养费用和保留天数必须大于零。";
            if (chargeRange <= 0 || chargeRange > 30 || jumpRange <= 0 || jumpRange > 30
                || chargeWindup < 1 || chargeCooldown < 1 || jumpCooldown < 1 || chargeSpeedFactor <= 0
                || chargeAccelerationTicks < 1 || chargeWallStunTicks < 1)
                yield return "蚁群冲锋与腾跃配置超出有效范围。";
        }

        //函数职责：根据当前等级返回升到下一等级需要消耗的实体储藏营养。
        public float GetUpgradeNutrition(int currentLevel)
        {
            switch (currentLevel)
            {
                case 1:
                    return level2Nutrition;
                case 2:
                    return level3Nutrition;
                case 3:
                    return level4Nutrition;
                case 4:
                    return level5Nutrition;
                default:
                    return 0f;
            }
        }
    }
}
