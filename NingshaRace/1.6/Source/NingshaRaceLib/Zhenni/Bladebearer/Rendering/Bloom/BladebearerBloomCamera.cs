using System;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //在地图相机透明阶段之后捕获可见火焰，沿用地图深度，再在图像回调中合成辉光。
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class BladebearerBloomCamera : MonoBehaviour
    {
        private const CameraEvent CaptureEvent = CameraEvent.AfterForwardAlpha;
        private static Camera attached;
        private Camera camera;
        private CommandBuffer commands;
        private RenderTexture emission;
        private BladebearerBloomProcessor processor;
        private int preparedFrame = -1;
        private Map preparedMap;
        private bool failed;

        //复用游戏地图相机，组件只安装一次。
        internal static void EnsureAttached()
        {
            Camera current = Find.Camera;
            if (current == null || current == attached) return;
            if (current.GetComponent<BladebearerBloomCamera>() == null) current.gameObject.AddComponent<BladebearerBloomCamera>();
            attached = current;
        }

        //缓存当前组件所属的相机。
        private void Awake() { camera = GetComponent<Camera>(); }

        //只为当前地图实际提交的部件录制命令，离图和无火焰时释放缓冲。
        private void OnPreRender()
        {
            preparedFrame = -1;
            if (failed || camera != Find.Camera || Current.ProgramState != ProgramState.Playing
                || Find.CurrentMap == null || !WorldRendererUtility.DrawingMap || LongEventHandler.ShouldWaitForEvent)
            {
                Release(); BladebearerBloomSources.Clear(); return;
            }
            BladebearerBloomDraws draws = BladebearerBloomSources.Current;
            if (draws == null || !draws.HasEmission) { Release(); return; }
            try
            {
                EnsureTarget();
                draws.Sort(camera);
                commands.Clear();
                //颜色是独立半精度缓冲；深度始终来自地图，只清颜色，不修改场景深度。
                commands.SetRenderTarget(emission, BuiltinRenderTextureType.CameraTarget);
                commands.ClearRenderTarget(false, true, Color.clear);
                commands.SetViewport(new Rect(0, 0, emission.width, emission.height));
                foreach (BladebearerBloomDraws.Draw draw in draws.Items)
                    if (draw.Mesh != null)
                        commands.DrawMesh(draw.Mesh, draw.Matrix, draw.CaptureMaterial(false), 0, draw.CapturePass, draw.Properties);
                commands.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                commands.SetViewport(camera.pixelRect);
                preparedMap = Find.CurrentMap; preparedFrame = Time.frameCount;
            }
            catch (Exception error) { StopAfterError(error); }
        }

        //场景只叠加已遮挡的火焰亮部，不把地图、头盔或身体当作发光来源。
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            try
            {
                if (preparedFrame == Time.frameCount && preparedMap == Find.CurrentMap && emission != null)
                    processor.Render(source, emission, destination);
                else Graphics.Blit(source, destination);
            }
            catch (Exception error)
            {
                StopAfterError(error);
                Graphics.Blit(source, destination);
            }
            finally { RenderTexture.active = destination; }
        }

        //颜色缓冲与地图深度保持相同尺寸和抗锯齿采样数，尺寸未变时复用。
        private void EnsureTarget()
        {
            int samples = camera.targetTexture != null ? camera.targetTexture.antiAliasing
                : camera.allowMSAA && camera.actualRenderingPath == RenderingPath.Forward ? Mathf.Max(1, QualitySettings.antiAliasing) : 1;
            int width = camera.targetTexture != null ? camera.targetTexture.width : camera.pixelWidth;
            int height = camera.targetTexture != null ? camera.targetTexture.height : camera.pixelHeight;
            if (emission != null && emission.width == width && emission.height == height && emission.antiAliasing == samples) return;
            Release();
            if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
                throw new NotSupportedException("当前显卡不支持震尼拥刀者辉光所需的半精度纹理。");
            processor = new BladebearerBloomProcessor();
            emission = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf)
            {
                name = "Bladebearer.BloomSources", antiAliasing = samples, filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
            };
            if (!emission.Create()) throw new InvalidOperationException("震尼拥刀者辉光缓冲创建失败。");
            commands = new CommandBuffer { name = "Bladebearer.VisibleFlames" };
            camera.AddCommandBuffer(CaptureEvent, commands);
        }

        //报错后停止辉光，原火焰仍按普通深度路径绘制，避免每帧重复报错。
        private void StopAfterError(Exception error)
        {
            failed = true; Release();
            Log.Error("[震尼拥刀者] 相机辉光失败，已停止本次运行的辉光：" + error);
        }

        //先解除相机命令，再释放材质和纹理。
        private void Release()
        {
            preparedFrame = -1; preparedMap = null;
            if (commands != null)
            {
                camera.RemoveCommandBuffer(CaptureEvent, commands); commands.Release(); commands = null;
            }
            if (emission != null) { emission.Release(); Destroy(emission); emission = null; }
            processor?.Dispose(); processor = null;
        }

        //相机销毁或退出地图时不保留上一场景的来源引用。
        private void OnDisable()
        {
            Release();
            if (attached != camera) return;
            attached = null; BladebearerBloomSources.Clear();
        }
    }
}
