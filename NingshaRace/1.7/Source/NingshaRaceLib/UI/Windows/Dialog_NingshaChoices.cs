using System.Collections.Generic;
using UnityEngine;
using Verse;
using NingshaRaceLib.UI.Controls;
using NingshaRaceLib.UI.Foundation;
using NingshaRaceLib.UI.Layout;
using NingshaRaceLib.UI.Models;

namespace NingshaRaceLib.UI.Windows
{
    //类职责：把模组独立选择菜单呈现为可滚动的铭刻列表，不接管地图右键混合菜单。
    public sealed class Dialog_NingshaChoices : NingshaWindow
    {
        private readonly string title;
        private readonly IReadOnlyList<NingshaChoice> choices;
        private Vector2 scroll;

        //构造职责：记录可选铭刻并建立不因确认键误选条目的选择窗口。
        public Dialog_NingshaChoices(string title, IReadOnlyList<NingshaChoice> choices)
        {
            this.title = title;
            this.choices = choices;
            closeOnCancel = true;
            closeOnAccept = false;
            absorbInputAroundWindow = true;
        }

        //属性职责：为选择列表提供受屏幕约束的可读窗口尺寸。
        public override Vector2 InitialSize => new Vector2(Mathf.Min(500f, Verse.UI.screenWidth), Mathf.Min(400f, Verse.UI.screenHeight));

        //函数职责：在测量过的滚动行内绘制所有选择，点击后关闭窗口并执行对应操作。
        public override void DoWindowContents(Rect inRect)
        {
            using (new NingshaGuiScope(GameFont.Small))
            {
                Rect body = DrawShell(inRect, title, "请选择要进行的操作。");
                float height = NingshaLayout.RowHeight(padding: 20f);
                Rect view = new Rect(0f, 0f, body.width - 18f, Mathf.Max(body.height, choices.Count * (height + 8f)));
                Widgets.BeginScrollView(body, ref scroll, view);
                try
                {
                    for (int i = 0; i < choices.Count; i++)
                    {
                        NingshaChoice choice = choices[i];
                        if (NingshaButton.Draw(new Rect(0f, i * (height + 8f), view.width, height), choice.Label,
                            "choice:" + ID + ":" + i, choice.Action != null, choice.Description))
                        {
                            Close();
                            choice.Action();
                            break;
                        }
                    }
                }
                finally { Widgets.EndScrollView(); }
            }
        }
    }
}
