#使用 RimUnity 自带检查器核对标准资源目录、预制体类型与成员引用。
param(
    [string]$RimUnityRoot = 'E:\RimModDev\RimUnity',
    [string]$GameRoot = 'E:\steam\steamapps\common\RimWorld'
)
$ErrorActionPreference = 'Stop'
$resourceRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\1.7\Data\RimUnity'))
$inspector = Join-Path $RimUnityRoot 'Tools\bin\ShaderTool\RimUnity.ShaderTool.exe'
$runtime = Join-Path $RimUnityRoot '1.7\Assemblies\RimUnity.dll'
& $inspector $GameRoot inspect $runtime $resourceRoot
if ($LASTEXITCODE -ne 0) { throw 'RimUnity 标准资源检查失败。' }
Write-Host '凝砂 RimUnity 标准资源静态检查通过。'
