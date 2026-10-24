# 凝砂界面资源源码

`SourceAssets/UI/Assets` 保存 UI 的 Shader、包含文件与各资源的 `.meta`，便于随模组源码一起维护。它不是独立 Unity 工程，也不是 RimWorld 运行时加载目录。

## 文件映射

`SourceAssets/UI/Assets` 的相对结构与外部 Unity 工程 `E:/mygame/NingshaRace/Assets` 一致：

- `NingshaUI/Shaders/WeatheredSandstone.shader`：静态砂岩底纹。
- `NingshaUI/Shaders/DriftingSand.shader`：保留底纹的稀薄流沙。
- `NingshaUI/Shaders/SandstormField.cginc`：风沙噪声与颗粒取样。

## 维护与构建

旧 AssetBundle 构建器与批处理入口已移除。当前 1.7 使用 `1.7/Data/RimUnity` 中的标准资源和清单，C# 编译不会重新生成 Shader。

后续修改时，保留源文件及 `.meta`，使用外部 RimUnity 标准工具更新运行资源；源码副本不会自动覆盖外部 Unity 工程。具体资源约定见 [1.7 明文特效与 GLSL](../开发/1.7明文特效与GLSL.md)。

界面通过 CL 获取 Shader 并自行管理材质，不添加无消费者的材质 Def。界面设计见 [古砂岩 UI 架构](../界面/凝砂古砂岩UI架构.md)，验收步骤见 [UI 验收须知](../界面/凝砂古砂岩UI验收须知.md)。
