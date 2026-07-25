using UnityEngine;
using Verse;

using NingshaRaceLib.Combat.SnakeBellySword.Tracking;
using NingshaRaceLib.Combat.SnakeBellySword.Utility;
using NingshaRaceLib.Combat.SnakeBellySword.Verbs;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Combat.SnakeBellySword.Rendering
{
    //类职责：从两张同步横向图集中按 Mote 生命周期切换并绘制鞭子与鞭刃。
    public class Graphic_MoteFrameAnimation : Graphic_Mote
    {
        private const int FrameCount = 21;
        private const int FrameColumns = 7;
        private const int FrameRows = 3;
        private Material whipMaterial;
        private Material bladeMaterial;
        private Mesh[] frameMeshes;

        public override Material MatSingle => whipMaterial ?? BaseContent.BadMat;

        public override Material MatWest => MatSingle;

        public override Material MatSouth => MatSingle;

        public override Material MatEast => MatSingle;

        public override Material MatNorth => MatSingle;

        //函数职责：为所有朝向返回鞭子图集材质。
        public override Material MatAt(Rot4 rot, Thing thing = null)
        {
            return MatSingle;
        }

        //函数职责：加载发光鞭子和不发光鞭刃图集，并建立共享的逐帧 UV 网格。
        public override void Init(GraphicRequest req)
        {
            data = req.graphicData;
            path = req.path;
            maskPath = req.maskPath;
            color = req.color;
            colorTwo = req.colorTwo;
            drawSize = req.drawSize;

            Texture2D whipTexture = ContentFinder<Texture2D>.Get(path, reportFailure: false);
            Texture2D bladeTexture = ContentFinder<Texture2D>.Get(path + "Blade", reportFailure: false);
            whipMaterial = CreateMaterial(whipTexture, req.shader);
            bladeMaterial = CreateMaterial(bladeTexture, ShaderDatabase.Mote);
            EnsureBladeLayerIsOnTop();

            frameMeshes = new Mesh[FrameCount];
            for (int frameIndex = 0; frameIndex < FrameCount; frameIndex++)
            {
                frameMeshes[frameIndex] = CreateFrameMesh(frameIndex);
            }
        }

        //函数职责：提高刃层材质的渲染队列，确保不发光刃层覆盖在红色发光层上。
        private void EnsureBladeLayerIsOnTop()
        {
            if (whipMaterial == null || bladeMaterial == null)
            {
                return;
            }

            bladeMaterial.renderQueue = Mathf.Max(bladeMaterial.renderQueue, whipMaterial.renderQueue + 1);
        }

        //函数职责：为单张纹理和指定 Shader 建立可用于 Mote 绘制的材质。
        private Material CreateMaterial(Texture2D texture, Shader shader)
        {
            if (texture == null)
            {
                Log.Error("未找到 Mote 图集纹理：" + path);
                return BaseContent.BadMat;
            }

            MaterialRequest materialRequest = new MaterialRequest(texture, shader, color)
            {
                colorTwo = colorTwo
            };
            return MaterialPool.MatFrom(materialRequest);
        }

        //函数职责：按七列三行图集位置建立单帧方形网格的 UV 坐标。
        private Mesh CreateFrameMesh(int frameIndex)
        {
            Mesh mesh = MeshMakerPlanes.NewPlaneMesh(1f);
            int column = frameIndex % FrameColumns;
            int rowFromTop = frameIndex / FrameColumns;
            int rowFromBottom = FrameRows - 1 - rowFromTop;
            float frameMin = (float)column / FrameColumns;
            float frameMax = (float)(column + 1) / FrameColumns;
            float rowMin = (float)rowFromBottom / FrameRows;
            float rowMax = (float)(rowFromBottom + 1) / FrameRows;
            mesh.uv = new[]
            {
                new Vector2(frameMin, rowMin),
                new Vector2(frameMin, rowMax),
                new Vector2(frameMax, rowMax),
                new Vector2(frameMax, rowMin)
            };
            mesh.name = "NingshaRace_MoteWhipFrame_" + frameIndex.ToString("D2") + "_Grid";
            return mesh;
        }

        //函数职责：依据 Mote 已播放时间选择当前帧并绘制双层动画网格。
        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            if (!(thing is Mote mote) || frameMeshes == null || frameMeshes.Length == 0)
            {
                DrawStaticFrame(loc, rot);
                return;
            }

            float lifespan = Mathf.Max(thingDef.mote.Lifespan, 1f / 60f);
            float progress = Mathf.Clamp01(mote.AgeSecs / lifespan);
            int frameIndex = Mathf.Min(Mathf.FloorToInt(progress * FrameCount), FrameCount - 1);
            DrawMoteFrame(frameMeshes[frameIndex], mote);
        }

        //函数职责：绘制没有实际 Mote 实例时使用的首帧双层预览。
        private void DrawStaticFrame(Vector3 loc, Rot4 rot)
        {
            if (frameMeshes == null || frameMeshes.Length == 0)
            {
                return;
            }

            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(loc, rot.AsQuat, new Vector3(drawSize.x, 1f, drawSize.y));
            propertyBlock.SetColor(ShaderPropertyIDs.Color, color);
            Graphics.DrawMesh(frameMeshes[0], matrix, whipMaterial, 0, null, 0, propertyBlock);
            Vector3 bladeLoc = loc;
            bladeLoc.y += 0.001f;
            Matrix4x4 bladeMatrix = default(Matrix4x4);
            bladeMatrix.SetTRS(bladeLoc, rot.AsQuat, new Vector3(drawSize.x, 1f, drawSize.y));
            Graphics.DrawMesh(frameMeshes[0], bladeMatrix, bladeMaterial, 0, null, 0, propertyBlock);
        }

        //函数职责：使用 Mote 的透明度、缩放和旋转状态按鞭子后鞭刃前的顺序绘制当前帧。
        private void DrawMoteFrame(Mesh mesh, Mote mote)
        {
            float alpha = mote.Alpha;
            if (alpha <= 0f)
            {
                return;
            }

            Color drawColor = color * mote.instanceColor;
            drawColor.a *= alpha;
            Vector3 scale = mote.ExactScale;
            scale.x *= data.drawSize.x;
            scale.z *= data.drawSize.y;
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(mote.DrawPos, Quaternion.AngleAxis(mote.exactRotation, Vector3.up), scale);

            propertyBlock.SetColor(ShaderPropertyIDs.Color, drawColor);
            Graphics.DrawMesh(mesh, matrix, whipMaterial, 0, null, 0, propertyBlock);
            Vector3 bladeLoc = mote.DrawPos;
            bladeLoc.y += 0.001f;
            Matrix4x4 bladeMatrix = default(Matrix4x4);
            bladeMatrix.SetTRS(bladeLoc, Quaternion.AngleAxis(mote.exactRotation, Vector3.up), scale);
            Graphics.DrawMesh(mesh, bladeMatrix, bladeMaterial, 0, null, 0, propertyBlock);
        }

        //函数职责：创建带新颜色和 Shader 的同类双层图集图形实例。
        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {
            return GraphicDatabase.Get<Graphic_MoteFrameAnimation>(path, newShader, drawSize, newColor, newColorTwo, data, maskPath);
        }
    }
}
