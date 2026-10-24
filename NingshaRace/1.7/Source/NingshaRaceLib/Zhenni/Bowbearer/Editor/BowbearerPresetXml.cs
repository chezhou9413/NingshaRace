using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using NingshaRaceLib.Zhenni.Bladebearer;
using Verse;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//可直接放回 Defs 的射击预设，不修改已加载的共享定义。
internal static class BowbearerPresetXml
{
    internal static string DirectoryPath => Path.Combine(GenFilePaths.SaveDataFolderPath, "NingshaRace", "BowbearerPresets");
    private const string DefinitionElement = "NingshaRaceLib.Zhenni.Bowbearer.BowbearerShootingDef";

    //导出前检查整个草稿及引用的箭体资源。
    internal static void Validate(BowbearerSettings settings)
    {
        string errors = string.Join("\n", settings.Errors());
        if (errors.Length > 0) throw new InvalidOperationException(errors);
        BladebearerResources.ValidateTextures(settings.arrowHead);
        BladebearerResources.ValidateTextures(settings.arrowTail);
    }

    //全部字段写入同一个文件，便于美工交付和版本管理。
    internal static string Export(string name, BowbearerShootingDef definition, BowbearerSettings settings)
    {
        Validate(settings);
        if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name is "." or "..")
            throw new InvalidOperationException("文件名不能为空或包含路径字符。");
        var root = new XElement("Defs", new XElement(DefinitionElement,
            new XElement("defName", definition.defName), new XElement("label", definition.label), Write("settings", settings)));
        Directory.CreateDirectory(DirectoryPath);
        string path = Path.Combine(DirectoryPath, name + ".xml");
        using var writer = XmlWriter.Create(path, new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) });
        root.Save(writer);
        return path;
    }

    //先在临时对象中完成解析与检查，成功后才替换编辑器草稿。
    internal static BowbearerSettings Import(string path, string expectedDefName)
    {
        using var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        var root = XElement.Load(reader);
        if (root.Name != "Defs" || root.Elements().Count() != 1 || root.Elements().First().Name != DefinitionElement)
            throw new InvalidOperationException("需要只包含一个挽弓者射击定义的 Defs 文件。");
        XElement def = root.Elements().First();
        if ((string)def.Element("defName") != expectedDefName) throw new InvalidOperationException("单位定义不匹配：" + expectedDefName);
        var result = new BowbearerSettings();
        Read(def.Element("settings") ?? throw new InvalidOperationException("缺少 settings。"), result);
        Validate(result);
        return result;
    }

    //只序列化固定的参数类型和现有图层类型。
    private static XElement Write(string name, object value)
    {
        var node = new XElement(name);
        foreach (FieldInfo field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            node.Add(field.FieldType == typeof(BladebearerPart) || field.FieldType == typeof(BowbearerEmpoweredSettings)
                ? Write(field.Name, field.GetValue(value))
                : new XElement(field.Name, BladebearerPresetXml.Format(field.GetValue(value))));
        return node;
    }

    //导出的完整格式不接受未知、重复或缺失字段。
    private static void Read(XElement node, object value)
    {
        var fields = value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (XElement child in node.Elements())
            if (!fields.Any(f => f.Name == child.Name.LocalName)) throw new InvalidOperationException("未知字段：" + child.Name);
        foreach (FieldInfo field in fields)
        {
            var matches = node.Elements(field.Name).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("字段缺失或重复：" + field.Name);
            try
            {
                if (field.FieldType == typeof(BladebearerPart) || field.FieldType == typeof(BowbearerEmpoweredSettings)) Read(matches[0], field.GetValue(value));
                else field.SetValue(value, BladebearerPresetXml.Parse(matches[0].Value, field.FieldType));
            }
            catch (Exception e) when (e is FormatException or OverflowException or ArgumentException)
            { throw new InvalidOperationException("字段「" + field.Name + "」无效：" + e.Message, e); }
        }
    }
}
