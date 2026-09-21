using UnityEngine;

namespace NingshaRaceLib.Erosion.Rendering
{
    //类职责：保存同一头部贴图的活体流动材质与尸体静态材质，供绘制线程只读选择。
    internal sealed class ErosionHeadMaterialVariants
    {
        public readonly Material Alive;
        public readonly Material Dead;

        //函数职责：接收主线程完成配置的两份独立材质并固定引用。
        public ErosionHeadMaterialVariants(Material alive, Material dead)
        {
            Alive = alive;
            Dead = dead;
        }

        //函数职责：根据人物生命状态返回对应材质，不访问或修改 Unity 原生对象。
        public Material ForState(bool dead)
        {
            return dead ? Dead : Alive;
        }
    }
}
