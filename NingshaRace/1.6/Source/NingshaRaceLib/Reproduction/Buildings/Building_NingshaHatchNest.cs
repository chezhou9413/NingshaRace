using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Reproduction.Components;
using NingshaRaceLib.Reproduction.Utility;

namespace NingshaRaceLib.Reproduction.Buildings
{
    //类职责：容纳单枚凝砂卵、驱动其内部 Tick，并提供装填、弹出、检查与开发者控制。
    public class Building_NingshaHatchNest : Building, IThingHolder
    {
        //字段职责：保存孵化巢内部唯一一枚凝砂卵。
        private ThingOwner<Thing> innerContainer;

        //字段职责：控制普通搬运工作是否会自动向当前巢内装填凝砂卵。
        private bool allowAutoLoad;

        //属性职责：公开孵化巢是否允许普通搬运工作自动装填。
        public bool AllowAutoLoad => allowAutoLoad;

        //属性职责：判断孵化巢当前是否为空。
        public bool Empty => innerContainer.Count == 0;

        //属性职责：取得孵化巢当前容纳的凝砂卵。
        public Thing ContainedEgg => Empty ? null : innerContainer[0];

        //构造函数职责：建立由孵化巢持有的物品容器，并默认关闭自动装填。
        public Building_NingshaHatchNest()
        {
            innerContainer = new ThingOwner<Thing>(this);
            allowAutoLoad = false;
        }

        //函数职责：让巢内卵继续执行腐烂和受精卵孵化组件的 Tick。
        protected override void Tick()
        {
            base.Tick();
            innerContainer.DoTick();
        }

        //函数职责：验证目标为凝砂卵且巢为空后，将其从原持有者转移到巢内。
        public bool TryAcceptEgg(Thing egg)
        {
            if (!Empty || !IsNingshaEgg(egg))
            {
                return false;
            }
            return innerContainer.TryAddOrTransfer(egg, canMergeWithExistingStacks: true);
        }

        //函数职责：判断目标物品是否为允许装入孵化巢的两种凝砂卵。
        public static bool IsNingshaEgg(Thing egg)
        {
            return egg != null && (egg.def == DefOfRefs.NingshaRace_EggUnfertilized || egg.def == DefOfRefs.NingshaRace_EggFertilized);
        }

        //函数职责：向 RimWorld 持有者遍历追加巢内卵可能拥有的子持有者。
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        //函数职责：向搬运、保存与食物搜索系统公开孵化巢的直接容器。
        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        //函数职责：保存巢内物品和自动装填开关。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "ningshaHatchNestContainer", this);
            Scribe_Values.Look(ref allowAutoLoad, "ningshaHatchNestAutoLoad", false);
        }

        //函数职责：在拆除或销毁孵化巢前把内部卵放回地图，避免物品被容器吞掉。
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            if (mode != DestroyMode.WillReplace && Spawned && !Empty)
            {
                innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
            }
            base.DeSpawn(mode);
        }

        //函数职责：显示巢内物品、未受精状态或受精卵的孵化信息。
        public override string GetInspectString()
        {
            StringBuilder builder = new StringBuilder(base.GetInspectString());
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            if (Empty)
            {
                builder.Append("NingshaRace_HatchNestEmpty".Translate());
                return builder.ToString();
            }

            builder.AppendLine("NingshaRace_HatchNestContents".Translate(ContainedEgg.LabelCap));
            CompNingshaEmbryo embryo = ContainedEgg.TryGetComp<CompNingshaEmbryo>();
            builder.Append(embryo == null
                ? "NingshaRace_UnfertilizedEggCannotHatch".Translate().ToString()
                : embryo.CompInspectStringExtra());
            return builder.ToString();
        }

        //函数职责：提供自动装填、弹出、内部物品选择和开发者孵化控制。
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            yield return new Command_Toggle
            {
                defaultLabel = "NingshaRace_HatchNestAutoLoadLabel".Translate(),
                defaultDesc = "NingshaRace_HatchNestAutoLoadDescription".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Install"),
                isActive = delegate { return allowAutoLoad; },
                toggleAction = delegate { allowAutoLoad = !allowAutoLoad; }
            };

            if (!Empty)
            {
                yield return new Command_Action
                {
                    defaultLabel = "NingshaRace_EjectEggLabel".Translate(),
                    defaultDesc = "NingshaRace_EjectEggDescription".Translate(),
                    icon = ContainedEgg.def.uiIcon,
                    action = delegate { innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near); }
                };

                Gizmo containedGizmo = SelectContainedItemGizmo(this, ContainedEgg);
                if (containedGizmo != null)
                {
                    yield return containedGizmo;
                }
            }

            if (!DebugSettings.godMode)
            {
                yield break;
            }

            Command_Action createEgg = new Command_Action
            {
                defaultLabel = "DEV：生成并装入受精卵",
                defaultDesc = "使用地图上的凝砂雌性和男性智人作为父母；找不到时创建无父母测试卵。",
                action = DebugCreateFertilizedEgg
            };
            if (!Empty)
            {
                createEgg.Disable("孵化巢已经装有凝砂卵。");
            }
            yield return createEgg;

            CompNingshaEmbryo containedEmbryo = ContainedEgg?.TryGetComp<CompNingshaEmbryo>();
            Command_Action addProgress = new Command_Action
            {
                defaultLabel = "DEV：孵化进度 +10%",
                defaultDesc = "为巢内受精凝砂卵增加百分之十孵化进度。",
                action = delegate { containedEmbryo.AddHatchProgress(0.1f); }
            };
            Command_Action hatchNow = new Command_Action
            {
                defaultLabel = "DEV：立刻孵化",
                defaultDesc = "直接调用正式破壳生成逻辑。",
                action = delegate { containedEmbryo.HatchNow(); }
            };
            if (containedEmbryo == null)
            {
                addProgress.Disable(Empty ? "孵化巢为空。" : "未受精凝砂卵无法孵化。");
                hatchNow.Disable(Empty ? "孵化巢为空。" : "未受精凝砂卵无法孵化。");
            }
            yield return addProgress;
            yield return hatchNow;
        }

        //函数职责：为开发者测试建立一枚使用地图现有父母的受精卵并直接装入当前巢。
        private void DebugCreateFertilizedEgg()
        {
            Pawn mother = Map.mapPawns.AllPawnsSpawned.FirstOrDefault(pawn => NingshaReproductionUtility.IsNingsha(pawn) && pawn.gender == Gender.Female);
            Pawn father = Map.mapPawns.AllPawnsSpawned.FirstOrDefault(pawn => pawn.RaceProps.Humanlike && pawn.gender == Gender.Male);
            Thing egg = NingshaReproductionUtility.CreateFertilizedEgg(mother, father, mother?.Faction ?? Faction.OfPlayer);
            if (egg == null || TryAcceptEgg(egg))
            {
                return;
            }
            egg.Destroy();
            Log.Error("[NingshaRace] 开发者受精卵无法装入凝砂孵化巢。");
        }
    }
}
