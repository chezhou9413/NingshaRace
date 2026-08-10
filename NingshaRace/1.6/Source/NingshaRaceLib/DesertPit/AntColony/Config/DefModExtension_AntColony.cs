using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Config
{
    //类职责：承载单个沙漠巨坑蚁巢的数量、活动范围、繁殖和自爆配置。
    public class DefModExtension_AntColony : DefModExtension
    {
        public int workerTarget = 4;
        public int soldierTarget = 3;
        public int regularAntCap = 8;
        public int boomAntCap = 3;
        public int storageCellCount = 12;
        public float secondColonyChance = 0.45f;
        public float alertRadius = 20f;
        public float soldierPatrolRadius = 12f;
        public float workerWanderRadius = 8f;
        public float queenLeashRadius = 5f;
        public float workerRetreatRadius = 8f;
        public float frenzyRadius = 40f;
        public int workerHaulLimit = 3;
        public int workerHaulCooldownTicks = 2500;
        public int reproductionWorkTicks = 600;
        public int reproductionCooldownTicks = 2500;
        public float workerNutritionCost = 1f;
        public float soldierNutritionCost = 1.5f;
        public int fullAlarmDurationTicks = 2500;
        public int boomWaveCooldownTicks = 1200;
        public float boomTriggerDistance = 1.5f;
        public float boomExplosionRadius = 2.5f;
        public int boomExplosionDamage = 25;
    }
}
