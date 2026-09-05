# 凝砂古砂岩 UI 架构

## 目标与视觉约定

将凝砂族自行绘制的 IMGUI、命令和进度信息统一为可组合工具，而不是全局更换 RimWorld 皮肤。

- **底材**：以原有静态砂岩、细沙凹凸和边缘积沙为主体，保留砂岩棕与旧铜配色，仅在上面叠加同色系的稀薄流沙。文字、图标和边框始终在背景之后绘制。
- **结构**：旧铜双边框、角部短刻线、带中心刻印的分隔线。
- **语义**：砂金表示通常状态；绿松石表示开启、选中或阈值；赭红表示警告，侵蚀紫和暗红表示高侵蚀与不可逆风险。
- **交互**：实时时间驱动悬停过渡，暂停游戏也可工作；选中状态有持续刻线；禁用控件保留解释。
- **动效真实性**：砂面高光可流动，填充比例与倒计时始终使用真实游戏数值，不模拟虚假加载进度。
- **环境动效**：稀薄沙尘在窗口、状态面板和技能按钮的整个背景上持续流动，中央也有轻微变化；悬停略微增强，凹入与禁用区域降低强度。使用与静态底材一致的颜色和非加法混合，不以浓厚色幕取代原有质感；文字加细窄暗影保持可读。
- **字体**：保留游戏字体，不另装字体；所有单行及多行区域先测量。窄控件截断并保留完整提示，状态卡可点击展开。
- **玩家文案**：使用“详情、按钮、物品、准备目的地”等直白词语，不向玩家介绍铭文、符印、模板、组件、线程等视觉或实现概念。物品和能力本身的设定名称保留；开发日志和本文中的技术入口不作混淆替换。

## 分层与组合

```text
业务状态与动作
  ├─ 侵蚀 / 蜕皮 / 沙傀适配器
  ├─ 货运 TransferableOneWay / 摆放目录
  └─ 原版行动、开关及能力命令
            ↓
UI/Gizmos · UI/Windows · 功能 UI 面板
            ↓
UI/Layout + UI/Controls
            ↓
UI/Foundation + UI/Motion + UI/Rendering
            ↓
Verse IMGUI + CL Shader 注册 + 独立 UI AssetBundle
```

所有 C# 路径相对 `1.6/Source/NingshaRaceLib`。

| 目录 | 职责 | 主要入口 |
| --- | --- | --- |
| `UI/Foundation` | 语义配色、公共间距、GUI 状态隔离 | `NingshaPalette`、`NingshaGuiScope` |
| `UI/Layout` | 顺序行、等分列、实测文字高度、主体与页脚分区 | `NingshaLayout` |
| `UI/Controls` | 石板容器、文字、按钮、输入、砂槽进度 | `NingshaFrame`、`NingshaText`、`NingshaButton`、`NingshaInput`、`NingshaProgress` |
| `UI/Motion` | 按实时时间插值悬停，回收离屏控件状态 | `NingshaUiMotion` |
| `UI/Rendering` | 经 CL 取得底材与风沙 Shader、缓存噪声与自由细沙、绘制背景及文字阴影 | `NingshaUiAssets`、`NingshaPanelGrain`、`NingshaPanelDrift`、`NingshaDriftSurface`、`NingshaStormNoise`、`NingshaSandGrainTexture`、`NingshaTextContrast`、`NingshaProgressTextures`、`NingshaProgressPainter` |
| `UI/Gizmos` | 状态石板、行动符印、开关符印、能力冷却 | `Gizmo_NingshaStatus`、`Command_Ningsha*` |
| `UI/Windows` | 窗口壳、完整铭文、不可逆确认、独立选项列表 | `NingshaWindow`、`Dialog_Ningsha*` |
| `UI/Panels` | 可直接装入窗口安全区的复合展示模块 | `NingshaGenerationPanel` |
| `UI/Models` | 界面选择项数据，不引用业务实现 | `NingshaChoice` |
| `PocketMaps/Cargo/UI` | 货运专用清单交互，不放进通用控件层 | `NingshaCargoListPanel` |

### 窗口组合方式

1. 继承 `NingshaWindow`；它关闭原版背景、内置标题和关闭按钮，避免双重边距及碰撞。
2. 在 `DoWindowContents` 使用 `new NingshaGuiScope(GameFont.Small)`；必须显式传入字体，不能使用结构体的默认构造值。
3. 用 `DrawShell` 取得标题、关闭区、说明与分隔线之后的内容区。
4. 创建 `NingshaLayout`，用 `Take` 自上而下取行，用 `Column` 分配同一行的多个控件。
5. 先用 `BodyWithFooter` 预留底部按钮，再把主体交给业务列表或复合面板。
6. 滚动区必须在 `finally` 中 `EndScrollView`；文字段落先调用 `TextHeight`，不要压缩字体行高。

