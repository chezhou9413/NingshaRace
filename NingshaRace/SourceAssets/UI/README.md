# 凝砂界面资源源码

本目录保存本次 UI 资源包对应的 Shader、包含文件、Unity 编辑器构建器与各资源的 `.meta`，便于随模组源码一起维护。它不是独立 Unity 工程，也不是 RimWorld 运行时加载目录；不包含 Unity 缓存、日志、许可证信息或第三方包。

## 文件映射

本目录下 `Assets` 的相对结构与外部 Unity 工程 `E:/mygame/NingshaRace/Assets` 一致：

- `NingshaUI/Shaders/WeatheredSandstone.shader`：静态砂岩底纹。
- `NingshaUI/Shaders/DriftingSand.shader`：保留底纹的稀薄流沙。
- `NingshaUI/Shaders/SandstormField.cginc`：风沙噪声与颗粒取样。
- `Editor/NingshaUI/NingshaUiBundleBuilder.cs`：Windows、macOS 双平台 UI 包构建入口。

## 维护与构建

1. 使用与现有工程一致的 Unity `2022.3.35f1c1`；本机编辑器位于 `E:/UN/2022.3.35f1/Editor/Unity.exe`。
2. 将本目录的 `Assets` 文件按相对路径合入对应 Unity 工程，保留 `.meta`。不要复制整个 Unity 缓存目录到模组仓库。
3. 构建器的 `Output` 当前指向 `E:/RimModDev/NingshaRace/NingshaRace/1.6/AssetBundles`。在其他机器构建前，需要将其设为实际模组的 `1.6/AssetBundles` 目录；同时为 `Tools/UI/Build-NingshaUi.ps1` 提供当地编辑器与工程路径。
4. 用模组中的 `Tools/UI/Build-NingshaUi.ps1` 执行批处理资源编译。资源分别输出为 `ningsha_ui.ab` 和 `ningsha_ui_mac.ab`，每包均包含底材与流沙 Shader；包含文件随 Shader 编译，不单独加载。
5. 后续编辑 Unity 工程中的上述源码时，同步更新本目录对应文件和构建产物，再一起提交。此源码副本不自动覆盖工作中的 Unity 工程。

注册仍由 `1.6/Defs/UI/NingshaRace_UiAssets.xml` 的 `UnityShaderLord` 完成。界面通过 CL 获取 Shader 并自行管理材质，不添加无消费者的材质 Def。实际显示与验证边界见 `Docs/凝砂古砂岩UI架构.md` 和对应验收须知。
