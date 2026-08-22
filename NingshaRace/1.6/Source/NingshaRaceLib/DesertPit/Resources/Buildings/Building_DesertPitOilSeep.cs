using RimWorld;
using Verse;
using Verse.Sound;

namespace NingshaRaceLib.DesertPit.Resources.Buildings
{
    //类职责：让油砂渗洞复用原版间歇喷泉的喷汽、声音和热量表现，同时继续推进自身生成组件。
    public sealed class Building_DesertPitOilSeep : Building
    {
        //字段职责：驱动原版间歇喷泉的喷发间隔、烟汽 Fleck 与热量释放。
        private IntermittentSteamSprayer steamSprayer;

        //字段职责：保存当前喷发循环使用的持续声音。
        private Sustainer spraySustainer;

        //字段职责：记录持续声音开始时间，防止异常状态下永久播放。
        private int spraySustainerStartTick = -999;

        //函数职责：建筑进入地图时初始化原版间歇喷泉控制器及其喷发回调。
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            steamSprayer = new IntermittentSteamSprayer(this)
            {
                startSprayCallback = StartSpray,
                endSprayCallback = EndSpray
            };
        }

        //函数职责：每 Tick 同时推进原版 ThingComp 和间歇喷泉表现。
        protected override void Tick()
        {
            base.Tick();
            steamSprayer.SteamSprayerTick();
            if (spraySustainer != null && Find.TickManager.TicksGame > spraySustainerStartTick + 1000)
            {
                Log.Message("油砂渗洞喷发声音持续超过1000 Tick，已强制结束。");
                EndSpray();
            }
        }

        //函数职责：开始喷发时清理邻近积雪并播放原版间歇喷泉持续声音。
        private void StartSpray()
        {
            WeatherBuildupUtility.AddSnowRadial(this.OccupiedRect().RandomCell, Map, 4f, -0.06f);
            spraySustainer = SoundDefOf.GeyserSpray.TrySpawnSustainer(new TargetInfo(Position, Map));
            spraySustainerStartTick = Find.TickManager.TicksGame;
        }

        //函数职责：结束当前喷发持续声音并清空运行时引用。
        private void EndSpray()
        {
            if (spraySustainer == null)
            {
                return;
            }

            spraySustainer.End();
            spraySustainer = null;
        }
    }
}
