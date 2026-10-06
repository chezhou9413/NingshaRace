using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //一个美术图层的摆放与置换参数，XML 和编辑器共用此结构。
    public sealed class BladebearerPart
    {
        public string id;
        public string label;
        public string texture;
        public string facing = "South";
        public string attachment = "Body";
        public string displacementTexture = "BladebearerDisplacement";
        public bool enabled = true;
        public bool writeDepth;
        public bool flame;
        public bool particle;
        public string particleFlame = "";
        public Vector2 position;
        public Vector2 size = Vector2.one;
        public float angle;
        public float layer = 50f;
        public Color color = Color.white;
        public float additive;
        public Vector2 displacement;
        public Vector2 root = new Vector2(0.5f, 0.1f);
        public Vector2 tip = new Vector2(0.5f, 0.9f);
        public float rootLock = 0.1f;
        public float bendPower = 0.8f;
        public Vector2 noiseScale = new Vector2(0.5f, 0.7f);
        public float speed = 0.9f;
        public float flowAngle;
        public float detailStrength = 0.35f;
        public float breakup;
        public Vector2 breakupScale = new Vector2(1.8f, 2.2f);
        public float phase;
        public float bloomIntensity = 0.8f;
        public float bloomThreshold = 0.5f;
        public float lightIntensity = 1f;
        public float lightFlickerStrength;
        public float lightFlickerFrequency = 1.3f;
        public float particleRate = 24f;
        public float particleLifetime = 2.4f;
        public float particleSpeed = 0.22f;
        public float particleSize = 0.010f;
        public float particleSpread = 45f;
        public float particleRadius = 0.015f;
        public float particleSway = 0.03f;

        //火焰、动态粒子和原画加色星光共用选择性辉光。
        internal bool HasBloom => flame || particle || additive > 0;

        //XML 使用方向单词；Rot4.ToString() 是数字，必须取 ToStringWord() 才能匹配。
        internal bool VisibleAt(Rot4 rotation) => facing == "All" || facing == rotation.ToStringWord();

        //参数仅包含值类型与不可变字符串，复制后可以独立编辑。
        public BladebearerPart Copy() => (BladebearerPart)MemberwiseClone();

        //以根部到尾端为零度，方向手柄独立于原图的根部和固定范围。
        internal Vector2 FlowHandleUv()
        {
            float radians = flowAngle * Mathf.Deg2Rad;
            Vector2 axis = tip - root;
            float sin = Mathf.Sin(radians), cos = Mathf.Cos(radians);
            return root + new Vector2(cos * axis.x - sin * axis.y, sin * axis.x + cos * axis.y) * 0.7f;
        }
    }
}
