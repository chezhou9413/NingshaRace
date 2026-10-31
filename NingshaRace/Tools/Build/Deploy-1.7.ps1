param(
    [string]$SteamMod = 'E:\steam\steamapps\common\RimWorld\Mods\NingshaRace',
    [string]$DeploymentAssembly = 'E:\aow\ModPack\xmlremove\bin\Release\net8.0-windows\xmlremove.dll'
)
$ErrorActionPreference = 'Stop'
if (Get-Process RimWorld,RimWorldWin64 -ErrorAction SilentlyContinue) { throw '请关闭 RimWorld 后再部署。' }
$modRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$SteamMod = [IO.Path]::GetFullPath($SteamMod)
if (!(Test-Path -LiteralPath (Join-Path $SteamMod 'About\About.xml'))) { throw "本地模组不存在：$SteamMod" }
$targetVersion = [IO.Path]::GetFullPath((Join-Path $SteamMod '1.7'))
if ($targetVersion -ne "$SteamMod\1.7") { throw '1.7 部署目标越界。' }
$workRoot = 'E:\ModdevMics\Outputs\NingshaRace\Deploy-1.7'
$stage = Join-Path $workRoot 'Source\NingshaRace'
#固定中转路径经过绝对路径核对，清理只限本次部署副本。
if ([IO.Path]::GetFullPath($stage) -ne 'E:\ModdevMics\Outputs\NingshaRace\Deploy-1.7\Source\NingshaRace') { throw '部署中转路径越界。' }
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage -Force | Out-Null
$deployAssembly = [Reflection.Assembly]::LoadFrom($DeploymentAssembly)
#现有部署服务尚未列出原生 Shader 资源，只补齐本进程的运行目录白名单。
$filterType = $deployAssembly.GetType('RuntimeContentFilter', $true)
$flags = [Reflection.BindingFlags]'Static,NonPublic'
$filterType.GetField('ResourceFolders', $flags).GetValue($null).Add('Shaders') | Out-Null
$filter = [Activator]::CreateInstance($filterType, @($modRoot))
$allowsFile = $filterType.GetMethod('AllowsFile')
#投影当前版本的运行文件，源码、文档、压缩素材和 1.6 不进入部署源。
$files = @(Get-ChildItem -LiteralPath (Join-Path $modRoot '1.7'),(Join-Path $modRoot 'About') -Recurse -File)
$files += Get-Item -LiteralPath (Join-Path $modRoot 'LoadFolders.xml')
foreach ($file in $files) {
    $relative = [IO.Path]::GetRelativePath($modRoot, $file.FullName)
    if ($file.Extension -eq '.zip' -or !$allowsFile.Invoke($filter, @($relative))) { continue }
    $destination = Join-Path $stage $relative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
    [IO.File]::Copy($file.FullName, $destination, $true)
}
$backupRoot = Join-Path 'E:\ModdevMics\Backups\NingshaRace' (Get-Date -Format 'yyyyMMdd-HHmmss')
[IO.Directory]::CreateDirectory($backupRoot) | Out-Null
if (Test-Path -LiteralPath $targetVersion) { Copy-Item -LiteralPath $targetVersion -Destination (Join-Path $backupRoot 'Previous-1.7') -Recurse }
$runtimeRoot = Join-Path $workRoot 'Runtime'
[IO.Directory]::CreateDirectory($runtimeRoot) | Out-Null
$options = [DeployOptions]::new($stage, 'NingshaRace', $null, (Join-Path $workRoot 'Processed'), $runtimeRoot, (Join-Path $backupRoot 'Source'))
$messages = [Collections.Concurrent.ConcurrentQueue[string]]::new()
$logger = $messages.GetType().GetMethod('Enqueue').CreateDelegate([Action[string]], $messages)
[ModDeployer]::new().Deploy($options, $logger)
$processedMod = Join-Path $runtimeRoot 'NingshaRace'
#复用原服务的同步与旧文件清理，但把边界限定为 1.7，其他版本不参与。
$sync = [ModDeployer].GetMethod('SyncProcessedToSteam', [Reflection.BindingFlags]'Static,NonPublic')
$syncVersion = $sync.CreateDelegate([Action[string,string,Action[string]]])
$syncVersion.Invoke((Join-Path $processedMod '1.7'), $targetVersion, $logger)
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $processedMod 'About') -File) {
    if ($file.Name -ne 'PublishedFileId.txt') { Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $SteamMod "About\$($file.Name)") -Force }
}
Copy-Item -LiteralPath (Join-Path $processedMod 'LoadFolders.xml') -Destination (Join-Path $SteamMod 'LoadFolders.xml') -Force
#按处理后的实际产物核对，包含 Def、程序集、贴图及 GLSL。
$count = 0
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $processedMod '1.7') -Recurse -File) {
    $destination = Join-Path $targetVersion ([IO.Path]::GetRelativePath((Join-Path $processedMod '1.7'), $file.FullName))
    if (!(Test-Path -LiteralPath $destination) -or (Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $destination).Hash) { throw "部署产物不一致：$destination" }
    $count++
}
$messages.Enqueue("已校验 $count 个 1.7 文件。")
[IO.File]::WriteAllLines((Join-Path $workRoot 'deploy.log'), $messages.ToArray(), [Text.UTF8Encoding]::new($false))
Write-Host "1.7 已部署：$targetVersion；校验 $count 个文件。日志：$workRoot\deploy.log"
