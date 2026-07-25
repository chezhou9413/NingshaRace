using RimWorld;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Core.Effects;
using NingshaRaceLib.Petrification.Health;
using NingshaRaceLib.Petrification.Patches;
using NingshaRaceLib.Petrification.Rendering;
using NingshaRaceLib.Petrification.Utility;
using NingshaRaceLib.SandGolem.Utility;

namespace NingshaRaceLib.Petrification.Abilities.Components
{
    //类职责：保存石化砂潮的范围、严重度曲线和 ChezhouLib 特效播放参数。
    public class CompProperties_AbilityPetrifyingSandwave : CompProperties_AbilityEffect
    {
        //字段职责：定义砂潮扇形的总角度。
        public float coneAngle = 90f;

        //字段职责：定义直接补满石化严重度的近距离半径。
        public float fullPetrificationRadius = 3f;

        //字段职责：定义超过近距离半径时的起始严重度增量。
        public float nearSeverity = 0.85f;

        //字段职责：定义射程边缘的最低严重度增量。
        public float edgeSeverity = 0.15f;

        //字段职责：定义 ChezhouLib 特效注册使用的模组标识。
        public string effectModId = "NingshaRace";

        //字段职责：定义砂潮主体 Prefab 名称。
        public string waveEffectName = "PetrifyingSandwave";

        //字段职责：定义目标命中 Prefab 名称。
        public string hitEffectName = "PetrifyingSandHit";

        //字段职责：定义砂潮主体的均匀缩放。
        public float waveEffectScale = 1f;

        //字段职责：定义砂潮实例回收前的秒数。
        public float waveEffectLifetime = 3f;

        //字段职责：定义最低严重度命中特效的均匀缩放。
        public float minHitEffectScale = 0.65f;

        //字段职责：定义满严重度命中特效的均匀缩放。
        public float maxHitEffectScale = 1.25f;

        //字段职责：定义命中特效实例回收前的秒数。
        public float hitEffectLifetime = 1.5f;

        //构造函数职责：绑定石化砂潮的能力效果实现。
        public CompProperties_AbilityPetrifyingSandwave()
        {
            compClass = typeof(CompAbilityEffect_PetrifyingSandwave);
        }
    }
}