### 状态石板适配方式

继承 `Gizmo_NingshaStatus`，只提供 `Title`、`Value`、`Detail`、`Help`、`Fraction`，可覆盖语义颜色与阈值。不在绘制函数中推进游戏状态。当前石板沿用 `180×75`；摘要空间不够时移入提示，点击展开的是当时的状态记录。

### 命令接入方式

- C# 主动构造的凝砂行动、开关分别使用 `Command_NingshaAction` 和 `Command_NingshaToggle`；原变量可继续声明为原版基类。
- 能力 Def 使用 `<gizmoClass>NingshaRaceLib.UI.Gizmos.Command_NingshaAbility</gizmoClass>`。六个现有 AbilityDef 均已接入。
- 命令宽度为 80，高度仍为 75；`NingshaCommandLayout` 按实际行高预留底部名称，其余空间留给图标，不再为顶部角落提示预留整行。缩小命令保留原版缩小入口。快捷键、右键命令、分组、教学权限和动作派发仍沿用原版命令核心。
- `LabelCap` 仅在内部绘制时隐藏，正常查询仍返回完整标题；不要为了自绘背景全程返回空标题。
- 能力保留原版施法与队列许可过滤；冷却用真实 tick 绘制三像素细条，悬停且空间允许时显示时间标签，完整悬停提示始终保留剩余时间，平时不遮挡图标。
- `NingshaCommandIcon` 保留命令比例、角度、偏移和灰度材质，并在图标区域内裁剪绘制。当前五张技能图片均为满幅 300×300，直接增大绘制区域即可，不进行额外裁切。蜕皮图片的有效内容边界为横向 13–284、纵向 50–275；绘制时在该范围外留四像素安全边并调整纹理坐标，不修改 PNG。以后更换蜕皮图片时应同步复核此范围。

### 面板沙粒

`NingshaFrame.Panel` 依次绘制砂岩底材、静态细沙与积沙、稀薄的全幅流沙，最后绘制边框和内容。所有面板均调用 `NingshaPanelGrain` 保留原有静态沉积感：椭圆细沙带有暖色亮面、暗面和更细的微尘，四周积沙在16像素内平滑淡出。短边不足56的紧凑控件省略静态积沙边带。

静态颗粒按界面坐标平铺，不随面板尺寸拉伸；双线性采样适应界面缩放。三张缓存纹理为128×128、128×16和16×128，像素数据共80 KiB（不含引擎对象开销）；首次重绘生成并释放 CPU 像素副本。每个面板最多增加五次静态纹理绘制，短控件一次，不逐帧生成颗粒。颗粒使用固定散列，不消耗游戏随机数，不需要重建资源包。绘制恢复调用方颜色，游戏切换时统一释放缓存。

### 全幅风沙背景

`NingshaPanelDrift` 把一张完整的动态风沙画面铺到内侧整个矩形，覆盖中央、四角和边缘，不限制为上下窄带。窗口、180×75状态面板、80×75技能按钮及小按钮均常驻绘制；普通强度0.88，悬停时增强至1.0，凹入区域0.46，禁用状态再乘0.6。风沙画面按界面像素平铺，左上角为取样锚点，拖动和拉大窗口不会拉伸或重排颗粒。原有边框、文字、数值、图标和命中矩形不随风沙运动。

`NingshaStormNoise` 首次使用时生成256×256线性 RGBA32噪声，四通道分别对应4、8、16、32格周期噪声。格点散列与平滑插值保证跨边界连续，不使用游戏随机数，上传后释放 CPU 可读副本。`SandstormField.cginc` 中的 `StormWarp` 用不同速度的噪声扭曲取样坐标；`StormDensity` 组合大团沙尘与细碎侵蚀场，持续表现聚拢、翻卷、破碎和消散。

`NingshaSandGrainTexture` 一次性把23000颗细沙与3500颗稍大沙粒自由散布在512×512纹理的独立通道中，不再按“每格一粒”布置。每颗沙粒在整张纹理内独立选取位置、半径、长短轴、角度与强度，允许自然重叠和疏密差异。细沙基础半径0.28至0.70像素，较大沙粒0.60至1.15像素，额外0.35像素取样支撑与柔边衰减用于亚像素显示；蓝通道提供微尘明暗。跨边界的颗粒回绕到另一侧，固定散列不消耗游戏随机数。

