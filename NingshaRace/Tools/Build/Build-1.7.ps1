param(
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
if (!(Test-Path -LiteralPath (Join-Path $versionRoot 'Data\RimUnity\catalog.json'))) { throw '缺少标准 RimUnity 导出资源，请先运行 Export-RimUnity.ps1。' }
#共享源码分别编译，游戏与框架 DLL 均不复制到凝砂输出。
& dotnet build (Join-Path $versionRoot 'Source\NingshaRace.FacialAnimation\NingshaRace.FacialAnimation.csproj') -c Release --nologo "-p:ChezhouLibDir=$ChezhouLibRoot" "-p:RimUnityDir=$RimUnityRoot"
if ($LASTEXITCODE -ne 0) { throw '凝砂 1.7 编译失败。' }
#发布一份独立版本内容，避免 1.7 加载路径带入 1.6 的程序集。
#根目录的贴图和语言先合入，1.7 无需挂载含旧 AB 的根内容目录。
foreach ($folder in @('Languages', 'Textures')) {
    $source = Join-Path $modRoot $folder
    if (!(Test-Path -LiteralPath $source)) { continue }
    $destination = Join-Path $versionRoot $folder
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Copy-Item -Path (Join-Path $source '*') -Destination $destination -Recurse -Force
}
foreach ($folder in @('Data', 'Defs', 'Languages', 'Patches', 'Textures')) {
    $source = Join-Path $modRoot "1.6\$folder"
    if (!(Test-Path -LiteralPath $source)) { continue }
    $destination = Join-Path $versionRoot $folder
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Copy-Item -Path (Join-Path $source '*') -Destination $destination -Recurse -Force
}
#1.7 保留材质模板和业务 Def，移除旧版的 AB 资源加载定义。
$utf8 = [Text.UTF8Encoding]::new($false)
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $modRoot '1.6\Defs') -Filter '*.xml' -Recurse) {
    $destinationFile = Join-Path (Join-Path $versionRoot 'Defs') ([IO.Path]::GetRelativePath((Join-Path $modRoot '1.6\Defs'), $file.FullName))
    $sourceText = [IO.File]::ReadAllText($destinationFile)
    $result = [regex]::Replace($sourceText,
        '(?ms)^[ \t]*<ChezhouLib\.LibDef\.(UnityShaderLord|ClAssetLord)(?:\s[^>]*)?>.*?</ChezhouLib\.LibDef\.\1>[ \t]*\r?\n?', '')
    $result = $result.Replace('<dataPath>1.6/Data/', '<dataPath>1.7/Data/')
    if ($result -ne $sourceText) { [IO.File]::WriteAllText($destinationFile, $result, $utf8) }
}
$faSource = Join-Path $modRoot 'Compat\FacialAnimation'
Get-ChildItem -LiteralPath $faSource -Directory | Where-Object Name -ne Assemblies | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $versionRoot 'Compat\FacialAnimation') -Recurse -Force
}
Write-Host '凝砂 1.7 程序集与内容已生成；预制体和 Shader 使用 Data/RimUnity 标准清单。'
