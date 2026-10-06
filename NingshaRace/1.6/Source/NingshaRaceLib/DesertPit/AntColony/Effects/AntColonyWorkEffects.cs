using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Effects
{
    //让地底产物出现时扬起少量尘土，不参与资源随机结算。
    internal static class AntColonyWorkEffects
    {
        //仅在当前可见画面播放粒子，并隔离视觉随机数。
        public static void ResourceEmerges(Pawn worker)
        {
            Map map = worker.Map;
            if (map != Find.CurrentMap || worker.Position.Fogged(map)
                || !Find.CameraDriver.CurrentViewRect.Contains(worker.Position)) return;
            Rand.PushState(Gen.HashCombineInt(worker.thingIDNumber, Find.TickManager.TicksGame));
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    FleckCreationData dust = FleckMaker.GetDataStatic(worker.Position.ToVector3Shifted(), map,
                        FleckDefOf.DustPuff, Rand.Range(0.35f, 0.65f));
                    dust.instanceColor = new Color(0.7f, 0.62f, 0.45f);
                    dust.velocityAngle = i * 72f;
                    dust.velocitySpeed = 0.5f;
                    map.flecks.CreateFleck(dust);
                }
            }
            finally
            {
                Rand.PopState();
            }
        }
    }
}
