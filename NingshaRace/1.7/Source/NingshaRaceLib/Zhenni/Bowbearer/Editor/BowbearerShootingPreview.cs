using System;
using NingshaRaceLib.Zhenni.Bladebearer;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//使用同一个 Pawn 相机和实际武器绘制回调预览完整射击。
internal sealed class BowbearerShootingPreview : IDisposable
{
    private readonly CompBowbearerShooting owner;
    private readonly BladebearerBloomPreview bloom = new();
    private readonly MaterialPropertyBlock block = new();
    private RenderTexture texture;
    private BowbearerSpiralTrail trail;
    private BowbearerSettings trailSettings;
    private BowbearerSettings visualSettings;
    internal bool Empowered;
    private BowbearerSettings VisualSettings => visualSettings ??= Empowered ? owner.Settings.empowered.Visuals(owner.Settings) : owner.Settings;
    internal Rot4 Facing = Rot4.East;
    internal float Time;
    internal float Distance = 8;
    internal float Zoom = .25f;
    internal string Error;
    internal bool LightBackground;
    internal float Warmup => owner.Pawn.equipment.Primary.def.Verbs[0].warmupTime * (Empowered ? owner.Settings.empowered.warmupMultiplier : 1);
    internal float Speed => owner.Pawn.equipment.Primary.def.Verbs[0].defaultProjectile.projectile.SpeedTilesPerTick * 60;
    internal float Duration => Warmup + Mathf.Max(owner.Settings.releaseSeconds + owner.Settings.recoverySeconds,
        Distance / Speed + Mathf.Max(1.5f, Mathf.Max(VisualSettings.impactDuration, VisualSettings.arrowAshLifetime))) + .2f;

    //草稿或模式变化后才重建视觉副本，避免每次绘制复制图层。
    internal void Invalidate() { visualSettings = null; Error = null; }

    //编辑会话与地图持有独立的渲染资源。
    internal BowbearerShootingPreview(CompBowbearerShooting owner) { this.owner = owner; }

    //在 IMGUI 控件之前绘制，避免相机中断输入。
    internal void Render()
    {
        if (Event.current.type != EventType.Repaint || texture == null || Error != null) return;
        Camera camera = Find.PawnCacheCamera;
        var appearance = owner.Pawn.GetComp<CompBladebearerAppearance>();
        Color background = camera.backgroundColor;
        var flags = camera.clearFlags; var active = RenderTexture.active; var target = camera.targetTexture;
        Vector3 position = camera.transform.position; float size = camera.orthographicSize;
        float oldClock = appearance.Clock; bool oldCapture = appearance.CapturePreview;
        try
        {
            appearance.CapturePreview = true; appearance.Clock = Time;
            owner.PreviewActive = true; owner.PreviewProgress = Time < Warmup ? Time / Warmup : -1;
            owner.PreviewEmpowered = Empowered; owner.PreviewVisuals = VisualSettings;
            owner.PreviewWarmupRemaining = Warmup - Time;
            owner.PreviewReleased = Time >= Warmup ? Time - Warmup : -1;
            owner.PreviewDraw = DrawProjectile;
            camera.backgroundColor = LightBackground ? new Color(.62f, .63f, .65f, 1) : new Color(.08f, .09f, .12f, 1);
            camera.clearFlags = CameraClearFlags.SolidColor;
            bloom.Begin(texture);
            Vector3 forward = Quaternion.AngleAxis(BowbearerWeaponDraws.PreviewAim(Facing), Vector3.up) * Vector3.forward;
            Find.PawnCacheRenderer.RenderPawn(owner.Pawn, texture, forward * Distance * .35f, Zoom, 0, Facing,
                renderHead: true, renderHeadgear: true, renderClothes: true, portrait: false);
            bloom.CheckError();
        }
        catch (Exception e)
        {
            Error = "射击预览失败：" + e.Message;
            Log.Error("[震尼挽弓者] " + e);
        }
        finally
        {
            bloom.End(); appearance.CapturePreview = oldCapture; appearance.Clock = oldClock;
            owner.PreviewActive = false; owner.PreviewDraw = null;
            camera.backgroundColor = background; camera.clearFlags = flags;
            camera.transform.position = position; camera.orthographicSize = size;
            camera.targetTexture = target; RenderTexture.active = active;
        }
    }

    //回放仅重建视觉采样，不生成地图弹丸或造成伤害。
    private void DrawProjectile(Vector3 pawnPosition)
    {
        if (Time < Warmup) return;
        if (trailSettings != VisualSettings)
        { trail?.Dispose(); trailSettings = VisualSettings; trail = new BowbearerSpiralTrail(trailSettings); }
        trail.Reset();
        float age = Time - Warmup, flight = Distance / Speed;
        float angle = BowbearerWeaponDraws.PreviewAim(Facing);
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
        Vector3 direction = rotation * Vector3.forward;
        Vector3 origin = BowbearerWeaponDraws.Muzzle(owner.Pawn, pawnPosition, angle, Facing, owner.Settings);
        Vector3 destination = pawnPosition + direction * Distance;
        float end = Mathf.Min(age, flight);
        for (int i = 0; i <= 96; i++)
        {
            float t = end * i / 96f;
            trail.Record(Vector3.Lerp(origin, destination, t / flight), rotation, Warmup + t);
        }
        if (age >= flight) trail.Impact(destination, rotation, Warmup + flight);
        trail.Draw(Time, true);
        if (age < flight) BowbearerArrowDraws.Draw(VisualSettings, Vector3.Lerp(origin, destination, age / flight), rotation, Time, true, block);
    }

    //视口只显示纹理，分配大小按 UI 比例限制。
    internal void Draw(Rect area)
    {
        int width = Mathf.Clamp(Mathf.RoundToInt(area.width * Prefs.UIScale), 64, 1600);
        int height = Mathf.Clamp(Mathf.RoundToInt(area.height * Prefs.UIScale), 64, 1600);
        if (texture == null || texture.width != width || texture.height != height)
        {
            if (texture != null) { texture.Release(); UnityEngine.Object.Destroy(texture); }
            texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { name = "挽弓者射击预览" };
            if (!texture.Create()) throw new InvalidOperationException("无法创建射击预览纹理。");
        }
        if (Event.current.type == EventType.Repaint) GUI.DrawTexture(area, texture);
        if (Error != null) Widgets.Label(area.ContractedBy(12), Error);
        if (Event.current.type == EventType.ScrollWheel && area.Contains(Event.current.mousePosition))
        { Zoom = Mathf.Clamp(Zoom * Mathf.Exp(-Event.current.delta.y * .08f), .08f, 2); Event.current.Use(); }
    }

    //释放会话创建的 GPU 资源和绘制回调。
    public void Dispose()
    {
        owner.PreviewActive = false; owner.PreviewDraw = null;
        bloom.Dispose(); trail?.Dispose();
        if (texture != null) { texture.Release(); UnityEngine.Object.Destroy(texture); texture = null; }
    }
}
