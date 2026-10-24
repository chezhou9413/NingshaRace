# 凝砂地图异步时序 API

适用于 RimWorld 1.7。命名空间 `NingshaRaceLib.AsyncTime`，入口 `NingshaTime`。这是在主线程顺序调度的独立地图逻辑时钟，不创建并行模拟线程。

地图默认沿用原版时序。首次调用 `SetRate`、`Pause` 或 `Resume` 后，在下一个世界 Tick 开始前接管该地图；不会自动改变地底地图速度。

```csharp
using NingshaRaceLib.AsyncTime;

//倍率相对于世界推进速度。
NingshaTime.SetRate(surfaceMap, 1f);
NingshaTime.SetRate(caveMap, 0.5f);
NingshaTime.Pause(caveMap);
NingshaTime.Resume(caveMap);

//null 代表世界时间。
int caveTicks = NingshaTime.GetTicks(caveMap);
int worldTicks = NingshaTime.GetTicks(null);

//地图外调用的技能或物体生成可以显式指定时间上下文。
NingshaTime.RunInMapContext(caveMap, () => ActivateAbility(caveMap));
```

| 入口 | 语义 |
| --- | --- |
| `SetRate(Map, float)` | 范围 0～15，零为暂停，支持小数倍率 |
| `Pause(Map)` | 暂停当地模拟，丢弃剩余小数 Tick，不积压补算 |
| `Resume(Map)` | 恢复最近的非零倍率，首次调用为 1 |
| `IsEnabled(Map)` | 包含已申请、等待接管的地图 |
| `GetRate(Map)` | 未接管地图返回 1 |
| `GetTicks(Map)` | 地图逻辑 Tick；null 返回世界 Tick |
| `GetTicksAbs(Map)` | 当地 Tick 加游戏开局绝对日期 |
| `RunInMapContext(Map, Action)` | 临时切换计时、减速器与叙事者，异常时也恢复 |
| `ThingTimeShifted` | 物体换时钟时通知，参数为物体和目标减来源的 Tick 差 |

所有入口应在游戏主线程调用。写入入口检查线程与地图有效性；不允许后台代码、异步委托或等待跨越时间上下文。世界暂停仍会暂停所有地图。每个世界 Tick 最多补算所设倍率对应的当地 Tick，加速增加实际模拟负荷。

## 接管范围

每张已接管地图保存独立 Tick、小数余量、倍率、减速器期限和当地叙事者；普通、稀有、长期物体队列与地图 PreTick/PostTick 按当地时间推进，读档重建队列。渲染保持调用，但 MapUpdate 中的时间和本帧步数来自当地时钟。

当地叙事事件和已排队地图事件使用当地时钟。外部通过 Storyteller.TryFire 触发的地图事件先进入当地队列，下一个当地 Tick 执行；返回 true 表示接收入队，不表示已实际发生。暂停地图不会靠这一入口立刻生成袭击。世界任务自身仍由全局 QuestManager 推进。

地图环境状态的开始时间和剩余时间使用目标地图时钟。生成与容器移动、人物离图会换算已覆盖的物体生成记录、人物出生日期、常用 MindState 期限与俘虏逃跑计时。生理年龄和相对持续时长不直接平移。

## 接入边界

这是一版基础 API，尚未做游戏运行验收，不能视为覆盖所有原版系统和 Mod 的完整隔离层。

- 世界任务期限、商队、运输中的世界对象继续使用世界时间；世界或其他 Mod 直接修改地图的行为不会全部被拦截。
- 原版和其他 Mod 的所有私有时间戳没有被自动扫描。自定义绝对 Tick 字段应在 `ThingTimeShifted` 回调中加上 delta，保留各自“未初始化”标记；相对倒计时不加 delta。
- GUI 等 MapUpdate 之外的查询应使用 `GetTicks(map)` 或显式上下文。基于真实时间的粒子、Shader 和外部 Update 不保证随地图暂停。
- 不提供时间控制 UI，也不提供重新并回世界时钟的接口；设置 1 倍只恢复推进倍率，不抹平已有时间差。
- 与 Multiplayer 的独立地图调度器互斥。推进出现可捕获异常时暂停对应地图并输出错误。
- 游戏更新可能改变 Harmony 挂接点，应重新核对本地源码后编译。
