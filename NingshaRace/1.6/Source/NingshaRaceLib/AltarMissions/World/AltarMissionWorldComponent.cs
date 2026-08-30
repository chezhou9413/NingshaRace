using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace NingshaRaceLib.AltarMissions.World
{
    //类职责：跨存档保存当前祭坛任务编号，并确保全世界同时最多存在一个未结束任务。
    public sealed class AltarMissionWorldComponent : WorldComponent
    {
        //字段职责：保存活动祭坛任务的原版Quest编号，负数表示空闲。
        private int activeQuestId = -1;

        //属性职责：取得当前世界的祭坛任务登记组件。
        public static AltarMissionWorldComponent Current => Find.World?.GetComponent<AltarMissionWorldComponent>();

        //属性职责：在校正登记后判断是否仍有未结束祭坛任务。
        public bool HasActiveMission
        {
            get
            {
                ReconcileActiveMission();
                return activeQuestId >= 0;
            }
        }

        //构造函数职责：把祭坛任务唯一登记状态附加到当前世界。
        public AltarMissionWorldComponent(RimWorld.Planet.World world) : base(world)
        {
        }

        //函数职责：登记刚刚成功生成并加入任务管理器的祭坛任务。
        public bool TryRegister(Quest quest)
        {
            ReconcileActiveMission();
            if (quest == null || activeQuestId >= 0)
            {
                return false;
            }
            activeQuestId = quest.id;
            return true;
        }

        //函数职责：在指定任务结束或异常消失后释放全局祭坛任务名额。
        public void Release(int questId)
        {
            if (activeQuestId == questId)
            {
                activeQuestId = -1;
            }
        }

        //函数职责：每六十Tick核对一次活动任务，及时释放已经结束或丢失的编号。
        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame % 60 == 0)
            {
                ReconcileActiveMission();
            }
        }

        //函数职责：保存任务编号并在读档完成后按Quest真实状态校正登记。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref activeQuestId, "activeAltarQuestId", -1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ReconcileActiveMission();
            }
        }

        //函数职责：以原版任务管理器为权威移除已结束或异常不存在的登记。
        private void ReconcileActiveMission()
        {
            if (activeQuestId < 0 || Find.QuestManager == null)
            {
                return;
            }
            Quest quest = Find.QuestManager.QuestsListForReading.FirstOrDefault(candidate => candidate.id == activeQuestId);
            if (quest == null || quest.State != QuestState.NotYetAccepted && quest.State != QuestState.Ongoing)
            {
                activeQuestId = -1;
            }
        }
    }
}
