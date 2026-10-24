using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChezhouLib.CustomMission.MapTemplates;
using NingshaRaceLib.GiantTomb.Layout;
using Verse;

namespace NingshaRaceLib.GiantTomb.Metadata
{
    //类职责：严格读取地图模板metadata v2并构造巨型墓葬需要的连接点与结构掩码。
    internal static class GiantTombMetadataLoader
    {
        private const string ExpectedSchema = "ChezhouLib.ClMapTemplateMetadata";
        private const int ExpectedVersion = 2;

        //函数职责：加载一个模板及其同名metadata，并完成全部运行前校验。
        public static GiantTombModule Load(ClMapTemplateDef def)
        {
            return Load(def, out _);
        }

        //函数职责：加载一个模板并同时报告是否直接复用了当前进程中的严格校验结果。
        public static GiantTombModule Load(ClMapTemplateDef def, out bool cacheHit)
        {
            if (def == null)
            {
                throw new ArgumentNullException(nameof(def));
            }
            if (GiantTombTemplateCache.TryGet(def, out GiantTombModule cached))
            {
                cacheHit = true;
                return cached;
            }

            ClCompiledMapTemplate template = ClMapTemplateLoader.Load(def);
            string binaryPath = ResolveDataPath(def);
            string metadataPath = binaryPath.Substring(0, binaryPath.Length - ".clmap".Length) + ".clmeta.json";
            if (!File.Exists(metadataPath))
            {
                throw new FileNotFoundException("巨型墓葬模板缺少metadata: " + def.defName, metadataPath);
            }

            GiantTombTemplateMetadata metadata = GiantTombMetadataJsonReader.Read(File.ReadAllText(metadataPath, Encoding.UTF8));
            ValidateHeader(def, template, binaryPath, metadata);
            ValidateGrid(metadata.WallDoorGrid, template.Width, template.Height, "wallDoorGrid", "012");
            ValidateGrid(metadata.OccupancyGrid, template.Width, template.Height, "occupancyGrid", "01");
            ValidateGrid(metadata.WalkabilityGrid, template.Width, template.Height, "walkabilityGrid", "01234");

            GiantTombModule module = new GiantTombModule
            {
                Def = def,
                Template = template,
                MetadataPath = metadataPath
            };
            module.Connectors.AddRange(ReadConnectors(metadata, template.Width, template.Height, def.defName));
            module.StructureMask = BuildStructureMask(metadata, module.Connectors, template.Width, template.Height);
            ValidateOccupancyInsideMask(metadata, module.StructureMask, template.Width, template.Height, def.defName);
            GiantTombTemplateCache.Add(def, module);
            cacheHit = false;
            return module;
        }

