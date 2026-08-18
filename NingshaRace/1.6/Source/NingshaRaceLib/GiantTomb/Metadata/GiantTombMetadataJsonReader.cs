using System;
using System.Collections.Generic;
using System.IO;

namespace NingshaRaceLib.GiantTomb.Metadata
{
    //类职责：把本地JSON解析树严格映射为巨型墓葬metadata数据模型。
    internal static class GiantTombMetadataJsonReader
    {
        //函数职责：读取完整metadata文本并映射运行时需要的全部字段。
        public static GiantTombTemplateMetadata Read(string json)
        {
            Dictionary<string, object> root = RequireObject(GiantTombJsonParser.Parse(json), "根对象");
            Dictionary<string, object> size = RequireObject(Require(root, "templateSize"), "templateSize");
            GiantTombTemplateMetadata result = new GiantTombTemplateMetadata
            {
                Schema = RequireString(root, "schema"),
                Version = RequireInt(root, "version"),
                Name = RequireString(root, "name"),
                SourceFile = RequireString(root, "sourceFile"),
                BinaryBytes = RequireLong(root, "binaryBytes"),
                TemplateSize = new GiantTombTemplateSize
                {
                    Width = RequireInt(size, "width"),
                    Height = RequireInt(size, "height"),
                    CellCount = RequireInt(size, "cellCount")
                },
                WallDoorGrid = ReadStringArray(root, "wallDoorGrid"),
                OccupancyGrid = ReadStringArray(root, "occupancyGrid"),
                WalkabilityGrid = ReadStringArray(root, "walkabilityGrid"),
                InferredConnectors = ReadConnectors(root)
            };
            return result;
        }

        //函数职责：读取并映射metadata中的连接点对象数组。
        private static List<GiantTombConnectorMetadata> ReadConnectors(Dictionary<string, object> root)
        {
            List<object> values = RequireArray(Require(root, "inferredConnectors"), "inferredConnectors");
            List<GiantTombConnectorMetadata> result = new List<GiantTombConnectorMetadata>();
            for (int i = 0; i < values.Count; i++)
            {
                Dictionary<string, object> source = RequireObject(values[i], "inferredConnectors[" + i + "]");
                result.Add(new GiantTombConnectorMetadata
                {
                    Type = RequireString(source, "type"),
                    Direction = RequireString(source, "direction"),
                    X = RequireInt(source, "x"),
                    Z = RequireInt(source, "z"),
                    Width = RequireInt(source, "width"),
                    StartX = RequireInt(source, "startX"),
                    StartZ = RequireInt(source, "startZ"),
                    EndX = RequireInt(source, "endX"),
                    EndZ = RequireInt(source, "endZ")
                });
            }
            return result;
        }

        //函数职责：读取一个只允许包含字符串的JSON数组。
        private static List<string> ReadStringArray(Dictionary<string, object> source, string name)
        {
            List<object> values = RequireArray(Require(source, name), name);
            List<string> result = new List<string>();
            for (int i = 0; i < values.Count; i++)
            {
                if (!(values[i] is string value)) throw new InvalidDataException(name + "[" + i + "]必须是字符串");
                result.Add(value);
            }
            return result;
        }

        //函数职责：取得必需对象成员并在缺失时直接报错。
        private static object Require(Dictionary<string, object> source, string name)
        {
            if (!source.TryGetValue(name, out object value)) throw new InvalidDataException("metadata缺少字段: " + name);
            return value;
        }

        //函数职责：取得必需字符串成员并检查类型。
        private static string RequireString(Dictionary<string, object> source, string name)
        {
            object value = Require(source, name);
            if (!(value is string text)) throw new InvalidDataException("metadata字段必须是字符串: " + name);
            return text;
        }

        //函数职责：取得必需32位整数成员并检查范围。
        private static int RequireInt(Dictionary<string, object> source, string name)
        {
            long value = RequireLong(source, name);
            if (value < int.MinValue || value > int.MaxValue) throw new InvalidDataException("metadata整数越界: " + name);
            return (int)value;
        }

        //函数职责：取得必需64位整数成员并拒绝小数编码。
        private static long RequireLong(Dictionary<string, object> source, string name)
        {
            object value = Require(source, name);
            if (!(value is long integer)) throw new InvalidDataException("metadata字段必须是整数: " + name);
            return integer;
        }

        //函数职责：把解析值检查并转换为JSON对象。
        private static Dictionary<string, object> RequireObject(object value, string name)
        {
            if (!(value is Dictionary<string, object> result)) throw new InvalidDataException("metadata字段必须是对象: " + name);
            return result;
        }

        //函数职责：把解析值检查并转换为JSON数组。
        private static List<object> RequireArray(object value, string name)
        {
            if (!(value is List<object> result)) throw new InvalidDataException("metadata字段必须是数组: " + name);
            return result;
        }
    }
}
