using RimWorld.Planet;
using UnityEngine;
using Verse;

using NingshaRaceLib.Erosion.Utility;

namespace NingshaRaceLib.Erosion.Effects
{
    //类职责：在 Unity 每帧阶段检查侵蚀头部特效是否仍属于当前游戏和当前地图，阻止实例穿透到世界地图或其他地图。
    public sealed class ErosionHeadEffectFollower : MonoBehaviour
    {
        //字段职责：保存当前特效跟随的侵蚀体。
        private Pawn pawn;

        //字段职责：保存创建特效时所属的游戏，读档切换游戏后用于识别旧实例。
        private Game game;

        //字段职责：保存创建特效时所属的地图，切换地图后用于立即回收旧实例。
        private Map map;

        //函数职责：把运行时特效绑定到指定侵蚀体及其当前游戏和地图。
        public void Bind(Pawn targetPawn)
        {
            pawn = targetPawn;
            game = Current.Game;
            map = targetPawn.Map;
        }

        //函数职责：每个 Unity 帧结束时验证游戏、地图、存活与侵蚀体状态，并在失效时销毁特效。
        private void LateUpdate()
        {
            if (IsValidForCurrentView())
            {
                return;
            }

            gameObject.SetActive(false);
            ErosionHeadEffectManager.NotifyInstanceDestroyed(pawn, gameObject);
            Destroy(gameObject);
            enabled = false;
        }

        //函数职责：在预制体因自身粒子设置被销毁时同步移除运行时索引。
        private void OnDestroy()
        {
            ErosionHeadEffectManager.NotifyInstanceDestroyed(pawn, gameObject);
        }

        //函数职责：判断特效是否仍应显示在当前打开的地图上，倒地状态不会中断跟随。
        private bool IsValidForCurrentView()
        {
            return Current.ProgramState == ProgramState.Playing
                && WorldRendererUtility.DrawingMap
                && Current.Game == game
                && pawn != null
                && !pawn.Destroyed
                && !pawn.Dead
                && pawn.Spawned
                && pawn.Map == map
                && Find.CurrentMap == map
                && ErosionPawnUtility.IsErosionBody(pawn);
        }
    }
}
