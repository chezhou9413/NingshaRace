using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NingshaRaceLib.Combat.SaltedFishGreatsword
{
    //地图上的临时漫画文字，使用屏幕字号保持远近缩放下的辨识度。
    public sealed class SaltedFishChatOverlay : MapComponent
    {
        private readonly List<SaltedFishQuote> quotes = new List<SaltedFishQuote>();
        private readonly System.Random random = new System.Random();
        private GUIStyle letterStyle;

        //台词只属于当前画面，不写入存档。
        public SaltedFishChatOverlay(Map map) : base(map) { }

        //可见攻击每次抽取一句，避免同一角色连续抽到相同内容。
        internal void Add(Pawn pawn)
        {
            if (Find.CurrentMap != map || pawn.Position.Fogged(map)) return;
            int previous = -1;
            int count = 0;
            for (int i = quotes.Count - 1; i >= 0; i--)
            {
                if (quotes[i].PawnId != pawn.thingIDNumber) continue;
                if (previous < 0) previous = quotes[i].LineIndex;
                if (++count >= 3) quotes.RemoveAt(i);
            }
            int line = random.Next(SaltedFishQuote.Lines.Length - (previous >= 0 ? 1 : 0));
            if (previous >= 0 && line >= previous) line++;
            if (quotes.Count >= 16) quotes.RemoveAt(0);
            quotes.Add(new SaltedFishQuote(line, pawn.thingIDNumber, pawn.DrawPos,
                (float)random.NextDouble() * Mathf.PI * 2f));
            DefDatabase<SoundDef>.GetNamed("NingshaRace_SaltedFishBoing")
                .PlayOneShot(new TargetInfo(pawn.Position, map));
        }

        //使用真实秒数保持高速游戏下的阅读时间，暂停时冻结动画。
        public override void MapComponentUpdate()
        {
            if (Find.TickManager.Paused) return;
            float delta = Time.unscaledDeltaTime;
            for (int i = quotes.Count - 1; i >= 0; i--)
            {
                SaltedFishQuote quote = quotes[i];
                quote.Age += delta;
                if (quote.Age >= quote.Lifetime) { quotes.RemoveAt(i); continue; }
                float stack = 0f;
                for (int j = i + 1; j < quotes.Count; j++)
                    if (quotes[j].PawnId == quote.PawnId) stack += 108f;
                quote.StackOffset = Mathf.Lerp(quote.StackOffset, stack, 1f - Mathf.Exp(-8f * delta));
            }
        }

        //仅重绘时提交文字，并恢复共享 GUI 状态。
        public override void MapComponentOnGUI()
        {
            if (Find.CurrentMap != map || quotes.Count == 0 || Event.current.type != EventType.Repaint) return;
            Color oldColor = GUI.color;
            GameFont oldFont = Text.Font;
            try
            {
                if (letterStyle == null)
                {
                    Text.Font = GameFont.Medium;
                    letterStyle = new GUIStyle(Text.CurFontStyle)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = false,
                        richText = false,
                        fontStyle = FontStyle.Bold
                    };
                    letterStyle.normal.textColor = Color.white;
                }
                foreach (SaltedFishQuote quote in quotes) DrawQuote(quote);
            }
            finally
            {
                GUI.color = oldColor;
                Text.Font = oldFont;
            }
        }

        //长句分行，各字依次放大弹跳，停留时轻摆，最后逐字向外飘散。
        private void DrawQuote(SaltedFishQuote quote)
        {
            Vector2 anchor = Find.Camera.WorldToScreenPoint(quote.Position) / Prefs.UIScale;
            anchor.y = Verse.UI.screenHeight - anchor.y;
            if (anchor.x < -80f || anchor.x > Verse.UI.screenWidth + 80f
                || anchor.y < -80f || anchor.y > Verse.UI.screenHeight + 80f) return;
            int columns = Mathf.Clamp((Verse.UI.screenWidth - 48) / 30, 1, 14);
            int rows = Mathf.CeilToInt(quote.Letters.Length / (float)columns);
            float width = Mathf.Min(columns, quote.Letters.Length) * 30f;
            float left = Mathf.Clamp(anchor.x - width / 2f, 24f, Mathf.Max(24f, Verse.UI.screenWidth - width - 24f));
            float top = anchor.y - 54f - rows * 42f - quote.StackOffset - quote.Age * 7f;
            for (int i = 0; i < quote.Letters.Length; i++)
            {
                float age = quote.Age - i * SaltedFishQuote.LetterDelay;
                if (age < 0f) continue;
                float fade = Mathf.Clamp01((quote.Age - quote.FadeStart - i * 0.008f) / 0.8f);
                float pop = Mathf.Exp(-age * 8f) * Mathf.Sin(age * 19f);
                float phase = quote.Phase + i * 0.7f;
                float x = left + (i % columns + 0.5f) * 30f
                    + Mathf.Sin(quote.Age * 3.5f + phase) * 2f + Mathf.Sin(phase) * fade * 28f;
                float y = top + (i / columns) * 42f - pop * 19f
                    + Mathf.Sin(quote.Age * 4.5f + phase) * 3f - fade * fade * 52f;
                letterStyle.fontSize = Mathf.RoundToInt((26f + pop * 13f) * (1f - fade * 0.25f));
                float height = letterStyle.CalcSize(new GUIContent(quote.Letters[i])).y + 12f;
                Rect rect = new Rect(x - 27f, y - height / 2f, 54f, height);
                float alpha = Mathf.Clamp01(age / 0.055f) * (1f - fade);
                DrawLetter(rect, quote.Letters[i], alpha, (i & 1) == 0);
            }
        }

        //深色描边配金黄、粉白交错文字，亮色地面上仍然清楚可读。
        private void DrawLetter(Rect rect, string letter, float alpha, bool gold)
        {
            GUI.color = new Color(0.12f, 0.055f, 0.14f, alpha);
            GUI.Label(new Rect(rect.x - 2f, rect.y, rect.width, rect.height), letter, letterStyle);
            GUI.Label(new Rect(rect.x + 2f, rect.y, rect.width, rect.height), letter, letterStyle);
            GUI.Label(new Rect(rect.x, rect.y - 2f, rect.width, rect.height), letter, letterStyle);
            GUI.Label(new Rect(rect.x, rect.y + 2f, rect.width, rect.height), letter, letterStyle);
            GUI.color = gold ? new Color(1f, 0.88f, 0.32f, alpha) : new Color(1f, 0.77f, 0.9f, alpha);
            GUI.Label(rect, letter, letterStyle);
        }
    }
}