`GrainField` 对细沙和较大颗粒使用不同速度、涡流扰动和错向坐标，基本风速约30界面像素每秒，细沙层乘1.35，较大颗粒乘0.83。细沙是主要细节，较大颗粒仅少量参与；分布固定但随风连续取样，不逐帧重排成闪烁白噪点。流沙底色与原有 `WeatheredSandstone` 一致，为 `(0.13, 0.105, 0.075)`；沙尘色沿用沉积纹的 `(0.23, 0.18, 0.115)`；受光细沙沿用静态颗粒的 `(0.83, 0.69, 0.46)`。不更改静态底材、旧铜边或文字配色，也不叠乘全幅压暗系数。

流沙不透明度为 `0.02 + density * 0.16 + grainLight * 0.10`，两项输入均在0至1之间，输出范围为0.02至0.28，再乘面板强度。即使悬停且沙尘最浓，静态底纹也保留至少72%的混合权重；稀疏处近乎透明。浓度仍由连续噪声控制，颗粒明暗与位置算法不变，不以铺满面板的高不透明度色幕制造风沙。

`NingshaDriftSurface` 经 CL 取得 `Ningsha/UI/DriftingSand`，将噪声绑定到 `_NoiseTex`、细沙绑定到 `_GrainTex`，共用一份材质和512×512、无深度与 mipmap 的 ARGB32 RenderTexture。背景像素数据1 MiB，噪声256 KiB，细沙1 MiB，总计2.25 MiB，不含引擎开销和既有静态底材；数据纹理上传后均释放 CPU 可读副本。以 `Time.frameCount` 限制每帧最多一次 `Graphics.Blit`，没有面板请求时不更新；每个面板只增加一次标准 GUI 纹理绘制。C# 只在创建细沙纹理时遍历颗粒，不逐帧分配贴图或粒子对象。未进行运行时帧率测量，不声称零开销。

着色器输出风沙颜色和未预乘透明度，标准 GUI 只调节整板强度；横纵向均可重复采样，涡流、噪声和沙粒分布同样遵守周期边界。实时时间由 `_FlowTime` 显式传入，暂停时仍移动，游戏倍速不影响速度。绘制保留原版滚动裁剪，不把自定义材质直接送入界面；`GUI.color` 和 `RenderTexture.active` 均由 `finally` 恢复，背景、噪声、细沙与材质由 `NingshaGraphicsLifecycle` 经 `NingshaPanelDrift.Reset` 一并回收。Shader 缺失、不受支持或纹理创建失败直接报错，不用静止图片掩盖资源问题。

`NingshaTextContrast` 为单行文字和说明段落补一像素暗影，使用原矩形、字号、对齐与换行设置，不增加布局高度。阴影和正文在原文字矩形内裁剪，各类界面事件维持一致的调用序列，颜色与裁剪通过 `finally` 恢复；每段文字增加一次标签绘制，不在整片背景上覆盖黑色阅读框。

### 进度条细节

`NingshaProgress` 负责真实比例、数值测量和提示；`NingshaProgressPainter` 负责柔边凹槽、砂面渐变、低对比颗粒、渐隐流光、前缘光晕及稀疏刻度。数值有独立的半透明深色底，关键阈值两端的标记在数值之后绘制，避免完全被底板遮挡。

`NingshaProgressTextures` 按需创建四张共用纹理并上传后释放像素副本，总像素数据约 53 KiB（不含引擎对象开销）。不引入新的 Shader 或资源包，不做逐帧纹理生成，也不读取或改变游戏随机数。全部装饰绘制只在重绘事件发生；`NingshaGraphicsLifecycle` 负责切换游戏时释放缓存。

高光使用实时时间，暂停时仍可缓慢移动；移动范围通过几何交集限制在已完成部分。零进度不显示填充，满进度不继续横扫；不对业务比例作虚假推进。生成百分比向下取整，未完成时不提前显示 100%。

## 覆盖清单

| 原界面 | 当前实现与交互 |
| --- | --- |
| 侵蚀值 | 侵蚀阶段颜色、危险阈值刻线、点击展开转化规则 |
| 蜕皮营养 | 营养砂槽、60 营养保命刻线、层数摘要和完整说明 |
| 沙傀寿命 | 汇聚与稳定阶段摘要、实际剩余时间、点击展开 |
| 凝砂 C# 行动与开关 | 祭坛、蜕皮、沙傀收回、格挡、货运、蚁巢、孵化与繁殖调试命令全部采用符印组件 |
| 六个凝砂能力 | 统一符印、真实冷却砂槽、计时、多选及缩小模式 |
| 货运窗口 | 动物/物资页签，搜索，数量输入，加减、全部、清空，选择统计，确认前检查全部页签输入 |
| 开发者摆放窗口 | 中文/DefName/分类检索，分类折叠，当前选中条目标识，原摆放工具 |
| 地图生成窗口 | 真实阶段和百分比，砂槽高光，可展开说明；仍禁止生成中关闭或保存 |
| 任务页拒绝按钮 | 统一警示按钮；仅当前未接受祭坛任务显示，保留原位置与时间信息避让 |
| 拒绝指引、侵蚀过载确认 | 警示铭文、完整滚动文本、确认/取消键、一次性业务派发 |
| 祭坛独立调试选择菜单 | 可滚动铭刻选项窗，原任务生成回调 |

