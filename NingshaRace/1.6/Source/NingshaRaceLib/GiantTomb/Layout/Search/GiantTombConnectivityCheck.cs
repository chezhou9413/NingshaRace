using System;
using System.Collections.Generic;

namespace NingshaRaceLib.GiantTomb.Layout
{
    //类职责：按剩余模板类别传播接口可达性，剪去无法接入主树的模块组合。
    internal sealed class GiantTombConnectivityCheck
    {
        private readonly GiantTombModule[] modules;
        private readonly bool[] reached;
        private readonly HashSet<int> signatures = new HashSet<int>();

        //职责：为模板类别分配复用缓冲区，避免反复传播同类模板的重复实例。
        public GiantTombConnectivityCheck(GiantTombModule[] modules)
        {
            this.modules = modules;
            reached = new bool[modules.Length];
        }

        //职责：从未连接出口传播兼容接口，确认每种剩余模板至少能间接接入主树。
        public bool CanConnect(Stack<int>[] instances, IReadOnlyList<GiantTombFrontierDomain> domains)
        {
            signatures.Clear();
            for (int i = 0; i < domains.Count; i++)
            {
                GiantTombPlacedConnector connector = domains[i].Connector;
                if (!connector.Connected) signatures.Add(GiantTombConnectorCompatibility.Signature(connector.Kind, connector.Cells.Count));
            }
            if (signatures.Count == 0) return false;
            Array.Clear(reached, 0, reached.Length);
            bool changed;
            do
            {
                changed = false;
                for (int i = 0; i < modules.Length; i++)
                {
                    if (instances[i].Count == 0 || reached[i] || !HasCompatibleConnector(modules[i])) continue;
                    reached[i] = true;
                    changed = true;
                    foreach (GiantTombConnector connector in modules[i].Connectors)
                        signatures.Add(GiantTombConnectorCompatibility.Signature(connector.Kind, connector.Cells.Count));
                }
            }
            while (changed);
            for (int i = 0; i < modules.Length; i++)
                if (instances[i].Count > 0 && !reached[i]) return false;
            return true;
        }

        //职责：判断模板是否有至少一个接口能够接入已知签名集合。
        private bool HasCompatibleConnector(GiantTombModule module)
        {
            foreach (GiantTombConnector connector in module.Connectors)
            {
                foreach (int signature in signatures)
                {
                    if (GiantTombConnectorCompatibility.AreCompatible((GiantTombConnectorKind)(signature >> 16),
                        signature & 0xFFFF, connector.Kind, connector.Cells.Count)) return true;
                }
            }
            return false;
        }
    }
}
