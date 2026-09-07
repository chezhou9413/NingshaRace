using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Config
{
    //类职责：配置分层洞群、水系和额外菌巢的规模，供两种自然巨坑共用。
    public sealed class DefModExtension_DesertPitLayout : DefModExtension
    {
        public FloatRange mainRadiusX = new FloatRange(34f, 40f);
        public FloatRange mainRadiusZ = new FloatRange(28f, 34f);
        public IntRange centralRoomCount = new IntRange(6, 8);
        public FloatRange smallRoomRadius = new FloatRange(9f, 14f);
        public FloatRange secondaryRoomRadius = new FloatRange(18f, 24f);
        public IntRange linkRoomCount = new IntRange(3, 5);
        public IntRange outerRoomCount = new IntRange(3, 5);
        public FloatRange outerRoomRadius = new FloatRange(10f, 16f);
        public IntRange branchRoomCount = new IntRange(2, 3);
        public float riverDiagonalReach = 0.82f;
        public float riverMeander = 7f;
        public float innerBoundary = 1f / 3f;
        public float outerBoundary = 2f / 3f;
        public float antPreferredLayer = 0.59f;
        public FloatRange riverWidth = new FloatRange(3f, 5f);
        public float riverBankWidth = 2f;
        public float mainDryRadius = 12f;
        public IntRange extraMoundsPerRoom = new IntRange(0, 3);
        public float moundSpacing = 12f;

        //函数职责：按地图短边缩放洞室，限制极大地图的局部洞体尺寸。
        public float Scale(Map map) => Mathf.Clamp(Mathf.Min(map.Size.x, map.Size.z) / 200f, 0.75f, 1.5f);

        //函数职责：计算格子在同心方形中的层位，零为中心、一为地图边缘。
        public static float Layer(Map map, IntVec3 cell)
        {
            float x = Mathf.Abs(cell.x - (map.Size.x - 1) * 0.5f) / ((map.Size.x - 1) * 0.5f);
            float z = Mathf.Abs(cell.z - (map.Size.z - 1) * 0.5f) / ((map.Size.z - 1) * 0.5f);
            return Mathf.Max(x, z);
        }

        //函数职责：检查关键布局尺寸及数量，避免无法比较的数值进入生成搜索。
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors()) yield return error;
            if (!ValidRange(mainRadiusX) || !ValidRange(mainRadiusZ) || !ValidRange(smallRoomRadius)
                || !ValidRange(secondaryRoomRadius) || !ValidRange(outerRoomRadius) || !ValidRange(riverWidth)) yield return "洞室半径和河宽必须是有限正数区间，最大不超过六十。";
            if (!(riverWidth.min >= 3f && riverWidth.max <= 8f)) yield return "贯穿浅河的完整宽度必须为三至八格，保证连续浅水和岸边通路。";
            if (!(0f < innerBoundary && innerBoundary < antPreferredLayer && antPreferredLayer < outerBoundary && outerBoundary < 1f))
                yield return "洞层分界必须依次满足：零、内层界、蚁巢层位、中外层界、一。";
            if (!(mainDryRadius >= 12f && mainDryRadius <= 20f) || !(riverBankWidth >= 1f && riverBankWidth <= 5f))
                yield return "出生干地半径必须为十二至二十格，河岸宽度必须为一至五格。";
            if (!ValidCount(centralRoomCount, 4, 10) || !ValidCount(linkRoomCount, 3, 8)
                || !ValidCount(outerRoomCount, 2, 8) || !ValidCount(branchRoomCount, 0, 5)
                || !ValidCount(extraMoundsPerRoom, 0, 6)) yield return "洞室或可选菌巢数量超出允许区间。";
            if (!(riverDiagonalReach >= 0.5f && riverDiagonalReach <= 0.9f)
                || !(riverMeander >= 2f && riverMeander <= 12f)) yield return "斜河端点展开比例必须为零点五至零点九，曲流幅度必须为二至十二格。";
            if (!(moundSpacing >= 12f && moundSpacing <= 30f)) yield return "菌巢最小间距必须为十二至三十格。";
            if (secondaryRoomRadius.max >= Mathf.Min(mainRadiusX.min, mainRadiusZ.min)) yield return "次要洞室必须小于主洞室。";
        }

        //函数职责：识别有序、有限且有上界的正数尺寸。
        private static bool ValidRange(FloatRange range) => range.min > 0f && range.max >= range.min && range.max <= 60f;

        //函数职责：识别落在指定安全区间内的整数数量范围。
        private static bool ValidCount(IntRange range, int min, int max) => range.min >= min && range.max >= range.min && range.max <= max;
    }
}
