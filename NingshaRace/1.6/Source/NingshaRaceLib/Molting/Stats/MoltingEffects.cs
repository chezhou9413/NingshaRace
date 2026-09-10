namespace NingshaRaceLib.Molting.Stats
{
    //类职责：统一提供蜕皮层数的实际修正与界面效果说明。
    public static class MoltingEffects
    {
        //函数职责：计算意识的乘算倍率。
        public static float ConsciousnessFactor(int count) => 1f + 0.1f * count;

        //函数职责：计算移动能力的乘算倍率。
        public static float MovingFactor(int count) => 1f + 0.08f * count;

        //函数职责：识别采用加算的属性。
        public static bool IsOffset(string mode) => mode == "MeleeDodgeChance" || mode == "ErosionLimit";

        //函数职责：计算指定属性的倍率或加算值，并直接报告未知模式。
        public static float StatModifier(string mode, int count)
        {
            switch (mode)
            {
                case "HealingFactor": return 1f + 0.03f * count;
                case "IncomingDamageFactor": return 1f - 0.03f * count;
                case "MeleeDodgeChance": return 0.01f * count;
                case "LifespanFactor": return (1f + 0.02f * count) * 1.4f;
                case "ErosionLimit": return 0.5f * count;
                default: throw new System.ArgumentException("未知蜕皮属性模式：" + mode);
            }
        }

        //函数职责：展示当前层数对应的七项修正，闪避加算以百分点表达。
        public static string Description(int count)
        {
            return "当前蜕皮效果："
                + "\n意识：×" + ConsciousnessFactor(count).ToString("0.##")
                + "\n移动能力：×" + MovingFactor(count).ToString("0.##")
                + "\n承伤系数：×" + StatModifier("IncomingDamageFactor", count).ToString("0.##")
                + "\n近战闪避率：+" + (StatModifier("MeleeDodgeChance", count) * 100f).ToString("0.##") + " 个百分点"
                + "\n愈合系数：×" + StatModifier("HealingFactor", count).ToString("0.##")
                + "\n寿命系数：×" + StatModifier("LifespanFactor", count).ToString("0.###")
                + "\n侵蚀上限：+" + StatModifier("ErosionLimit", count).ToString("0.##");
        }
    }
}