        //函数职责：把模板相对路径限制在其声明模组目录内并返回二进制文件绝对路径。
        private static string ResolveDataPath(ClMapTemplateDef def)
        {
            if (def.modContentPack == null || def.modContentPack.RootDir.NullOrEmpty())
            {
                throw new InvalidDataException("巨型墓葬模板缺少来源模组: " + def.defName);
            }
            string root = Path.GetFullPath(def.modContentPack.RootDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string path = Path.GetFullPath(Path.Combine(root, def.dataPath.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("巨型墓葬模板路径越出来源模组目录: " + def.dataPath);
            }
            return path;
        }

        //函数职责：校验metadata身份、版本、来源文件、二进制大小和模板尺寸。
        private static void ValidateHeader(ClMapTemplateDef def, ClCompiledMapTemplate template, string binaryPath, GiantTombTemplateMetadata metadata)
        {
            if (metadata == null || metadata.Schema != ExpectedSchema || metadata.Version != ExpectedVersion)
            {
                throw new InvalidDataException("巨型墓葬metadata版本无效: " + def.defName);
            }
            if (!string.Equals(metadata.SourceFile, Path.GetFileName(binaryPath), StringComparison.Ordinal))
            {
                throw new InvalidDataException("巨型墓葬metadata的sourceFile不匹配: " + def.defName);
            }
            if (!File.Exists(binaryPath) || new FileInfo(binaryPath).Length != metadata.BinaryBytes)
            {
                throw new InvalidDataException("巨型墓葬metadata记录的二进制大小不匹配: " + def.defName);
            }
            if (metadata.TemplateSize == null || metadata.TemplateSize.Width != template.Width || metadata.TemplateSize.Height != template.Height || metadata.TemplateSize.CellCount != template.Width * template.Height)
            {
                throw new InvalidDataException("巨型墓葬metadata记录的模板尺寸不匹配: " + def.defName);
            }
        }

        //函数职责：校验二维字符网格的行数、列数和允许编码。
        private static void ValidateGrid(List<string> rows, int width, int height, string name, string allowed)
        {
            if (rows == null || rows.Count != height)
            {
                throw new InvalidDataException(name + "行数不匹配");
            }
            for (int row = 0; row < rows.Count; row++)
            {
                string value = rows[row];
                if (value == null || value.Length != width)
                {
                    throw new InvalidDataException(name + "列数不匹配: " + row);
                }
                for (int x = 0; x < value.Length; x++)
                {
                    if (allowed.IndexOf(value[x]) < 0)
                    {
                        throw new InvalidDataException(name + "包含未知编码: " + value[x]);
                    }
                }
            }
        }

        //函数职责：解析并校验全部连接点位于声明方向对应的模板边界。
        private static IEnumerable<GiantTombConnector> ReadConnectors(GiantTombTemplateMetadata metadata, int width, int height, string defName)
        {
            if (metadata.InferredConnectors == null || metadata.InferredConnectors.Count == 0)
            {
                throw new InvalidDataException("巨型墓葬模板没有连接点: " + defName);
            }
            for (int i = 0; i < metadata.InferredConnectors.Count; i++)
            {
                GiantTombConnectorMetadata source = metadata.InferredConnectors[i];
                GiantTombConnector connector = new GiantTombConnector
                {
                    Kind = ParseKind(source.Type, defName),
                    Direction = ParseDirection(source.Direction, defName)
                };
                int deltaX = Math.Sign(source.EndX - source.StartX);
                int deltaZ = Math.Sign(source.EndZ - source.StartZ);
                int distance = Math.Abs(source.EndX - source.StartX) + Math.Abs(source.EndZ - source.StartZ);
                if (source.Width <= 0 || distance + 1 != source.Width || deltaX != 0 && deltaZ != 0)
                {
                    throw new InvalidDataException("巨型墓葬连接点跨度无效: " + defName + "/" + i);
                }
                for (int cellIndex = 0; cellIndex < source.Width; cellIndex++)
                {
                    IntVec3 cell = new IntVec3(source.StartX + deltaX * cellIndex, 0, source.StartZ + deltaZ * cellIndex);
                    ValidateBoundaryCell(cell, connector.Direction, width, height, defName, i);
                    connector.Cells.Add(cell);
                }
                connector.AlignmentCells.AddRange(BuildAlignmentCells(metadata, connector, width, height));
                yield return connector;
            }
        }

        //函数职责：把有效连接点扩展到相邻的物理人工开口，用真实开口中心对齐连续走廊并保留原有效宽度兼容规则。
        private static IEnumerable<IntVec3> BuildAlignmentCells(GiantTombTemplateMetadata metadata, GiantTombConnector connector, int width, int height)
        {
            if (connector.Kind != GiantTombConnectorKind.Open)
            {
                return connector.Cells;
            }
            bool spanAlongZ = connector.Direction == Rot4.East || connector.Direction == Rot4.West;
            int minimum = int.MaxValue;
            int maximum = int.MinValue;
            for (int i = 0; i < connector.Cells.Count; i++)
            {
                int offset = spanAlongZ ? connector.Cells[i].z : connector.Cells[i].x;
                minimum = Math.Min(minimum, offset);
                maximum = Math.Max(maximum, offset);
            }
            int length = spanAlongZ ? height : width;
            while (minimum > 0 && IsArtificialBoundaryOpening(metadata, connector.Direction, minimum - 1, width, height)) minimum--;
            while (maximum + 1 < length && IsArtificialBoundaryOpening(metadata, connector.Direction, maximum + 1, width, height)) maximum++;

            List<IntVec3> result = new List<IntVec3>();
            for (int offset = minimum; offset <= maximum; offset++)
            {
                result.Add(BoundaryCell(connector.Direction, offset, width, height));
            }
            return result;
        }

        //函数职责：判断指定边界偏移是否属于连续的人工可行走开口。
        private static bool IsArtificialBoundaryOpening(GiantTombTemplateMetadata metadata, Rot4 direction, int offset, int width, int height)
        {
            IntVec3 cell = BoundaryCell(direction, offset, width, height);
            return GridValue(metadata.WallDoorGrid, cell.x, cell.z, height) == '0'
                && GridValue(metadata.OccupancyGrid, cell.x, cell.z, height) == '1'
                && GridValue(metadata.WalkabilityGrid, cell.x, cell.z, height) == '1';
        }

        //函数职责：把边界方向和跨度偏移转换为模板局部格坐标。
        private static IntVec3 BoundaryCell(Rot4 direction, int offset, int width, int height)
        {
            if (direction == Rot4.North) return new IntVec3(offset, 0, height - 1);
            if (direction == Rot4.East) return new IntVec3(width - 1, 0, offset);
            if (direction == Rot4.South) return new IntVec3(offset, 0, 0);
            return new IntVec3(0, 0, offset);
        }

        //函数职责：把metadata连接点类型转换为受控枚举并拒绝未知类型。
        private static GiantTombConnectorKind ParseKind(string value, string defName)
        {
            if (value == "Door") return GiantTombConnectorKind.Door;
            if (value == "Open") return GiantTombConnectorKind.Open;
            throw new InvalidDataException("巨型墓葬连接点类型未知: " + defName + "/" + value);
        }

        //函数职责：把metadata方向转换为原版四向旋转并拒绝未知方向。
        private static Rot4 ParseDirection(string value, string defName)
        {
            if (value == "North") return Rot4.North;
            if (value == "East") return Rot4.East;
            if (value == "South") return Rot4.South;
            if (value == "West") return Rot4.West;
            throw new InvalidDataException("巨型墓葬连接点方向未知: " + defName + "/" + value);
        }

        //函数职责：确认连接点格子处于声明方向对应的模板边缘。
        private static void ValidateBoundaryCell(IntVec3 cell, Rot4 direction, int width, int height, string defName, int connectorIndex)
        {
            bool inBounds = cell.x >= 0 && cell.z >= 0 && cell.x < width && cell.z < height;
            bool onEdge = direction == Rot4.North && cell.z == height - 1
                || direction == Rot4.South && cell.z == 0
                || direction == Rot4.East && cell.x == width - 1
                || direction == Rot4.West && cell.x == 0;
            if (!inBounds || !onEdge)
            {
                throw new InvalidDataException("巨型墓葬连接点不在声明边界: " + defName + "/" + connectorIndex);
            }
        }

        //函数职责：封闭全部开放口后从矩形边界洪泛背景，得到不会漏掉天然室内地面的结构掩码。
        private static bool[] BuildStructureMask(GiantTombTemplateMetadata metadata, List<GiantTombConnector> connectors, int width, int height)
        {
            int count = width * height;
            bool[] blocked = new bool[count];
            bool[] exterior = new bool[count];
            Queue<IntVec3> queue = new Queue<IntVec3>();
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    char code = GridValue(metadata.WallDoorGrid, x, z, height);
                    blocked[z * width + x] = code == '1' || code == '2';
                }
            }
            SealArtificialBoundaryFloor(metadata, width, height, blocked);
            for (int i = 0; i < connectors.Count; i++)
            {
                for (int j = 0; j < connectors[i].Cells.Count; j++)
                {
                    IntVec3 cell = connectors[i].Cells[j];
                    blocked[cell.z * width + cell.x] = true;
                }
            }
            for (int x = 0; x < width; x++)
            {
                EnqueueExterior(x, 0, width, height, blocked, exterior, queue);
                EnqueueExterior(x, height - 1, width, height, blocked, exterior, queue);
            }
            for (int z = 1; z < height - 1; z++)
            {
                EnqueueExterior(0, z, width, height, blocked, exterior, queue);
                EnqueueExterior(width - 1, z, width, height, blocked, exterior, queue);
            }
            while (queue.Count > 0)
            {
                IntVec3 cell = queue.Dequeue();
                for (int i = 0; i < GenAdj.CardinalDirections.Length; i++)
                {
                    IntVec3 next = cell + GenAdj.CardinalDirections[i];
                    EnqueueExterior(next.x, next.z, width, height, blocked, exterior, queue);
                }
            }
            bool[] result = new bool[count];
            for (int i = 0; i < count; i++) result[i] = !exterior[i];
            return result;
        }

