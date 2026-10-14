# NingshaRace 构建说明

## 文件职责
本文件记录当前 RimWorld 模组脚手架的本地编译和部署入口，方便后续继续开发。

## 编译

### RimWorld 1.7

在 PowerShell 中运行：

```powershell
& 'E:\RimModDev\NingshaRace\NingshaRace\Tools\Build\Build-1.7.ps1'
```

编译目标是本机 `1.7.4977 rev1248`、.NET 10、CL 1.7 和 RimUnity 1.7。
主程序集输出到 `NingshaRace/1.7/Assemblies`，FA 扩展输出到 `NingshaRace/1.7/Compat/FacialAnimation/Assemblies`。
两版共用本目录源码，通过 `RW_1_7` 处理引擎差异。1.7 专属明文资源和粒子接入代码位于 `1.7/Source/NingshaRaceLib`。构建先合入根目录贴图和语言，再同步 1.6 的 Defs、Data、语言、补丁、贴图到 1.7；同步时移除旧 AB 加载定义并改写地图模板路径。共用内容编辑时回到 1.6 的对应来源，1.7 独有资源直接编辑版本目录。

1.7 使用 RimUnity 标准导出的 Shader、纹理和预制体。`ClNativeResourceProvider` 将 RimUnity 资源桥接给现有 `ClShaderMaterial`、`ClShaderPro`，四组预制体通过 CL 的 RimUnity 兼容模块播放。资源与导出用法见 [1.7 RimUnity 资源与特效](../../../Docs/1.7明文特效与GLSL.md)。

明文资源检查入口：

```powershell
& 'E:\RimModDev\NingshaRace\NingshaRace\Tools\Build\Build-1.7-Assets.ps1'
```

此命令使用 RimUnity ShaderTool inspect 静态核对标准资源目录，不启动 Unity 或 RimWorld。重新生成资源时运行 `Tools/Build/Export-RimUnity.ps1`，该命令会后台打开源 Unity 工程，调用标准导出器并退出。

当前范围与限制：

- 主 DLL 和 FA 扩展已通过 1.7 Release 编译，零警告、零错误；未启动游戏验证。
- 沙瓶、石化砂潮、石化命中、侵蚀头部烟雾使用 RimUnity 标准预制体和粒子系统；源材质与贴图随依赖自动导出。
- 拥刀者发光部件使用 Layer 28，地图与外观编辑器都复用 RimUnity Bloom。凝砂 1.7 不编译自己的模糊、合成和缓冲实现，也不导出旧 Bloom Shader。
- 25 个导出入口均成功，四组预制体通过标准静态检查；未启动游戏，实机遮挡、色彩和预览仍待验证。
- HAR、FA 的编译引用复用 CL 的 `Source/.build/references/1.7`，只是将 Unity 类型引用重定向到 UnityStubs；它们不是完整移植的第三方运行时，不随凝砂分发。
- 正式加载时需要能够在 1.7 运行的 HAR、CL，以及启用 FA 时对应的 FA；不能把此处编译通过视为这些前置已经适配完成。
- 本地部署目标为 `E:\steam\steamapps\common\RimWorld\Mods\NingshaRace`；不改动工坊安装或启用列表。1.6 程序集和资源包继续保留。

### RimWorld 1.6

在项目根目录双击或运行：

```bat
compile_modSelf.bat
```

实际 C# 项目位于：

```text
NingshaRace\1.6\Source\NingshaRaceLib\NingshaRaceLib.csproj
```

编译输出 DLL 位于：

```text
NingshaRace\1.6\Assemblies
```

## 部署
在项目根目录双击或运行：

```bat
deploy_modSelf.bat
```

部署脚本只执行部署，不会自动编译。
