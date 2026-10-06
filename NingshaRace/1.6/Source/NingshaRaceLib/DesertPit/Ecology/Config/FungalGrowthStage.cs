using Verse;
using UnityEngine;

namespace NingshaRaceLib.DesertPit.Ecology.Config
{
    //保存菌类幼体阶段的成长率、贴图和根部对齐偏移。
    public sealed class FungalGrowthStage
    {
        public float maxGrowth;
        public string texPath;
        public Vector3 drawOffset;
        private GraphicData cachedGraphicData;

        //替换阶段贴图与根部偏移，复用原植物的材质、尺寸和图集设置。
        public Graphic GraphicFor(GraphicData matureGraphicData)
        {
            if (cachedGraphicData == null)
            {
                cachedGraphicData = new GraphicData();
                cachedGraphicData.CopyFrom(matureGraphicData);
                cachedGraphicData.texPath = texPath;
                cachedGraphicData.drawOffset = drawOffset;
                cachedGraphicData.shaderParameters = matureGraphicData.shaderParameters;
            }
            return cachedGraphicData.Graphic;
        }
    }
}
