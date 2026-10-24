using System.Collections.Generic;
using UnityEngine;

namespace NingshaRaceLib.UI.Motion
{
    //类职责：为即时模式控件保存短期悬停过渡，使用实时时间以支持游戏暂停界面。
    public static class NingshaUiMotion
    {
        //类职责：保存单个交互键的当前插值和最后访问时间。
        private sealed class Sample
        {
            public float value;
            public float time;
        }

        private static readonly Dictionary<string, Sample> samples = new Dictionary<string, Sample>();
        private static readonly List<string> expired = new List<string>();
        private static float nextPrune;

        //函数职责：按时间而非 IMGUI 事件次数插值到目标，定期回收离屏控件状态。
        public static float Hover(string key, bool hovered)
        {
            float now = Time.realtimeSinceStartup;
            if (now >= nextPrune)
            {
                expired.Clear();
                foreach (KeyValuePair<string, Sample> item in samples)
                {
                    if (now - item.Value.time > 3f) expired.Add(item.Key);
                }
                foreach (string id in expired) samples.Remove(id);
                nextPrune = now + 4f;
            }
            if (!samples.TryGetValue(key, out Sample sample))
            {
                sample = new Sample { time = now };
                samples.Add(key, sample);
            }
            sample.value = Mathf.MoveTowards(sample.value, hovered ? 1f : 0f, (now - sample.time) * 7f);
            sample.time = now;
            return sample.value;
        }

        //函数职责：在游戏切换时清空界面短期状态，避免跨存档遗留交互。
        public static void Reset()
        {
            samples.Clear();
            expired.Clear();
            nextPrune = 0f;
        }
    }
}
