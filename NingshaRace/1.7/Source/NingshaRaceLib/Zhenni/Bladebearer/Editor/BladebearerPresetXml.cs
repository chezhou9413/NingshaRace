using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //直接读写可放入 Defs 的外观 XML，不引入独立的中间文件格式。
    internal static class BladebearerPresetXml
    {
        internal static string DirectoryPath => Path.Combine(GenFilePaths.SaveDataFolderPath, "NingshaRace", "BladebearerPresets");
        private const string ElementName = "NingshaRaceLib.Zhenni.Bladebearer.BladebearerAppearanceDef";
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        //所有部件通过检查后才写文件，文件名不接受路径分隔符。
        internal static string Export(string name, BladebearerAppearanceDef definition, List<BladebearerPart> parts)
        {
            RequireValid(parts);
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name == "." || name == "..")
                throw new InvalidOperationException("文件名不能为空，且不能包含路径或非法字符。");
            Directory.CreateDirectory(DirectoryPath);
            string path = Path.Combine(DirectoryPath, name + ".xml");
            var document = new XmlDocument();
            XmlElement defs = document.CreateElement("Defs"); document.AppendChild(defs);
            XmlElement appearance = document.CreateElement(ElementName); defs.AppendChild(appearance);
            Add(appearance, "defName", definition.defName); Add(appearance, "label", definition.label ?? "震尼拥刀者外观");
            XmlElement list = document.CreateElement("parts"); appearance.AppendChild(list);
            foreach (BladebearerPart part in parts)
            {
                XmlElement item = document.CreateElement("li"); list.AppendChild(item);
                foreach (FieldInfo field in BladebearerValidation.Fields) Add(item, field.Name, Format(field.GetValue(part)));
            }
            using (XmlWriter writer = XmlWriter.Create(path, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true }))
                document.Save(writer);
            return path;
        }

        //在临时对象上完整解析、检查字段和资源，成功后再交给编辑器替换草稿。
        internal static List<BladebearerPart> Import(string path, string expectedDefName)
        {
            var document = new XmlDocument { XmlResolver = null };
            using (XmlReader reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
                document.Load(reader);
            XmlNode root = document.DocumentElement;
            var definitions = root?.ChildNodes.Cast<XmlNode>().Where(n => n.NodeType == XmlNodeType.Element).ToList();
            if (root?.Name != "Defs" || definitions.Count != 1 || definitions[0].Name != ElementName)
                throw new InvalidOperationException("文件必须包含一个震尼拥刀者 BladebearerAppearanceDef。");
            XmlNode definition = definitions[0];
            if (definition["defName"]?.InnerText != expectedDefName)
                throw new InvalidOperationException("defName 不匹配，预期：" + expectedDefName);
            XmlNode list = definition["parts"];
            if (list == null) throw new InvalidOperationException("缺少 parts 部件列表。");
            var result = new List<BladebearerPart>();
            foreach (XmlNode item in list.ChildNodes)
            {
                if (item.NodeType != XmlNodeType.Element) continue;
                if (item.Name != "li") throw new InvalidOperationException("parts 只能包含 li 部件。");
                var part = new BladebearerPart(); var seen = new HashSet<string>();
                string name = item["label"]?.InnerText ?? item["id"]?.InnerText ?? (result.Count + 1).ToString();
                foreach (XmlNode fieldNode in item.ChildNodes)
                {
                    if (fieldNode.NodeType != XmlNodeType.Element) continue;
                    //美工已交付的预设含有这些发光字段，导入时舍弃，其他参数照常检查。
                    switch (fieldNode.Name)
                    {
                        case "emissionColor": case "emission": case "emissionThreshold":
                        case "glow": case "glowRadius": case "glowSoftness":
                        case "twinkleFrequency": case "twinkleMin": case "twinkleMax":
                            continue;
                    }
                    FieldInfo field = BladebearerValidation.Fields.FirstOrDefault(f => f.Name == fieldNode.Name);
                    if (field == null || !seen.Add(fieldNode.Name)) throw new InvalidOperationException("部件「" + name + "」字段未知或重复：" + fieldNode.Name);
                    try { field.SetValue(part, Parse(fieldNode.InnerText, field.FieldType)); }
                    catch (Exception e) when (e is FormatException || e is OverflowException || e is ArgumentException)
                    { throw new InvalidOperationException("部件「" + name + "」字段 " + field.Name + "：" + e.Message, e); }
                }
                foreach (FieldInfo field in BladebearerValidation.Fields)
                {
                    //已经交付的预设可省略偏转角和后处理参数，使用对应字段的默认值。
                    if (field.Name == nameof(BladebearerPart.flowAngle) || field.Name == nameof(BladebearerPart.bloomIntensity)
                        || field.Name == nameof(BladebearerPart.bloomThreshold) || field.Name == nameof(BladebearerPart.particle)
                        || field.Name == nameof(BladebearerPart.particleRate) || field.Name == nameof(BladebearerPart.particleLifetime)
                        || field.Name == nameof(BladebearerPart.particleSpeed) || field.Name == nameof(BladebearerPart.particleSize)
                        || field.Name == nameof(BladebearerPart.particleSpread) || field.Name == nameof(BladebearerPart.particleRadius)
                        || field.Name == nameof(BladebearerPart.particleSway) || field.Name == nameof(BladebearerPart.particleFlame)
                        || field.Name == nameof(BladebearerPart.lightIntensity)
                        || field.Name == nameof(BladebearerPart.lightFlickerStrength)
                        || field.Name == nameof(BladebearerPart.lightFlickerFrequency)) continue;
                    if (!seen.Contains(field.Name)) throw new InvalidOperationException("部件「" + name + "」缺少字段：" + field.Name);
                }
                //已交付的匕首粒子贴图始终独立于火焰，导入美工现有预设时也保持分离。
                if (!seen.Contains(nameof(BladebearerPart.particle)) && part.texture == "Zhenni/Bladebearer/Weapon/Bladebearer_DaggerSparks")
                { part.particle = true; part.flame = false; }
                result.Add(part);
            }
            RequireValid(result);
            return result;
        }

        //导入和导出采用同一套范围与资源检查。
        private static void RequireValid(List<BladebearerPart> parts)
        {
            string errors = string.Join("\n", BladebearerValidation.Errors(parts));
            if (errors.Length > 0) throw new InvalidOperationException(errors);
            foreach (BladebearerPart part in parts) BladebearerResources.ValidateTextures(part);
        }

        //使用与 RimWorld Def 解析相同的括号向量和颜色格式。
        internal static string Format(object value)
        {
            if (value is float f) return f.ToString("R", Culture);
            if (value is int integer) return integer.ToString(Culture);
            if (value is bool b) return b ? "true" : "false";
            if (value is Vector2 v) return "(" + Format(v.x) + ", " + Format(v.y) + ")";
            if (value is Color c) return "(" + Format(c.r) + ", " + Format(c.g) + ", " + Format(c.b) + ", " + Format(c.a) + ")";
            return (string)value;
        }

        //仅解析本部件结构使用的四类值，不接受可执行类型或任意 Def。
        internal static object Parse(string text, Type type)
        {
            if (type == typeof(string)) return text;
            if (type == typeof(bool)) return bool.Parse(text);
            if (type == typeof(float)) return float.Parse(text, NumberStyles.Float, Culture);
            if (type == typeof(int)) return int.Parse(text, NumberStyles.Integer, Culture);
            string[] entries = text.Trim().Trim('(', ')').Split(',');
            int length = type == typeof(Vector2) ? 2 : 4;
            if (entries.Length != length) throw new FormatException("需要 " + length + " 个逗号分隔的数字。");
            float[] values = entries.Select(t => float.Parse(t, NumberStyles.Float, Culture)).ToArray();
            if (type == typeof(Vector2)) return new Vector2(values[0], values[1]);
            return new Color(values[0], values[1], values[2], values[3]);
        }

        //通过 XML API 转义中文标签和用户输入。
        private static void Add(XmlElement parent, string name, string text)
        {
            XmlElement element = parent.OwnerDocument.CreateElement(name); element.InnerText = text;
            parent.AppendChild(element);
        }
    }
}
