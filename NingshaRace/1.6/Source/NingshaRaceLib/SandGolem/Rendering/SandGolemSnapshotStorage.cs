using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.SandGolem.Rendering
{
    //类职责：以四张透明 PNG 保存召唤时的完整外观，避免读档时从空沙傀重新截图。
    public static class SandGolemSnapshotStorage
    {
        //函数职责：在主线程把四方向截图编码成可序列化的文本。
        public static List<string> Encode(Texture2D[] textures)
        {
            if (!UnityData.IsInMainThread) throw new InvalidOperationException("沙傀截图编码必须在主线程执行。");
            List<string> result = new List<string>(Rot4.RotationCount);
            foreach (Texture2D texture in textures) result.Add(Convert.ToBase64String(texture.EncodeToPNG()));
            return result;
        }

        //函数职责：还原本次存档中的召唤外观，缺失数据时明确报告错误。
        public static Texture2D[] Decode(List<string> images)
        {
            if (!UnityData.IsInMainThread) throw new InvalidOperationException("沙傀截图解码必须在主线程执行。");
            if (images == null || images.Count != Rot4.RotationCount)
                throw new InvalidOperationException("沙傀存档缺少完整的四方向截图。");
            Texture2D[] result = new Texture2D[Rot4.RotationCount];
            try
            {
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!result[i].LoadImage(Convert.FromBase64String(images[i])))
                        throw new InvalidOperationException("沙傀截图 PNG 解码失败，方向：" + i);
                    result[i].wrapMode = TextureWrapMode.Clamp;
                    result[i].filterMode = FilterMode.Bilinear;
                }
                return result;
            }
            catch
            {
                foreach (Texture2D texture in result) if (texture != null) UnityEngine.Object.Destroy(texture);
                throw;
            }
        }
    }
}
