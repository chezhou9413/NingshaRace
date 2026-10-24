using NingshaRaceLib.Zhenni.Bladebearer;
using UnityEngine;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//反求火焰贴图上的可见连接点，跟随片元置换而不是未变形的画布。
internal readonly struct BowbearerFlameAnchor
{
    private readonly BladebearerPart part;
    private readonly Texture2D noise;
    private readonly Vector2 axis, direction;
    private readonly float lengthSquared, time, sine, cosine;

    //采样契约与 BladebearerFlameCommon 的线性 RG、Repeat 和游戏时钟一致。
    internal BowbearerFlameAnchor(BladebearerPart part, float clock)
    {
        this.part = part;
        noise = BladebearerResources.Noise(part.displacementTexture);
        axis = part.tip - part.root;
        lengthSquared = Mathf.Max(axis.sqrMagnitude, .00001f);
        sine = Mathf.Sin(part.flowAngle * Mathf.Deg2Rad);
        cosine = Mathf.Cos(part.flowAngle * Mathf.Deg2Rad);
        direction = Rotate(axis / Mathf.Sqrt(lengthSquared), sine, cosine);
        time = clock * part.speed + part.phase;
    }

    //Shader 从屏幕点反查纹理，因此需要求解 displacedUV(可见位置) = 纹理锚点。
    internal Vector2 Position(Vector2 anchor)
    {
        if (part.displacement.sqrMagnitude < .000000000001f) return anchor;
        Vector2 best = Solve(anchor, anchor);
        float bestError = (Displaced(best) - anchor).sqrMagnitude;
        //二维破碎会折叠映射；在实际位移界限内寻找其余分支，不能把局部极小误当作连接点。
        Vector2 reach = part.displacement * ((1 + part.breakup * .65f) * .75f);
        for (int y = -1; y <= 1 && bestError > .00000001f; y++)
            for (int x = -1; x <= 1 && bestError > .00000001f; x++)
            {
                if (x == 0 && y == 0) continue;
                Vector2 candidate = Solve(anchor, anchor + Rotate(new Vector2(x * reach.x, y * reach.y), sine, cosine));
                float error = (Displaced(candidate) - anchor).sqrMagnitude;
                if (error < bestError) { best = candidate; bestError = error; }
            }
        return best;
    }

    //限制每步距离并检查残差下降，避免高梯度破碎区迭代发散。
    private Vector2 Solve(Vector2 anchor, Vector2 position)
    {
        const float epsilon = .001f;
        for (int iteration = 0; iteration < 8; iteration++)
        {
            Vector2 value = Displaced(position), error = value - anchor;
            if (error.sqrMagnitude < .00000001f) break;
            Vector2 x = (Displaced(position + Vector2.right * epsilon) - value) / epsilon;
            Vector2 y = (Displaced(position + Vector2.up * epsilon) - value) / epsilon;
            float determinant = x.x * y.y - x.y * y.x;
            //破碎场局部折叠时使用有界残差步，避免连接点跳到另一段火苗。
            Vector2 step = Mathf.Abs(determinant) > .0001f
                ? new Vector2(y.y * error.x - y.x * error.y, x.x * error.y - x.y * error.x) / determinant
                : error;
            step = Vector2.ClampMagnitude(step, .05f);
            bool advanced = false;
            for (int reduction = 0; reduction < 4; reduction++)
            {
                Vector2 candidate = position - step;
                if ((Displaced(candidate) - anchor).sqrMagnitude < error.sqrMagnitude)
                { position = candidate; advanced = true; break; }
                step *= .5f;
            }
            if (!advanced) break;
        }
        return position;
    }

    //同时包含主波动、细节与二维破碎，保持根部固定区和流向旋转一致。
    private Vector2 Displaced(Vector2 uv)
    {
        Vector2 relative = uv - part.root;
        float along = Mathf.Clamp01(Vector2.Dot(relative, axis) / lengthSquared);
        float weight = Mathf.Pow(Smooth(part.rootLock, 1, along), part.bendPower);
        float across = Vector2.Dot(relative, new Vector2(-direction.y, direction.x));
        float distance = Vector2.Dot(relative, direction);
        Vector2 noiseUV = new(part.phase * .37f + .173f, distance * part.noiseScale.y - time);
        Vector2 coarse = Read(noiseUV);
        Vector2 fine = Read(noiseUV * 2.07f + new Vector2(across * part.noiseScale.x + .31f, .67f - time * .37f));
        Vector2 displacement = (coarse + fine * part.detailStrength) / (1 + part.detailStrength);
        Vector2 result = uv + Rotate(Vector2.Scale(displacement, part.displacement), sine, cosine) * weight;
        if (part.breakup > .001f)
        {
            float inverseLength = 1 / Mathf.Sqrt(lengthSquared);
            Vector2 tear = Read(new Vector2(across * inverseLength * part.breakupScale.x + part.phase * .43f,
                distance * inverseLength * part.breakupScale.y - time * 2.2f + part.phase * .19f)) * 2.5f;
            tear.x = Mathf.Clamp(tear.x, -1, 1); tear.y = Mathf.Clamp(tear.y, -1, 1);
            result += Rotate(Vector2.Scale(tear, part.displacement), sine, cosine)
                * (part.breakup * Smooth(part.rootLock + .04f, .65f, along) * .65f);
        }
        return result;
    }

    //置换纹理按循环双线性采样读取，不使用贴图颜色的 sRGB 转换。
    private Vector2 Read(Vector2 uv)
    {
        Color color = noise.GetPixelBilinear(Mathf.Repeat(uv.x, 1), Mathf.Repeat(uv.y, 1));
        return new Vector2(color.r * 2 - 1, color.g * 2 - 1);
    }

    //与 Shader 的二维流向矩阵同向旋转。
    private static Vector2 Rotate(Vector2 value, float sine, float cosine)
        => new(value.x * cosine - value.y * sine, value.x * sine + value.y * cosine);

    //Shader smoothstep 的参数是输入区间，不能直接套用 Mathf 的输出插值区间。
    private static float Smooth(float min, float max, float value)
        => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(min, max, value));
}
