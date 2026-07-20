# NingshaRace 构建说明

## 文件职责
本文件记录当前 RimWorld 模组脚手架的本地编译和部署入口，方便后续继续开发。

## 编译
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