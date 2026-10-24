param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$ChezhouLibRoot = 'E:\RimModDev\ChezhouLib',
    [string]$RimUnityRoot = 'E:\RimModDev\RimUnity'
)
$ErrorActionPreference = 'Stop'
$modRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$versionRoot = Join-Path $modRoot '1.7'
if (Get-Process RimWorld,RimWorldWin64 -ErrorAction SilentlyContinue) { throw '游戏正在运行，请关闭后再编译写入程序集。' }
foreach ($dependency in @('1.7\Assemblies\ChezhouLib.dll', '1.7\RimUnityCompat\Assemblies\ChezhouLib.RimUnity.dll', 'Source\.build\references\1.7\AlienRace.dll', 'Source\.build\references\1.7\FacialAnimation.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $ChezhouLibRoot $dependency))) {
        throw "缺少 1.7 编译依赖：$dependency。先使用 CL 的 Prepare-WaylithReferences.ps1 和 1.7 编译入口。"
    }
}
foreach ($assembly in @('RimUnity.Core.dll', 'RimUnity.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $RimUnityRoot "1.7\Assemblies\$assembly"))) { throw "缺少 RimUnity 运行库：$assembly" }
}
if (!(Test-Path -LiteralPath (Join-Path $versionRoot 'Data\RimUnity\catalog.json'))) { throw '缺少 1.7/Data/RimUnity/catalog.json，请恢复完整的 1.7 运行资源。资源说明见 Docs/开发/1.7明文特效与GLSL.md。' }
#直接编译维护中的版本内容，不从旧目录覆盖源码或资源。
& dotnet build (Join-Path $versionRoot 'Source\NingshaRace.FacialAnimation\NingshaRace.FacialAnimation.csproj') -c $Configuration --nologo "-p:ChezhouLibDir=$ChezhouLibRoot" "-p:RimUnityDir=$RimUnityRoot"
if ($LASTEXITCODE -ne 0) { throw '凝砂 1.7 编译失败。' }
Write-Host '凝砂 1.7 主程序集与 FA 扩展已生成。'
