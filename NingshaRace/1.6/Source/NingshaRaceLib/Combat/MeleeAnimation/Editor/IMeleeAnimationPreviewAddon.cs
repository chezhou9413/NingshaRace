using UnityEngine;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //拥有独立后处理的外观组件可参与相机预览，框架无需认识具体怪物。
    public interface IMeleeAnimationPreviewAddon
    {
        void BeginMeleePreview(RenderTexture target, float time);
        void CheckMeleePreview();
        void EndMeleePreview();
        void CloseMeleePreview();
    }
}
