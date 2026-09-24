using System;
using NingshaRaceLib.DesertPit.Ecology.Config;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Plants
{
    //类职责：让洞穴食用菌随真实成长率切换造型，并同步刷新野生和种植菌类的地图网格。
    public sealed class Plant_FungalGrowth : Plant
    {
        private DefModExtension_FungalGrowth growthSettings;

        //属性职责：取得当前植物的阶段配置，缺少配置时直接报告定义错误。
        private DefModExtension_FungalGrowth Settings => growthSettings ?? (growthSettings =
            def.GetModExtension<DefModExtension_FungalGrowth>()
                ?? throw new InvalidOperationException(def.defName + "缺少菌类生长阶段配置。"));

        //属性职责：区分播种占位图与各生长阶段，保证开始萌发时也会刷新造型。
        private int VisualStage => LifeStage == PlantLifeStage.Sowing ? -1 : Settings.StageIndex(Growth);

        //属性职责：按当前成长率返回幼体或成熟图像，保留原版播种和枯萎状态的处理。
        public override Graphic Graphic
        {
            get
            {
                int stage = VisualStage;
                if (stage < 0 || LeaflessNow || stage == Settings.stages.Count) return base.Graphic;
                return Settings.stages[stage].GraphicFor(def.graphicData);
            }
        }

        //属性职责：沿用原版成长率范围，并在外部直接设置成长率后提交外观变化。
        public override float Growth
        {
            get => base.Growth;
            set
            {
                int previousStage = VisualStage;
                int previousSizeStep = (int)(base.Growth * 10f);
                base.Growth = value;
                RefreshGraphic(previousStage, previousSizeStep);
            }
        }

        //函数职责：沿用原版生长、寿命和环境计算，额外提交野生菌类跨阶段的绘制变化。
        public override void TickLong()
        {
            int previousStage = VisualStage;
            int previousSizeStep = (int)(Growth * 10f);
            //原版直接更新成长率字段，且常规尺寸刷新仅覆盖种植区，需要在长周期结尾检查野生菌。
            base.TickLong();
            RefreshGraphic(previousStage, previousSizeStep);
        }

        //函数职责：只在阶段或十分之一尺寸区间变化时标记本格重绘，不产生逐帧检查。
        private void RefreshGraphic(int previousStage, int previousSizeStep)
        {
            if (Spawned && (previousStage != VisualStage || previousSizeStep != (int)(Growth * 10f)))
                Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Things);
        }
    }
}
