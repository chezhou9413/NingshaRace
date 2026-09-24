using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Config
{
    //类职责：保存一个菌类幼体阶段的成长率上限和贴图，并复用成熟造型的渲染参数。
    public sealed class FungalGrowthStage
    {
        public float maxGrowth;
        public string texPath;
        private GraphicData cachedGraphicData;

        //函数职责：按阶段替换贴图路径，保留原植物的材质、尺寸、颜色和图集设置。
        public Graphic GraphicFor(GraphicData matureGraphicData)
        {
            if (cachedGraphicData == null)
            {
                cachedGraphicData = new GraphicData();
                cachedGraphicData.CopyFrom(matureGraphicData);
                cachedGraphicData.texPath = texPath;
                cachedGraphicData.shaderParameters = matureGraphicData.shaderParameters;
            }
            return cachedGraphicData.Graphic;
        }
    }
}
