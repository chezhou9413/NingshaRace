#if RW_1_7
using System.Collections.Generic;
using UnityEngine;

namespace NingshaRaceLib.Combat.SandBottle.Utility
{
    //让沙瓶实例拥有自己的粒子材质，避免修改共享模板或泄漏 GPU 资源。
    public sealed class SandBottleParticleMaterials : MonoBehaviour
    {
        private readonly List<Material> materials = new List<Material>();

        //记录已分配的材质供销毁时统一释放。
        internal void Add(Material material) { materials.Add(material); }

        //预制体自然结束或提前清理时释放所有副本。
        private void OnDestroy()
        {
            foreach (Material material in materials) Object.Destroy(material);
            materials.Clear();
        }
    }
}
#endif
