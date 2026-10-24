using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //在当前朝向创建独立的紫焰火星发射器，复用现有粒子 Shader 和配置字段。
    internal static class BladebearerParticleEditing
    {
        //选中火焰时直接创建，否则让美工选择当前面上的火焰。
        internal static void ChooseSource(Window_BladebearerEditor editor)
        {
            BladebearerPart selected = editor.Selected;
            if (selected != null && selected.flame && selected.VisibleAt(editor.Preview.Facing))
            { Create(editor, selected); return; }
            var sources = editor.Owner.Draft.Where(p => p.flame && !p.particle && p.VisibleAt(editor.Preview.Facing)).ToList();
            if (sources.Count == 0)
            { editor.Status = "当前朝向没有可跟随的火焰，请先配置火焰部件。"; return; }
            Find.WindowStack.Add(new FloatMenu(sources.Select(source =>
                new FloatMenuOption("跟随：" + source.label, () => Create(editor, source))).ToList()));
        }

        //从火焰取得画布与方向，火星数量、路径和颜色使用独立的紫色默认参数。
        private static void Create(Window_BladebearerEditor editor, BladebearerPart source)
        {
            string facing = editor.Preview.Facing.ToStringWord();
            BladebearerPart part = source.Copy();
            part.id = "PurpleSparks_" + facing + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            part.label = BladebearerParameterPanel.FacingLabel(facing) + " · 紫色火星";
            part.facing = facing; part.enabled = true; part.flame = false; part.particle = true; part.writeDepth = false;
            part.texture = "Zhenni/Bladebearer/Weapon/Bladebearer_DaggerSparks";
            part.particleFlame = source.id;
            part.layer = Mathf.Clamp(source.layer + 1, source.attachment == "Weapon" ? 0 : -10, source.attachment == "Weapon" ? 20 : 100);
            part.color = new Color(0.7f, 0.38f, 1, 1); part.additive = 0; part.displacement = Vector2.zero; part.breakup = 0;
            part.lightIntensity = 1; part.lightFlickerStrength = 0; part.bloomIntensity = 0.28f; part.bloomThreshold = 0.65f;
            part.particleRate = 18; part.particleLifetime = 2.4f; part.particleSpeed = 0.22f; part.particleSize = 0.01f;
            part.particleSpread = 45; part.particleRadius = 0.015f; part.particleSway = 0.03f;
            editor.Owner.Draft.Add(part); editor.RememberInitial(part); editor.Select(part.id);
            editor.ShowParticles(); editor.Changed(true);
            editor.Status = "已添加本朝向紫色火星：拖绿点移动发射点，拖青色箭头调整方向；导出 XML 保存。";
        }
    }
}
