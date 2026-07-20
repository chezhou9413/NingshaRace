using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.Combat
{
    //类职责：记录正在播放蛇腹剑攻击动画的武器，并提供绘制隐藏状态查询。
    public static class SnakeBellySwordRenderState
    {
        private static readonly Dictionary<Thing, int> hiddenWeaponsUntilTick = new Dictionary<Thing, int>();

        //函数职责：让指定武器在当前游戏 Tick 后的一段时间内不绘制。
        public static void HideWeapon(Thing weapon, int durationTicks)
        {
            if (weapon == null)
            {
                return;
            }

            hiddenWeaponsUntilTick[weapon] = Find.TickManager.TicksGame + durationTicks;
        }

        //函数职责：判断指定武器是否仍处于攻击动画隐藏状态。
        public static bool IsHidden(Thing weapon)
        {
            if (weapon == null || !hiddenWeaponsUntilTick.TryGetValue(weapon, out int hiddenUntilTick))
            {
                return false;
            }

            if (Find.TickManager.TicksGame < hiddenUntilTick)
            {
                return true;
            }

            hiddenWeaponsUntilTick.Remove(weapon);
            return false;
        }
    }
}
