using System.Collections.Generic;
using System.Linq;
using RimWorld;
using NingshaRaceLib.Combat.MeleeAnimation;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //为震尼系列实体提供默认图层及编辑期间的独立外观。
    public sealed class CompBladebearerAppearance : ThingComp, IMeleeAnimationPreviewAddon
    {
        internal List<BladebearerPart> Draft;
        internal string SoloId;
        internal bool CapturePreview;
        internal bool Paused;
        internal float Clock;
        private BladebearerBloomPreview meleeBloom;
        internal readonly BladebearerParticleEmitters Particles = new BladebearerParticleEmitters();
        internal readonly Dictionary<string, BladebearerProjection> Projections = new Dictionary<string, BladebearerProjection>();
        internal BladebearerAppearanceDef Definition => ((CompProperties_BladebearerAppearance)props).appearance;
        internal ThingDef WeaponDef => ((CompProperties_BladebearerAppearance)props).weapon;
        internal string WeaponPartId => ((CompProperties_BladebearerAppearance)props).weaponPart;
        internal Pawn Pawn => (Pawn)parent;
        internal CompBladebearerPhaseStep Phase => Pawn.TryGetComp<CompBladebearerPhaseStep>();
        internal float AnimationTime => Draft == null && !CapturePreview ? Time.realtimeSinceStartup : Clock;

        //拥刀者的选择性辉光通过通用预览接口参与相机绘制。
        public void BeginMeleePreview(RenderTexture target, float time)
        {
            CapturePreview = true; Clock = time;
            if (meleeBloom == null) meleeBloom = new BladebearerBloomPreview();
            meleeBloom.Begin(target);
        }
        //相机回调错误交回通用编辑器，明确显示失败原因。
        public void CheckMeleePreview() { meleeBloom.CheckError(); }
        //预览结束后恢复地图绘制上下文。
        public void EndMeleePreview()
        { CapturePreview = false; meleeBloom?.End(); }
        //关闭通用编辑器时释放拥刀者专用的预览后处理。
        public void CloseMeleePreview()
        { meleeBloom?.Dispose(); meleeBloom = null; }

        //生成时补齐专属装备；倒地与收容期间保持空手。
        public override void PostSpawnSetup(bool respawningAfterLoad)
        { base.PostSpawnSetup(respawningAfterLoad); EnsureWeapon(); }

        //恢复行动或收容逃脱后重新持刀，不在渲染线程修改装备。
        public override void CompTickRare() { EnsureWeapon(); }

        //专属武器交给原版公开持械规则，待机时也可显示。
        public override bool WantHoldWeapon(Pawn pawn) => pawn.equipment.Primary?.def == WeaponDef;

        //装备由原版装备栏保存，专属武器使用原版销毁式弃置。
        private void EnsureWeapon()
        {
            if (!Pawn.Spawned || Pawn.Dead || Pawn.Downed || Pawn.ParentHolder is Building_HoldingPlatform
                || Pawn.equipment.Primary != null) return;
            Pawn.equipment.AddEquipment((ThingWithComps)ThingMaker.MakeThing(WeaponDef));
            Rebuild();
        }

        //空引用表示独立节奏；有引用时必须使用当前草稿里的对应火焰。
        internal BladebearerPart ParticleFlame(BladebearerPart particle)
        {
            if (string.IsNullOrEmpty(particle.particleFlame)) return particle;
            foreach (BladebearerPart part in Draft ?? Definition.parts)
                if (part.id == particle.particleFlame && part.flame && !part.particle) return part;
            throw new System.InvalidOperationException("部件「" + particle.label + "」particleFlame：找不到火焰「" + particle.particleFlame + "」。");
        }

        //结构或显隐发生变化时只使当前 Pawn 的渲染树失效。
        internal void Rebuild() { Particles.Dispose(); Projections.Clear(); Pawn.Drawer.renderer.renderTree.SetDirty(); }

        //离开地图或被销毁时释放粒子网格。
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        { Particles.Dispose(); base.PostDeSpawn(map, mode); }

        //未经过正常离图流程的销毁也释放当前组件拥有的网格。
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        { Particles.Dispose(); base.PostDestroy(mode, previousMap); }

        //身体按深度与颜色分层提交，武器部件只由原版持械节点提交。
        public override List<PawnRenderNode> CompRenderNodes()
        {
            var ordered = (Draft ?? Definition.parts).OrderBy(p => p.layer).ToList();
            var nodes = new List<PawnRenderNode>();
            var weaponParts = new List<PawnRenderNode_BladebearerPart>();
            foreach (BladebearerPart p in ordered) if (p.writeDepth)
            {
                var node = CreateNode(p, true);
                if (p.attachment == "Weapon") weaponParts.Add(node); else nodes.Add(node);
            }
            foreach (BladebearerPart p in ordered)
            {
                var node = CreateNode(p, false);
                if (p.attachment == "Weapon") weaponParts.Add(node); else nodes.Add(node);
            }
            var weaponProps = new PawnRenderNodeProperties_Carried
            {
                nodeClass = typeof(PawnRenderNode_BladebearerWeapon),
                workerClass = typeof(PawnRenderNodeWorker_BladebearerWeapon),
                pawnType = PawnRenderNodeProperties.RenderNodePawnType.Any,
                debugLabel = Pawn.def.label + "握持", skipFlag = RenderSkipFlagDefOf.None
            };
            var weaponNode = new PawnRenderNode_BladebearerWeapon(Pawn, weaponProps, Pawn.Drawer.renderer.renderTree)
            { Parts = weaponParts };
            nodes.Add(weaponNode);
            return nodes;
        }

        //固定使用 Any，让非人类异常实体也能使用分层外观。
        private PawnRenderNode_BladebearerPart CreateNode(BladebearerPart part, bool depth)
        {
            var properties = new PawnRenderNodeProperties
            {
                nodeClass = typeof(PawnRenderNode_BladebearerPart), workerClass = typeof(PawnRenderNodeWorker_BladebearerPart),
                pawnType = PawnRenderNodeProperties.RenderNodePawnType.Any, useGraphic = false,
                debugLabel = part.label + (depth ? " 深度" : ""), baseLayer = part.layer,
                skipFlag = RenderSkipFlagDefOf.None
            };
            return new PawnRenderNode_BladebearerPart(Pawn, properties, Pawn.Drawer.renderer.renderTree)
            { Part = part, Owner = this, DepthOnly = depth };
        }

        //开发者模式下给选中的震尼实体显示独立编辑入口。
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Prefs.DevMode) yield return new Command_Action
            {
                defaultLabel = Pawn.def.label + "外观编辑器", defaultDesc = "切换四向外观，调整部件、握持与火焰，并导入或导出 XML。",
                icon = TexCommand.DesirePower, action = () => Find.WindowStack.Add(new Window_BladebearerEditor(this))
            };
        }

        //退出编辑后移除临时参数，下一次绘制回到 Def。
        internal void EndEditing()
        {
            Draft = null; SoloId = null; CapturePreview = false; Paused = false;
            Rebuild();
        }
    }
}
