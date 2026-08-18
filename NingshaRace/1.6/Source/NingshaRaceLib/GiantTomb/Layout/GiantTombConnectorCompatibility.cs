using System.Collections.Generic;

namespace NingshaRaceLib.GiantTomb.Layout
{
    //类职责：集中定义墓葬接口兼容规则，并校验模板池中各兼容组能否两两闭合。
    internal static class GiantTombConnectorCompatibility
    {
        //函数职责：判断两个接口的类型和有效宽度是否允许直接拼接。
        public static bool AreCompatible(GiantTombConnectorKind firstKind, int firstWidth, GiantTombConnectorKind secondKind, int secondWidth)
        {
            if (firstKind == secondKind && firstWidth == secondWidth) return true;
            if (firstWidth == 2 && secondWidth == 2 && firstKind != secondKind) return true;
            return firstKind != secondKind && (firstWidth == 1 && secondWidth == 3 || firstWidth == 3 && secondWidth == 1);
        }

        //函数职责：确认每个互相兼容的接口组都有偶数个接口，从拓扑上允许全部两两连接。
        public static bool HasEvenConnectorComponents(List<GiantTombModule> modules)
        {
            Dictionary<int, int> counts = new Dictionary<int, int>();
            for (int moduleIndex = 0; moduleIndex < modules.Count; moduleIndex++)
            {
                List<GiantTombConnector> connectors = modules[moduleIndex].Connectors;
                for (int connectorIndex = 0; connectorIndex < connectors.Count; connectorIndex++)
                {
                    int signature = Signature(connectors[connectorIndex].Kind, connectors[connectorIndex].Cells.Count);
                    counts.TryGetValue(signature, out int count);
                    counts[signature] = count + 1;
                }
            }

            List<int> signatures = new List<int>(counts.Keys);
            HashSet<int> visited = new HashSet<int>();
            Queue<int> queue = new Queue<int>();
            for (int i = 0; i < signatures.Count; i++)
            {
                int start = signatures[i];
                if (!visited.Add(start)) continue;
                int componentCount = 0;
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    componentCount += counts[current];
                    for (int j = 0; j < signatures.Count; j++)
                    {
                        int candidate = signatures[j];
                        if (visited.Contains(candidate) || !AreSignaturesCompatible(current, candidate)) continue;
                        visited.Add(candidate);
                        queue.Enqueue(candidate);
                    }
                }
                if ((componentCount & 1) != 0) return false;
            }
            return true;
        }

        //函数职责：把接口类型和宽度压缩为稳定整数，供兼容组计数使用。
        public static int Signature(GiantTombConnectorKind kind, int width)
        {
            return ((int)kind << 16) | width;
        }

        //函数职责：解码两个接口签名并套用统一兼容规则。
        private static bool AreSignaturesCompatible(int first, int second)
        {
            GiantTombConnectorKind firstKind = (GiantTombConnectorKind)(first >> 16);
            GiantTombConnectorKind secondKind = (GiantTombConnectorKind)(second >> 16);
            return AreCompatible(firstKind, first & 0xFFFF, secondKind, second & 0xFFFF);
        }
    }
}
