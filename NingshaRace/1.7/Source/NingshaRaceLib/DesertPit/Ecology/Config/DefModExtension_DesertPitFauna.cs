using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Config
{
    //类职责：集中配置沙漠巨坑原版虫巢、洞穴动物数量、物种权重和场景避让距离。
    public class DefModExtension_DesertPitFauna : DefModExtension
    {
        public int hiveCount = 1;
        public IntRange animalCountRange = new IntRange(5, 8);
        public List<PawnKindDef> guaranteedAnimals = new List<PawnKindDef>();
        public List<DesertPitAnimalWeight> animalPool = new List<DesertPitAnimalWeight>();
        public PawnKindDef cappedAnimal;
        public int cappedAnimalCount = 2;
        public float entranceAvoidRadius = 25f;
        public float antNestAvoidRadius = 24f;
        public float hiveSceneRadius = 8f;
        public float animalEntranceAvoidRadius = 12f;
        public float animalSpacing = 8f;
        public int scanBatchSize = 512;
    }
}
