using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.DesertPit.Antlion.Components
{
    //类职责：通过种族定义配置蚁狮的感知、追击、加速和潜伏节奏。
    public sealed class CompProperties_AntlionAmbush : CompProperties
    {
        public float triggerRadius = 3f;
        public float chaseRadius = 12f;
        public float burstMultiplier = 1.7f;
        public int burstTicks = 180;
        public int emergeTicks = 18;
        public int submergeTicks = 30;
        public float sandSearchRadius = 12f;
        public int scanIntervalTicks = 10;
        public int unreachableTicks = 120;
        public int sandRetryTicks = 120;
        public int rearmTicks = 120;
        public List<TerrainDef> burrowTerrains;

        //构造职责：将定义绑定到蚁狮伏击组件。
        public CompProperties_AntlionAmbush()
        {
            compClass = typeof(CompAntlionAmbush);
        }

        //函数职责：检查半径、持续时间和沙地列表，防止无效配置引起循环任务或越界扫描。
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef)) yield return error;
            if (!ValidRadius(triggerRadius) || !ValidRadius(sandSearchRadius))
                yield return "蚁狮感知和沙地搜索半径必须为正数且小于原版径向扫描上限。";
            if (float.IsNaN(chaseRadius) || float.IsInfinity(chaseRadius) || chaseRadius < triggerRadius)
                yield return "蚁狮追击半径必须为有限数值且不小于感知半径。";
            if (float.IsNaN(burstMultiplier) || float.IsInfinity(burstMultiplier) || burstMultiplier < 1f)
                yield return "蚁狮突袭移速倍率必须为不小于一的有限数值。";
            if (burstTicks <= 0 || emergeTicks <= 0 || submergeTicks <= 0 || scanIntervalTicks <= 0
                || unreachableTicks <= 0 || sandRetryTicks <= 0 || rearmTicks <= 0)
                yield return "蚁狮持续时间与检查间隔必须大于零。";
            if (burrowTerrains == null || burrowTerrains.Count == 0 || burrowTerrains.Contains(null))
                yield return "蚁狮必须配置有效的可潜伏沙地列表。";
        }

        //函数职责：验证局部扫描半径能由原版预计算格子表完整覆盖。
        private static bool ValidRadius(float radius)
        {
            return !float.IsNaN(radius) && radius > 0f && radius < GenRadial.MaxRadialPatternRadius;
        }
    }
}
