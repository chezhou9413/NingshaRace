#职责：以批处理编译凝砂界面 Shader 资源包，保留完整 Unity 日志且不启动 RimWorld。
param(
    [string]$UnityEditor = 'E:\UN\2022.3.35f1\Editor\Unity.exe',
    [string]$UnityProject = 'E:\mygame\NingshaRace'
)

$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $UnityEditor -PathType Leaf)) { throw "Unity 编译器不存在：$UnityEditor" }
if (!(Test-Path -LiteralPath (Join-Path $UnityProject 'ProjectSettings\ProjectVersion.txt'))) { throw "Unity 工程无效：$UnityProject" }
$buildLog = Join-Path $UnityProject 'Logs\NingshaUiBundleBuild.log'
$buildArguments = '-batchmode -nographics -quit -projectPath "' + $UnityProject + '" -executeMethod NingshaUiBundleBuilder.Build -logFile "' + $buildLog + '"'
$buildProcess = Start-Process -FilePath $UnityEditor -ArgumentList $buildArguments -WindowStyle Hidden -PassThru -Wait
if ($buildProcess.ExitCode -ne 0) { throw "界面资源编译失败，退出码 $($buildProcess.ExitCode)，日志：$buildLog" }
Write-Output "界面资源编译完成。日志：$buildLog"