原版地图右键菜单里的凝砂选项、原版检查面板字符串、施法鼠标上的禁止图标和基础建筑命令仍由游戏自身绘制。这些是与其他模组混用的标准入口，不做全局 Harmony 换肤；其中凝砂自行绘制的窗口和按钮已全部接入上述组件。

## Shader 与构建

Unity 工程：`E:/mygame/NingshaRace`，匹配编辑器 `2022.3.35f1c1`。

可随仓库维护的 UI 源码位于 `SourceAssets/UI/Assets`，保留同名 Unity 工程路径与资源 `.meta`；包含下列两个 Shader、包含文件和构建器，不包含 Unity 缓存或第三方包。同步与异机构建说明见 `SourceAssets/UI/README.md`。外部工程中的源码、仓库副本和构建产物需一起维护。

- 底材 Shader：`Assets/NingshaUI/Shaders/WeatheredSandstone.shader`
- 风沙 Shader：`Assets/NingshaUI/Shaders/DriftingSand.shader`
- 风沙噪声与颗粒函数：`Assets/NingshaUI/Shaders/SandstormField.cginc`，编译时包含，不是单独加载的资源。
- 构建器：`Assets/Editor/NingshaUI/NingshaUiBundleBuilder.cs`
- 注册：模组 `1.6/Defs/UI/NingshaRace_UiAssets.xml`
- 输出：`1.6/AssetBundles/ningsha_ui.ab`、`ningsha_ui_mac.ab`
- 包内资源：上述两个 Shader，无外部依赖；构建器显式列出两项，并在平台构建前后检查着色器错误。
- CL key：包标识 `chezhou.race.ningsharace`，Shader 真名分别为 `Ningsha/UI/WeatheredSandstone` 和 `Ningsha/UI/DriftingSand`。

`NingshaRace_Enable_UnityAssets` 已存在，独立 UI 包由 CL 声明加载，不复用或重写人物特效包。

首次界面绘制时，底材 Shader 在256×256 RenderTexture 中生成静止衬底；全幅风沙另用512×512共享画面按需更新，噪声由 C# 一次性生成，两者最终都通过标准 GUI 纹理函数绘制。`NingshaGraphicsLifecycle` 在游戏切换时释放底纹、风沙画面与材质、噪声、面板颗粒、进度纹理和悬停缓存。Shader、包含文件或构建清单变更必须重建 UI 包，不能仅替换 DLL。界面自行持有材质，不添加没有消费者的 `ClShaderMaterial` 或 `ClShaderPro` Def。

动画使用框架自身的实时时间插值，没有引入 DOTween DLL 或修改 ChezhouLib。用户允许使用 CL 的 DOTween，但当前悬停动效不需要额外补间生命周期。

复现构建：

```powershell
& 'E:/RimModDev/NingshaRace/NingshaRace/Tools/UI/Build-NingshaUi.ps1'
& 'E:/VS/MSBuild/Current/Bin/MSBuild.exe' 'E:/RimModDev/NingshaRace/NingshaRace/1.6/Source/NingshaRaceLib/NingshaRaceLib.csproj' /p:Configuration=Release /nologo /verbosity:minimal
```

Unity 日志：`E:/mygame/NingshaRace/Logs/NingshaUiBundleBuild.log`。本次构建成功输出双平台包，两个包的清单均包含底材与流沙 Shader，无外部依赖，无 UI Shader 编译错误，批处理退出码为0。Unity 启动阶段仍有许可客户端诊断，完整日志保留，不修改无关工具配置来隐藏诊断。C# Release 编译通过，现有 Harmony 引用的 MSIL/AMD64 架构警告仍保留。

## 验证边界

已进行源码覆盖核对、原版 API 核对、Shader/AssetBundle 编译、Release 编译及文本静态检查。遵守项目约束，没有启动 RimWorld、没有搭建测试沙盒或执行游戏运行测试。视觉、鼠标与键盘实际验收步骤见 [古砂岩 UI 验收须知](凝砂古砂岩UI验收须知.md)，不把“编译通过”写成“游戏视觉验收通过”。

本轮不迁移存档，不改营养、侵蚀、寿命、任务奖励或地图生成数值；保留之前已存在的修复工作区改动。
