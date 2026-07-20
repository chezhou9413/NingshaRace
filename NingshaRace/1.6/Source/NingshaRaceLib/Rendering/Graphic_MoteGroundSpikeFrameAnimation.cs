using UnityEngine;
using Verse;

namespace NingshaRaceLib.Rendering
{
    //类职责：从五列四行图集中按 Mote 生命周期绘制二十帧地刺动画。
    public class Graphic_MoteGroundSpikeFrameAnimation : Graphic_Mote
    {
        private const int FrameCount = 20;
        private const int FrameColumns = 5;
        private const int FrameRows = 4;
        private Material material;
        private Mesh[] frameMeshes;

        public override Material MatSingle => material ?? BaseContent.BadMat;

        public override Material MatWest => MatSingle;

        public override Material MatSouth => MatSingle;

        public override Material MatEast => MatSingle;

        public override Material MatNorth => MatSingle;

        //函数职责：为所有朝向返回地刺图集材质。
        public override Material MatAt(Rot4 rot, Thing thing = null)
        {
            return MatSingle;
        }

        //函数职责：加载地刺图集材质并建立二十份共享逐帧 UV 网格。
        public override void Init(GraphicRequest req)
        {
            data = req.graphicData;
            path = req.path;
            maskPath = req.maskPath;
            color = req.color;
            colorTwo = req.colorTwo;
            drawSize = req.drawSize;

            Texture2D texture = ContentFinder<Texture2D>.Get(path, reportFailure: false);
            if (texture == null)
            {
                Log.Error("未找到地刺 Mote 图集纹理：" + path);
                material = BaseContent.BadMat;
            }
            else
            {
                material = MaterialPool.MatFrom(new MaterialRequest(texture, req.shader, color));
            }

            frameMeshes = new Mesh[FrameCount];
            for (int frameIndex = 0; frameIndex < FrameCount; frameIndex++)
            {
                frameMeshes[frameIndex] = CreateFrameMesh(frameIndex);
            }
        }

        //函数职责：依据 Mote 播放进度选择当前帧并绘制地刺网格。
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

        //函数职责：按五列四行图集位置建立单帧方形网格的 UV 坐标。
        private static Mesh CreateFrameMesh(int frameIndex)
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
            mesh.name = "NingshaRace_MoteGroundSpikeFrame_" + frameIndex.ToString("D2") + "_Grid";
            return mesh;
        }

        //函数职责：绘制没有实际 Mote 实例时使用的首帧预览。
        private void DrawStaticFrame(Vector3 loc, Rot4 rot)
        {
            if (frameMeshes == null || frameMeshes.Length == 0)
            {
                return;
            }

            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(loc, rot.AsQuat, new Vector3(drawSize.x, 1f, drawSize.y));
            propertyBlock.SetColor(ShaderPropertyIDs.Color, color);
            Graphics.DrawMesh(frameMeshes[0], matrix, material, 0, null, 0, propertyBlock);
        }

        //函数职责：使用 Mote 的透明度、缩放和旋转状态绘制当前地刺帧。
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
            Graphics.DrawMesh(mesh, matrix, material, 0, null, 0, propertyBlock);
        }

        //函数职责：创建带新颜色和 Shader 的同类地刺图集图形实例。
        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {
            return GraphicDatabase.Get<Graphic_MoteGroundSpikeFrameAnimation>(
                path,
                newShader,
                drawSize,
                newColor,
                newColorTwo,
                data,
                maskPath);
        }
    }
}
