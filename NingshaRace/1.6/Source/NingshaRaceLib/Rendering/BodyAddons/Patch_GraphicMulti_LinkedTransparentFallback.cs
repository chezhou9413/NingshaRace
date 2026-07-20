using HarmonyLib;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Rendering.BodyAddons
{
    //类职责：让受管链接 BodyAddon 的缺失方向使用共享透明材质，而不是借用其他方向纹理。
    [HarmonyPatch(typeof(Graphic_Multi), nameof(Graphic_Multi.Init))]
    public static class Patch_GraphicMulti_LinkedTransparentFallback
    {
        //字段职责：访问 Graphic_Multi 的四方向材质缓存。
        private static readonly AccessTools.FieldRef<Graphic_Multi, Material[]> MatsRef = AccessTools.FieldRefAccess<Graphic_Multi, Material[]>("mats");

        //字段职责：定义材质数组与 Rot4.AsInt 一致的方向后缀顺序。
        private static readonly string[] DirectionSuffixes = { "_north", "_east", "_south", "_west" };

        //函数职责：只为已注册的链接层自行构造四方向材质，并把缺失方向替换为共享透明材质。
        [HarmonyPrefix]
        public static bool Prefix(Graphic_Multi __instance, GraphicRequest req)
        {
            if (!BodyAddonLinkFallbackUtility.IsManagedTexturePath(req.path))
            {
                return true;
            }

            __instance.data = req.graphicData;
            __instance.path = req.path;
            __instance.maskPath = req.maskPath;
            __instance.color = req.color;
            __instance.colorTwo = req.colorTwo;
            __instance.drawSize = req.drawSize;

            Material[] materials = new Material[DirectionSuffixes.Length];
            for (int index = 0; index < DirectionSuffixes.Length; index++)
            {
                string suffix = DirectionSuffixes[index];
                Texture2D texture = ContentFinder<Texture2D>.Get(req.path + suffix, reportFailure: false);
                materials[index] = texture == null ? BaseContent.ClearMat : CreateMaterial(req, texture, suffix);
            }

            MatsRef(__instance) = materials;
            return false;
        }

        //函数职责：使用原 GraphicRequest 的 Shader、颜色、遮罩和队列创建存在方向的正常材质。
        private static Material CreateMaterial(GraphicRequest req, Texture2D texture, string directionSuffix)
        {
            Texture2D mask = null;
            if (req.shader.SupportsMaskTex())
            {
                string maskBasePath = req.maskPath.NullOrEmpty() ? req.path : req.maskPath;
                string maskSuffix = req.maskPath.NullOrEmpty() ? "m" : string.Empty;
                mask = ContentFinder<Texture2D>.Get(maskBasePath + directionSuffix + maskSuffix, reportFailure: false);
            }

            MaterialRequest materialRequest = new MaterialRequest
            {
                mainTex = texture,
                shader = req.shader,
                color = req.color,
                colorTwo = req.colorTwo,
                maskTex = mask,
                shaderParameters = req.shaderParameters,
                renderQueue = req.renderQueue
            };
            return MaterialPool.MatFrom(materialRequest);
        }
    }
}
