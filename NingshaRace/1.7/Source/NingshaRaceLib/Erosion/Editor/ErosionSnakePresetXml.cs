using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using NingshaRaceLib.Erosion.Rendering;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Erosion.Editor
{
    //导出可直接放入模组 Patches 的 XML，也可读回本工具生成的补丁。
    internal static class ErosionSnakePresetXml
    {
        internal static string DirectoryPath => Path.Combine(GenFilePaths.SaveDataFolderPath, "NingshaRace", "ErosionSnakePresets");
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
        private static readonly string[] DirectionNodes = { "dataNorth", "dataEast", "dataSouth", "dataWest" };

        //只替换蛇头的摆放、显隐和动画数据，不重复定义整个侵蚀体。
        internal static string Export(ErosionSnakeEditorSession session)
        {
            var doc = new XmlDocument();
            XmlElement root = Add(doc, "Patch");
            XmlElement operation = Add(root, "Operation"); operation.SetAttribute("Class", "PatchOperationSequence");
            XmlElement operations = Add(operation, "operations");
            foreach (ErosionSnakeDraft snake in session.Snakes)
            {
                string xpath = Target(session, snake);
                XmlElement remove = Add(operations, "li"); remove.SetAttribute("Class", "PatchOperationConditional");
                string fields = xpath + "/*[self::drawData or self::visibleFacing or self::swayByFacing]";
                Add(remove, "xpath", fields);
                XmlElement match = Add(remove, "match"); match.SetAttribute("Class", "PatchOperationRemove");
                Add(match, "xpath", fields);
                XmlElement append = Add(operations, "li"); append.SetAttribute("Class", "PatchOperationAdd");
                Add(append, "xpath", xpath);
                XmlElement value = Add(append, "value");
                XmlElement draw = Add(value, "drawData");
                Add(draw, "scale", Number(snake.Props.drawData.scale));
                Add(draw, "childScale", Number(snake.Props.drawData.childScale));
                Add(draw, "scaleOffsetByBodySize", snake.Props.drawData.scaleOffsetByBodySize ? "true" : "false");
                Add(draw, "useBodyPartAnchor", snake.Props.drawData.useBodyPartAnchor ? "true" : "false");
                XmlElement visible = Add(value, "visibleFacing");
                XmlElement sways = Add(value, "swayByFacing");
                for (int i = 0; i < 4; i++)
                {
                    DrawData.RotationalData pose = snake.Poses[i];
                    XmlElement direction = Add(draw, DirectionNodes[i]);
                    Vector3 offset = pose.offset.Value;
                    Vector2 pivot = pose.pivot.Value;
                    Add(direction, "offset", "(" + Number(offset.x) + ", " + Number(offset.y) + ", " + Number(offset.z) + ")");
                    Add(direction, "pivot", "(" + Number(pivot.x) + ", " + Number(pivot.y) + ")");
                    Add(direction, "rotationOffset", Number(pose.rotationOffset.Value));
                    Add(direction, "layer", Number(pose.layer.Value));
                    Add(direction, "flip", pose.flip.Value ? "true" : "false");
                    string facing = new Rot4(i).ToStringWord();
                    if (snake.Visible[i]) Add(visible, "li", facing);
                    ErosionSnakeSway sway = snake.Props.swayByFacing[i];
                    XmlElement animation = Add(sways, "li");
                    Add(animation, "facing", facing); Add(animation, "angle", Number(sway.angle));
                    Add(animation, "period", Number(sway.period)); Add(animation, "phase", Number(sway.phase));
                }
            }
            Directory.CreateDirectory(DirectoryPath);
            string path = Path.Combine(DirectoryPath, "侵蚀体蛇头_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".xml");
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            using (XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) }))
                doc.Save(writer);
            return path;
        }

        //先完整解析到新草稿，任何错误都不会覆盖当前调整。
        internal static void Import(string path, ErosionSnakeEditorSession session)
        {
            var doc = new XmlDocument { XmlResolver = null };
            using (XmlReader reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
                doc.Load(reader);
            XmlNodeList additions = doc.SelectNodes("/Patch/Operation[@Class='PatchOperationSequence']/operations/li[@Class='PatchOperationAdd']");
            if (additions.Count != session.Snakes.Count) throw new InvalidOperationException("请选择本工具导出的完整蛇头 XML 补丁。");
            var imported = session.Snakes.Select(snake => new ErosionSnakeDraft(snake.Source)).ToList();
            foreach (ErosionSnakeDraft snake in imported)
            {
                XmlNode[] matches = additions.Cast<XmlNode>().Where(node => node["xpath"]?.InnerText == Target(session, snake)).ToArray();
                if (matches.Length != 1) throw new InvalidOperationException("蛇头节点缺失或重复：" + snake.Source.debugLabel);
                XmlNode value = matches[0]["value"];
                if (value?["drawData"] == null || value["visibleFacing"] == null || value["swayByFacing"] == null)
                    throw new InvalidOperationException("缺少摆放、显隐或动画数据。");
                for (int i = 0; i < 4; i++)
                {
                    XmlNode pose = value["drawData"][DirectionNodes[i]];
                    float[] offset = Vector(Read(pose, "offset"), 3), pivot = Vector(Read(pose, "pivot"), 2);
                    if (offset.Any(n => Mathf.Abs(n) > 2) || pivot.Any(n => n < 0 || n > 1))
                        throw new InvalidOperationException("位置必须在 -2～2 之间，连接点必须在 0～1 之间。");
                    Rot4 facing = new Rot4(i);
                    snake.Poses[i] = new DrawData.RotationalData(facing, Numeric(pose, "layer",
                        PawnRenderNodeProperties_ErosionSnake.MinimumLayer, PawnRenderNodeProperties_ErosionSnake.MaximumLayer))
                    {
                        offset = new Vector3(offset[0], offset[1], offset[2]), pivot = new Vector2(pivot[0], pivot[1]),
                        rotationOffset = Numeric(pose, "rotationOffset", -180, 180), flip = bool.Parse(Read(pose, "flip"))
                    };
                    snake.Visible[i] = value["visibleFacing"].SelectNodes("li").Cast<XmlNode>().Any(node => node.InnerText == facing.ToStringWord());
                    XmlNode[] animations = value["swayByFacing"].SelectNodes("li").Cast<XmlNode>()
                        .Where(node => node["facing"]?.InnerText == facing.ToStringWord()).ToArray();
                    if (animations.Length != 1) throw new InvalidOperationException("动画朝向缺失或重复：" + facing.ToStringWord());
                    snake.Props.swayByFacing[i] = new ErosionSnakeSway
                    {
                        facing = facing, angle = Numeric(animations[0], "angle", 0, 30),
                        period = Numeric(animations[0], "period", 0.1f, 30), phase = Numeric(animations[0], "phase", -100, 100)
                    };
                }
                snake.Apply();
            }
            session.Snakes.Clear(); session.Snakes.AddRange(imported);
            session.Pawn.Drawer.renderer.SetAllGraphicsDirty();
        }

        //按贴图路径锁定具体蛇头，避免依赖节点排列顺序。
        private static string Target(ErosionSnakeEditorSession session, ErosionSnakeDraft snake)
            => "Defs/MutantDef[defName='" + session.Pawn.mutant.Def.defName + "']/renderNodeProperties/li[texPath='" + snake.Source.texPath + "']";

        //创建 XML 节点并交给 XML 库转义文本。
        private static XmlElement Add(XmlNode parent, string name, string text = null)
        {
            XmlDocument doc = parent as XmlDocument ?? parent.OwnerDocument;
            XmlElement node = doc.CreateElement(name); parent.AppendChild(node);
            if (text != null) node.InnerText = text;
            return node;
        }

        //统一浮点格式，不受系统语言影响。
        private static string Number(float value) => value.ToString("R", Culture);
        //缺失字段应明确报错。
        private static string Read(XmlNode node, string field)
            => node?[field]?.InnerText ?? throw new InvalidOperationException("参数文件缺少字段：" + field);
        //向量使用 RimWorld XML 的括号格式。
        private static float[] Vector(string text, int count)
        {
            float[] values = text.Trim().Trim('(', ')').Split(',').Select(Parse).ToArray();
            if (values.Length != count) throw new InvalidOperationException("坐标分量数量不正确。");
            return values;
        }
        //数值必须有限且位于控件支持的范围。
        private static float Numeric(XmlNode node, string field, float min, float max)
        {
            float value = Parse(Read(node, field));
            if (value < min || value > max) throw new InvalidOperationException(field + " 超出允许范围。");
            return value;
        }
        //拒绝会破坏渲染矩阵的 NaN 和无穷值。
        private static float Parse(string text)
        {
            float value = float.Parse(text, NumberStyles.Float, Culture);
            if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidOperationException("参数必须为有限数字。");
            return value;
        }
    }
}
