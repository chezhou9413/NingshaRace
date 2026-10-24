namespace NingshaRaceLib.Zhenni.Bowbearer;

//强化视觉使用独立页，保留普通箭的全部参数。
internal sealed partial class BowbearerShootingPanel
{
    //战斗范围固定由 Def 决定，编辑器只调整预警、箭体和爆焰外观。
    private void Empowered(BowbearerEmpoweredSettings s)
    {
        Label("每三发普通箭后发射一发强化箭");
        Number("warmupMultiplier", "强化蓄力倍率", ref s.warmupMultiplier, 1.1f, 4);
        Label("拥刀者同款星芒预警");
        Number("flashLead", "放箭前提前量（秒）", ref s.flashLead, .1f, 2);
        Number("flashDuration", "闪光时长（秒）", ref s.flashDuration, .05f, 1);
        Number("flashSize", "星芒画布大小（格）", ref s.flashSize, .2f, 4);
        Number("flashIntensity", "星芒亮芯强度", ref s.flashIntensity, 0, 3);
        Label("强化箭体");
        Number("arrowScale", "箭体尺寸倍率", ref s.arrowScale, 1, 2);
        Tint("arrowColor", ref s.arrowColor);
        Label("三股螺旋");
        Number("radius", "螺旋半径（格）", ref s.radius, 0, 1);
        Number("pitch", "每圈前进距离（格）", ref s.pitch, .1f, 6);
        Number("width", "火焰带宽度（格）", ref s.width, .005f, .5f);
        Number("length", "拖尾长度（格）", ref s.length, .1f, 12);
        Number("lifetime", "拖尾寿命（秒）", ref s.lifetime, .03f, 2);
        Number("rotationsPerSecond", "旋转速度（圈／秒）", ref s.rotationsPerSecond, -8, 8);
        Number("trailIntensity", "螺旋亮度", ref s.trailIntensity, 0, 4);
        Number("trailGlow", "螺旋柔光", ref s.trailGlow, 0, 2);
        Tint("trailColor", ref s.trailColor);
        Label("体积爆焰（伤害半径固定两格）");
        Number("impactRadius", "视觉半径（格）", ref s.impactRadius, .5f, 3);
        Number("impactDuration", "爆焰时长（秒）", ref s.impactDuration, .2f, 2);
        Number("impactIntensity", "爆焰亮度", ref s.impactIntensity, 0, 3);
        Number("impactGlow", "柔光强度", ref s.impactGlow, 0, 2);
        Number("impactDistortion", "翻卷扰动", ref s.impactDistortion, 0, 2);
        float count = s.impactAshCount;
        Number("impactAshCount", "散射灰烬数量", ref count, 0, 128);
        s.impactAshCount = UnityEngine.Mathf.RoundToInt(count);
        Tint("impactColor", ref s.impactColor);
    }
}
