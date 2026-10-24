using UnityEngine;

namespace NingshaRaceLib.Combat.SaltedFishGreatsword
{
    //一次出手对应一句临时台词，逐字显示后统一进入错落消散阶段。
    internal sealed class SaltedFishQuote
    {
        internal static readonly string[] Lines =
        {
            "我是扫咸鱼",
            "求求你了平海酱，只要你帮帮我，我扫咸鱼什么都会做的",
            "其实伦家一直是这样的咸鱼酱，各位大人",
            "虫佬夺舍什么的，才不存在，人家就是这样的咸鱼酱",
            "我不介意的",
            "伦家可是超级可爱咸鱼酱，你们来我的群不是理所应当的"
        };

        internal const float LetterDelay = 0.045f;
        internal readonly string[] Letters;
        internal readonly int PawnId;
        internal readonly Vector3 Position;
        internal readonly float Phase;
        internal readonly int LineIndex;
        internal float Age;
        internal float StackOffset;
        internal float FadeStart => Letters.Length * LetterDelay + 1.3f;
        internal float Lifetime => FadeStart + 1.15f;

        //预先拆出字符，绘制时不重复创建字符串。
        internal SaltedFishQuote(int lineIndex, int pawnId, Vector3 position, float phase)
        {
            LineIndex = lineIndex;
            PawnId = pawnId;
            Position = position;
            Phase = phase;
            string line = Lines[lineIndex];
            Letters = new string[line.Length];
            for (int i = 0; i < line.Length; i++) Letters[i] = line[i].ToString();
        }
    }
}
