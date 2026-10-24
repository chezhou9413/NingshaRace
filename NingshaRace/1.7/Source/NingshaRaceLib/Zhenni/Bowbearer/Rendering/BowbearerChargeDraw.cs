using NingshaRaceLib.Combat.MeleeAnimation;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//强化拉弦使用拥刀者的同一张星芒、材质和明灭曲线。
internal static class BowbearerChargeDraw
{
    //预警跟随弓口；倒地、取消预热或发射后不再显示。
    internal static void Draw(CompBowbearerShooting owner, Vector3 focus, PawnDrawParms parms)
    {
        if (!owner.ChargingEmpowered) return;
        var settings = owner.Settings.empowered;
        float elapsed = settings.flashLead - owner.WarmupRemaining;
        if (elapsed <= 0 || elapsed >= settings.flashDuration) return;
        float progress = elapsed / settings.flashDuration;
        float flash = Mathf.SmoothStep(0, 1, progress / .1f)
            * (1 - Mathf.SmoothStep(0, 1, (progress - .36f) / .64f));
        float size = settings.flashSize * Mathf.Lerp(.86f, 1, flash);
        float brightness = 1 + settings.flashIntensity * .6f;
        Color color = new Color(brightness, brightness, brightness, flash * Mathf.Clamp01(settings.flashIntensity * .8f)) * parms.tint;
        var block = owner.FlashProperties;
        Material material = MeleeAnimationResources.Flash(color, block, parms.DrawNow);
        focus.y += .03f;
        GenDraw.DrawMeshNowOrLater(MeshPool.plane10, Matrix4x4.TRS(focus, Quaternion.identity, new Vector3(size, 1, size)),
            material, parms.DrawNow, block);
        block.Clear();
    }
}
