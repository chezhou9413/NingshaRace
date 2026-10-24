using UnityEngine;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //在发射点读取火焰共用的 RG 波动，每帧只采样发射器，不逐颗读取贴图。
    internal struct BladebearerParticleFlow
    {
        private readonly BladebearerPart flame;
        private readonly Texture2D noise;
        private readonly float across, distance;

        //先把粒子根部转换到火焰画布，避免移动、旋转或缩放后取错波动位置。
        internal BladebearerParticleFlow(BladebearerPart particle, BladebearerPart flame)
        {
            this.flame = flame;
            noise = BladebearerResources.Noise(flame.displacementTexture);
            Vector3 point = Quaternion.AngleAxis(particle.angle, Vector3.up)
                * new Vector3((particle.root.x - 0.5f) * particle.size.x, 0, (particle.root.y - 0.5f) * particle.size.y);
            point += new Vector3(particle.position.x - flame.position.x, 0, particle.position.y - flame.position.y);
            point = Quaternion.AngleAxis(-flame.angle, Vector3.up) * point;
            Vector2 relative = new Vector2(point.x / flame.size.x + 0.5f, point.z / flame.size.y + 0.5f) - flame.root;
            Vector2 axis = (flame.tip - flame.root).normalized;
            float angle = flame.flowAngle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(axis.x * Mathf.Cos(angle) - axis.y * Mathf.Sin(angle),
                axis.x * Mathf.Sin(angle) + axis.y * Mathf.Cos(angle));
            across = Vector2.Dot(relative, new Vector2(-direction.y, direction.x));
            distance = Vector2.Dot(relative, direction);
        }

        //与 BladebearerFlameCommon 的粗细 RG 采样一致，时钟、速度、相位和密度都取自对应火焰。
        internal Vector2 Sample(float clock)
        {
            float time = clock * flame.speed + flame.phase;
            Vector2 uv = new Vector2(flame.phase * 0.37f + 0.173f, distance * flame.noiseScale.y - time);
            Vector2 coarse = Read(uv);
            Vector2 fine = Read(uv * 2.07f + new Vector2(across * flame.noiseScale.x + 0.31f, 0.67f - time * 0.37f));
            //纹理采样坐标向右移动时，画面里的火苗向左移动。
            return -(coarse + fine * flame.detailStrength) / (1 + flame.detailStrength);
        }

        //共用线性、双线性和 Repeat 采样，保留 RG 通道的正负波动。
        private Vector2 Read(Vector2 uv)
        {
            Color value = noise.GetPixelBilinear(Mathf.Repeat(uv.x, 1), Mathf.Repeat(uv.y, 1));
            return new Vector2(value.r * 2 - 1, value.g * 2 - 1);
        }
    }
}