        //函数职责：把边界人工地面作为洪泛挡板，避免未达到连接深度的边缘格泄漏背景，同时不改变有效连接点宽度。
        private static void SealArtificialBoundaryFloor(GiantTombTemplateMetadata metadata, int width, int height, bool[] blocked)
        {
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x != 0 && z != 0 && x != width - 1 && z != height - 1) continue;
                    bool artificialFloor = GridValue(metadata.OccupancyGrid, x, z, height) == '1';
                    bool walkable = GridValue(metadata.WalkabilityGrid, x, z, height) == '1';
                    if (artificialFloor && walkable) blocked[z * width + x] = true;
                }
            }
        }

        //函数职责：把一个尚未访问的非阻挡格加入背景洪泛队列。
        private static void EnqueueExterior(int x, int z, int width, int height, bool[] blocked, bool[] exterior, Queue<IntVec3> queue)
        {
            if (x < 0 || z < 0 || x >= width || z >= height) return;
            int index = z * width + x;
            if (blocked[index] || exterior[index]) return;
            exterior[index] = true;
            queue.Enqueue(new IntVec3(x, 0, z));
        }

        //函数职责：确认所有人工地板或建筑格都被结构掩码覆盖。
        private static void ValidateOccupancyInsideMask(GiantTombTemplateMetadata metadata, bool[] mask, int width, int height, string defName)
        {
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = z * width + x;
                    if (GridValue(metadata.OccupancyGrid, x, z, height) == '1' && !mask[index])
                    {
                        throw new InvalidDataException("巨型墓葬模板存在结构外人工占用格: " + defName + " @ " + x + "," + z);
                    }
                }
            }
        }

        //函数职责：按metadata的北到南行序读取指定局部坐标的字符。
        private static char GridValue(List<string> rows, int x, int z, int height)
        {
            return rows[height - 1 - z][x];
        }
    }
}
