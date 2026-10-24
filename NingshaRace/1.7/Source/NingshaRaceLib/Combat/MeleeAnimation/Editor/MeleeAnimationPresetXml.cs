using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using UnityEngine;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //只序列化近战定义的公开参数，导出文件可直接替换 Mod 内同名 Def。
    internal static class MeleeAnimationPresetXml
    {
        private const string Element = "NingshaRaceLib.Combat.MeleeAnimation.MeleeAnimationDef";
        private const BindingFlags Fields = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        internal static string DirectoryPath => Path.Combine(Verse.GenFilePaths.SaveDataFolderPath, "NingshaRace", "MeleeAnimations");

        //完整复制握点与四向曲线，不让预览影响共享 Def。
        internal static MeleeAnimationDef Copy(MeleeAnimationDef def)
        {
            return new MeleeAnimationDef
            {
                defName = def.defName, label = def.label, grip = def.grip, bladeStart = def.bladeStart, bladeEnd = def.bladeEnd, mirrorBlade = def.mirrorBlade,
                handSouth = def.handSouth, handEast = def.handEast, handWest = def.handWest, handNorth = def.handNorth, bodyPivot = def.bodyPivot,
                holdAngleSouth = def.holdAngleSouth, holdAngleEast = def.holdAngleEast, holdAngleWest = def.holdAngleWest, holdAngleNorth = def.holdAngleNorth,
                moves = def.moves.Select(m => m.Copy()).ToList()
            };
        }

        //先检查完整定义，再创建文件，失败时不覆盖已有文件。
        internal static string Export(string name, MeleeAnimationDef definition)
        {
            RequireValid(definition);
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name == "." || name == "..")
                throw new InvalidOperationException("文件名不能为空，且不能包含路径或非法字符。");
            Directory.CreateDirectory(DirectoryPath);
            string path = Path.Combine(DirectoryPath, name + ".xml");
            var document = new XmlDocument();
            var root = document.CreateElement("Defs"); document.AppendChild(root);
            var def = Add(root, Element, null);
            Add(def, "defName", definition.defName); Add(def, "label", definition.label);
            Write(def, definition);
            using (var writer = XmlWriter.Create(path, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true })) document.Save(writer);
            return path;
        }

        //在独立对象上完成解析与校验，失败时保持当前草稿。
        internal static MeleeAnimationDef Import(string path, string defName)
        {
            var document = new XmlDocument { XmlResolver = null };
            using (var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null })) document.Load(reader);
            XmlElement root = document.DocumentElement;
            var defs = root?.ChildNodes.OfType<XmlElement>().ToList();
            if (root?.Name != "Defs" || defs.Count != 1 || defs[0].Name != Element)
                throw new InvalidOperationException("文件必须包含一个 MeleeAnimationDef 近战定义。");
            var node = defs[0];
            if (node["defName"]?.InnerText != defName) throw new InvalidOperationException("近战 defName 不匹配，预期：" + defName);
            var result = new MeleeAnimationDef { defName = defName, label = node["label"]?.InnerText };
            Read(node, result, "近战"); RequireValid(result); return result;
        }

        //字段列表只来自四种已知参数类型，不从 XML 创建任意类。
        private static void Write(XmlElement parent, object value)
        {
            foreach (FieldInfo field in value.GetType().GetFields(Fields))
            {
                object data = field.GetValue(value);
                XmlElement node = Add(parent, field.Name, null);
                if (data is IList list) foreach (object item in list) Write(Add(node, "li", null), item);
                else node.InnerText = Format(data);
            }
        }

        //错误路径包含动作编号、朝向编号和字段，未知、缺失或重复字段均报错。
        private static void Read(XmlElement parent, object value, string path)
        {
            FieldInfo[] fields = value.GetType().GetFields(Fields);
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (XmlElement node in parent.ChildNodes.OfType<XmlElement>())
            {
                if (value is MeleeAnimationDef && (node.Name == "defName" || node.Name == "label")) continue;
                FieldInfo field = fields.FirstOrDefault(f => f.Name == node.Name);
                if (field == null || !seen.Add(node.Name)) throw new InvalidOperationException(path + "：字段未知或重复：" + node.Name);
                string current = path + " / " + node.Name;
                try
                {
                    if (typeof(IList).IsAssignableFrom(field.FieldType))
                    {
                        var list = (IList)Activator.CreateInstance(field.FieldType);
                        Type itemType = field.FieldType.GetGenericArguments()[0];
                        foreach (XmlElement entry in node.ChildNodes.OfType<XmlElement>())
                        {
                            if (entry.Name != "li") throw new InvalidOperationException(current + "：列表中只能包含 li。");
                            object item = Activator.CreateInstance(itemType);
                            Read(entry, item, current + "[" + (list.Count + 1) + "]"); list.Add(item);
                        }
                        field.SetValue(value, list);
                    }
                    else field.SetValue(value, Parse(node.InnerText, field.FieldType));
                }
                catch (Exception e) when (e is FormatException || e is OverflowException || e is ArgumentException)
                { throw new InvalidOperationException(current + "：" + e.Message, e); }
            }
            foreach (FieldInfo field in fields)
                if (!seen.Contains(field.Name)) throw new InvalidOperationException(path + "：缺少字段 " + field.Name);
        }

        //导入和导出与游戏启动时使用相同检查。
        private static void RequireValid(MeleeAnimationDef def)
        {
            string error = string.Join("\n", def.ConfigErrors());
            if (error.Length > 0) throw new InvalidOperationException(error);
        }

        //使用游戏 XML 的向量和颜色格式，不依赖具体单位的外观编辑器。
        private static string Format(object value)
        {
            if (value is float f) return f.ToString("R", CultureInfo.InvariantCulture);
            if (value is int n) return n.ToString(CultureInfo.InvariantCulture);
            if (value is bool b) return b ? "true" : "false";
            if (value is Vector2 v) return "(" + Format(v.x) + ", " + Format(v.y) + ")";
            if (value is Color c) return "(" + Format(c.r) + ", " + Format(c.g) + ", " + Format(c.b) + ", " + Format(c.a) + ")";
            return (string)value;
        }

        //只解析框架实际使用的值类型。
        private static object Parse(string text, Type type)
        {
            if (type == typeof(string)) return text;
            if (type == typeof(bool)) return bool.Parse(text);
            if (type == typeof(float)) return float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
            if (type == typeof(int)) return int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
            string[] entries = text.Trim().Trim('(', ')').Split(',');
            if (entries.Length != (type == typeof(Vector2) ? 2 : 4)) throw new FormatException("坐标或颜色分量数量不正确。");
            float[] values = entries.Select(v => float.Parse(v, CultureInfo.InvariantCulture)).ToArray();
            if (type == typeof(Vector2)) return new Vector2(values[0], values[1]);
            return new Color(values[0], values[1], values[2], values[3]);
        }

        //文本由 XML API 转义，保留中文名称。
        private static XmlElement Add(XmlElement parent, string name, string text)
        {
            XmlElement node = parent.OwnerDocument.CreateElement(name); parent.AppendChild(node);
            if (text != null) node.InnerText = text;
            return node;
        }
    }
}
