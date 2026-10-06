using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //近战美术面板共用 Pawn 相机，只编辑当前会话的握点、关键帧和刀光。
    internal sealed class MeleeAnimationPanel
    {
        private readonly Window_MeleeAnimationEditor editor;
        private readonly Dictionary<string, string> buffers = new Dictionary<string, string>();
        private Vector2 scroll;
        private float y, width, height = 1500;
        private int selected, keyIndex = 3;
        private int inputFacing = -1;
        private bool loop = true, timing, effects = true, pose = true, holding;
        internal bool Dirty;
        private float Row => Mathf.Max(32, Text.LineHeight + 10);
        private MeleeAnimationDef Draft => editor.Owner.Draft;
        private MeleeAnimationMove Move => Draft.moves[selected];
        private MeleeAnimationPlayback Play => editor.Owner.PreviewPlayback;

        //界面不会改变任何战斗或装备状态。
        internal MeleeAnimationPanel(Window_MeleeAnimationEditor editor) { this.editor = editor; }

        //导入后清除旧字段缓存，预览从第一套动作开始。
        internal void Reset() { selected = 0; keyIndex = 0; buffers.Clear(); Play.Clear(); Dirty = false; }

        //在窗口时钟中播放，游戏保持暂停。
        internal void Update()
        {
            if (Play.Move == null) return;
            if (Play.Facing != editor.Preview.Facing)
            { float elapsed = Play.Elapsed; Start(); Play.Seek(elapsed, editor.Owner.Clock); }
            Play.Advance(editor.Owner.Clock);
            if (!Play.Active && loop) Start();
        }

        //当前方向使用原版朝向的近战瞄准角，和地图战斗共用曲线。
        private void Start()
        {
            Play.Start(Move, editor.Preview.Facing, editor.Owner.Clock, true);
            editor.Preview.Invalidate();
        }

        //所有参数位于可滚动区，窗口底部文件操作始终可见。
        internal void Draw(Rect rect)
        {
            if (inputFacing != editor.Preview.Facing.AsInt)
            { inputFacing = editor.Preview.Facing.AsInt; buffers.Clear(); GUI.FocusControl(null); }
            width = rect.width - 20; y = 0;
            Widgets.BeginScrollView(rect, ref scroll, new Rect(0, 0, width, height));
            try
            {
                if (Widgets.ButtonText(Next(), "动作：" + Move.label))
                    Find.WindowStack.Add(new FloatMenu(Draft.moves.Select((m, i) => new FloatMenuOption(m.label, () =>
                    { selected = i; keyIndex = 0; buffers.Clear(); Start(); })).ToList()));
                string label = Widgets.TextField(Next(), Move.label);
                if (label != Move.label) { Move.label = label; Changed(); }
                Rect actions = Next();
                if (Widgets.ButtonText(new Rect(actions.x, actions.y, (width - 6) / 2, Row), "播放此招"))
                { editor.Owner.Paused = false; Start(); }
                if (Widgets.ButtonText(new Rect((width + 6) / 2, actions.y, (width - 6) / 2, Row), "返回待机")) Play.Clear();
                Widgets.CheckboxLabeled(Next(), "循环预览（不造成伤害）", ref loop);
                if (Play.Move != null)
                {
                    float end = Play.Duration + Play.Stop + Mathf.Max(Move.trailLifetime, Move.sparkLifetime);
                    Widgets.Label(Next(), "播放位置（秒）：" + Play.Elapsed.ToString("F2"));
                    float time = Widgets.HorizontalSlider(Next(), Mathf.Min(Play.Elapsed, end), 0, end);
                    if (Mathf.Abs(time - Mathf.Min(Play.Elapsed, end)) > 0.0001f)
                    { editor.Owner.Paused = true; Play.Seek(time, editor.Owner.Clock); editor.Preview.Invalidate(); }
                }
                Number("weight", "随机选用权重", ref Move.weight, 0, 100);
                Group("全方向握点校准", ref holding);
                if (holding) DrawHold();
                Group("起手、命中与收刀节奏", ref timing);
                if (timing) DrawTiming();
                Group(Window_MeleeAnimationEditor.FacingLabel(editor.Preview.Facing.ToStringWord()) + " · 关键帧", ref pose);
                if (pose) DrawPose();
                Group("紫焰刀光与飘散火屑", ref effects);
                if (effects) DrawEffects();
                if (Widgets.ButtonText(Next(), "复制为新动作"))
                {
                    var copy = Move.Copy(); int suffix = 2;
                    while (Draft.moves.Any(m => m.id == Move.id + "_" + suffix)) suffix++;
                    copy.id = Move.id + "_" + suffix; copy.label += " 副本";
                    Draft.moves.Add(copy); selected = Draft.moves.Count - 1; buffers.Clear(); Changed(); Start();
                }
                if (Draft.moves.Count > 1 && Widgets.ButtonText(Next(), "删除此动作"))
                { Draft.moves.RemoveAt(selected); selected = 0; buffers.Clear(); Changed(); Start(); }
                if (Widgets.ButtonText(Next(), "恢复此动作默认参数"))
                {
                    var original = editor.Owner.Definition.moves.FirstOrDefault(m => m.id == Move.id);
                    if (original == null) editor.Status = "这是复制的动作，没有 Mod 默认版本。";
                    else { Draft.moves[selected] = original.Copy(); buffers.Clear(); Changed(); Start(); }
                }
                height = y + 8;
            }
            finally { Widgets.EndScrollView(); }
        }

        //连击次数、间隔和首次接触都可配置，时间提示直接显示实际秒数。
        private void DrawTiming()
        {
            Number("duration", "动作时长（秒）", ref Move.duration, 0.25f, 2);
            Number("contact", "首次伤害接触点（进度）", ref Move.contact, 0.05f, 0.85f);
            float count = Move.strikeCount;
            if (Number("strikeCount", "连续出手次数", ref count, 1, 3))
            {
                Move.strikeCount = Mathf.RoundToInt(count);
                if (Move.strikeCount > 1) Move.hitstop = 0;
                Changed();
            }
            if (Move.strikeCount == 1) Number("hitstop", "命中停顿（秒）", ref Move.hitstop, 0, 0.35f);
            else Number("strikeInterval", "连击间隔（秒）", ref Move.strikeInterval, 0.06f, 0.5f);
            if (Move.chargeIntensity > 0)
            {
                Number("chargeReady", "出招预警时刻（进度）", ref Move.chargeReady, 0.01f, Mathf.Max(0.01f, Move.contact - 0.01f));
                Widgets.Label(Next(), "预警时刻：" + (Move.chargeReady * Move.duration).ToString("F2") + " 秒");
                Widgets.Label(Next(), "距首次命中：" + ((Move.contact - Move.chargeReady) * Move.duration).ToString("F2") + " 秒");
            }
            for (int i = 0; i < Move.strikeCount; i++)
                Widgets.Label(Next(), "第 " + (i + 1) + " 次接触：" + (Move.ContactAt(i) * Move.duration).ToString("F2") + " 秒");
        }

        //各方向的握点与待机角度分别输入，源贴图上的刀柄与刃段全方向共用。
        private void DrawHold()
        {
            bool mirror = Draft.mirrorBlade;
            Widgets.CheckboxLabeled(Next(), "镜像刃口", ref Draft.mirrorBlade);
            if (mirror != Draft.mirrorBlade) Changed();
            Vector2 hand = Draft.Hand(editor.Preview.Facing);
            Pair("hand", "握点左右", "握点上下", ref hand, -1, 1);
            if (editor.Preview.Facing == Rot4.South) Draft.handSouth = hand;
            else if (editor.Preview.Facing == Rot4.East) Draft.handEast = hand;
            else if (editor.Preview.Facing == Rot4.West) Draft.handWest = hand;
            else Draft.handNorth = hand;
            float angle = Draft.HoldAngle(editor.Preview.Facing);
            Number("holdAngle", "持刀角度校准（°）", ref angle, -180, 180);
            if (editor.Preview.Facing == Rot4.South) Draft.holdAngleSouth = angle;
            else if (editor.Preview.Facing == Rot4.East) Draft.holdAngleEast = angle;
            else if (editor.Preview.Facing == Rot4.West) Draft.holdAngleWest = angle;
            else Draft.holdAngleNorth = angle;
            Pair("grip", "刀柄 UV 横坐标", "刀柄 UV 纵坐标", ref Draft.grip, 0, 1);
            Pair("bladeStart", "刃根 UV 横坐标", "刃根 UV 纵坐标", ref Draft.bladeStart, 0, 1);
            Pair("bladeEnd", "刃尖 UV 横坐标", "刃尖 UV 纵坐标", ref Draft.bladeEnd, 0, 1);
            Pair("bodyPivot", "身体转轴左右", "身体转轴上下", ref Draft.bodyPivot, -1, 1);
        }

        //中间帧可调整时间和姿态，首尾固定归零，避免接回待机时跳动。
        private void DrawPose()
        {
            MeleeAnimationFacing face = Move.For(editor.Preview.Facing);
            Pair("offset", "整招左右修正", "整招上下修正", ref face.offset, -0.5f, 0.5f);
            Number("angle", "整招角度修正", ref face.angle, -90, 90);
            Number("layer", "武器与刀光层级", ref face.layer, -10, 100);
            keyIndex = Mathf.Clamp(keyIndex, 0, face.keys.Count - 1);
            if (Widgets.ButtonText(Next(), "关键帧 " + (keyIndex + 1) + " / " + face.keys.Count))
                Find.WindowStack.Add(new FloatMenu(face.keys.Select((k, i) => new FloatMenuOption(
                    (i + 1) + " · " + k.time.ToString("F2"), () => { keyIndex = i; buffers.Clear(); })).ToList()));
            MeleeAnimationKey key = face.keys[keyIndex];
            if (keyIndex == 0 || keyIndex == face.keys.Count - 1) Widgets.Label(Next(), "首尾帧回到配置的持刀姿态。" );
            else
            {
                Number("key.time", "归一化时间", ref key.time, face.keys[keyIndex - 1].time + 0.001f, face.keys[keyIndex + 1].time - 0.001f);
                Number("key.angle", "绕刀柄旋转（°）", ref key.angle, -270, 270);
                Pair("key.position", "挥刀左右位移", "挥刀上下位移", ref key.position, -0.6f, 0.6f);
                Pair("key.body", "身体左右位移", "身体上下位移", ref key.body, -0.2f, 0.2f);
                Number("key.lean", "身体倾斜（°）", ref key.lean, -12, 12);
                Number("key.trail", "刀光强度包络", ref key.trail, 0, 1);
            }
            if (Widgets.ButtonText(Next(), "预览此关键帧"))
            { Start(); editor.Owner.Paused = true; Play.Seek(key.time * Play.Duration + (key.time > Move.contact ? Play.Stop : 0), editor.Owner.Clock); }
        }

        //刀光边缘位移、辉光和火屑各自可调，不改变身体已有火焰。
        private void DrawEffects()
        {
            Number("chargeIntensity", "十字预警亮度（0 关闭）", ref Move.chargeIntensity, 0, 2);
            if (Move.chargeIntensity > 0)
            {
                Number("chargeFlash", "十字闪光亮芯强度", ref Move.chargeFlash, 0, 3);
                Number("chargeScale", "十字闪光大小", ref Move.chargeScale, 0.5f, 2);
            }
            Number("trailLifetime", "刀光残留（秒）", ref Move.trailLifetime, 0.04f, 0.6f);
            Number("trailWidth", "沿刀刃覆盖比例", ref Move.trailWidth, 0.1f, 1.5f);
            Number("trailIntensity", "刀光亮度", ref Move.trailIntensity, 0, 3);
            Number("flameDistortion", "紫焰置换扭动", ref Move.flameDistortion, 0, 2);
            Number("effectSpeed", "紫焰与火屑波动速度", ref Move.effectSpeed, 0, 3);
            Number("bloom", "刀光辉光", ref Move.bloom, 0, 2);
            Number("color.r", "红", ref Move.color.r, 0, 1); Number("color.g", "绿", ref Move.color.g, 0, 1);
            Number("color.b", "蓝", ref Move.color.b, 0, 1); Number("color.a", "透明度", ref Move.color.a, 0, 1);
            float count = Move.sparks;
            if (Number("sparks", "火屑数量", ref count, 0, 48)) Move.sparks = Mathf.RoundToInt(count);
            Number("sparkLifetime", "火屑存活（秒）", ref Move.sparkLifetime, 0.1f, 1.5f);
            Number("sparkSpeed", "火屑飘散速度", ref Move.sparkSpeed, 0, 1.5f);
            Number("sparkSize", "火屑大小", ref Move.sparkSize, 0.003f, 0.05f);
        }

        //参数改变立即更新预览时长，保留当前播放位置。
        private void Changed()
        {
            Dirty = true;
            if (Play.Move != null) { Play.Duration = Move.duration; Play.Stop = Move.hitstop; }
            editor.Preview.Invalidate();
        }

        //折叠分组与数值字段采用动态中文行高。
        private void Group(string label, ref bool open)
        { y += 6; if (Widgets.ButtonText(Next(), (open ? "▼ " : "▶ ") + label)) open = !open; }
        //分配滚动内容中的下一行。
        private Rect Next() { var r = new Rect(0, y, width, Row); y += Row + 5; return r; }
        //二维坐标使用相同的范围与输入方式。
        private void Pair(string id, string x, string z, ref Vector2 v, float min, float max)
        { Number(id + ".x", x, ref v.x, min, max); Number(id + ".y", z, ref v.y, min, max); }
        //数值输入与滑条分行，非法输入保留红框并阻止应用。
        private bool Number(string id, string label, ref float value, float min, float max)
        {
            float before = value;
            float rowHeight = Mathf.Max(Row, Text.CalcHeight(label, width - 92) + 8);
            Rect rect = new Rect(0, y, width, rowHeight); y += rowHeight + 5;
            Widgets.Label(new Rect(0, rect.y, width - 92, rowHeight), label);
            string control = "MeleeAnimation_" + id;
            if (!buffers.ContainsKey(id) || GUI.GetNameOfFocusedControl() != control) buffers[id] = value.ToString("R", CultureInfo.InvariantCulture);
            GUI.SetNextControlName(control);
            Rect field = new Rect(width - 88, rect.y, 88, Row);
            buffers[id] = Widgets.TextField(field, buffers[id]);
            if (float.TryParse(buffers[id], NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                && !float.IsNaN(parsed) && !float.IsInfinity(parsed) && parsed >= min && parsed <= max) value = parsed;
            else
            {
                Color color = GUI.color; GUI.color = Color.red; Widgets.DrawBox(field); GUI.color = color;
                TooltipHandler.TipRegion(field, "范围 " + min + "～" + max + "；红框内容尚未应用。");
            }
            float slider = Widgets.HorizontalSlider(new Rect(0, y, width, 22), value, min, max); y += 27;
            if (slider != value) { value = slider; buffers[id] = value.ToString("R", CultureInfo.InvariantCulture); }
            if (value == before) return false;
            Changed(); return true;
        }
    }
}
