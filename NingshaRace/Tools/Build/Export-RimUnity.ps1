param(
    [string]$UnityProject = 'E:\mygame\NingshaRace',
    [string]$UnityEditor = 'E:\UN\2022.3.35f1\Editor\Unity.exe',
    [string]$RimUnityRoot = 'E:\RimModDev\RimUnity'
)
$ErrorActionPreference = 'Stop'
if (Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -like "*$UnityProject*" -or $_.CommandLine -like "*$($UnityProject.Replace('\','/'))*" }) {
    throw '凝砂 Unity 工程已打开，请在编辑器执行 NingshaRimUnityExport.Export，或关闭后使用此批处理。'
}
& dotnet build (Join-Path $RimUnityRoot 'Source\RimUnity.ShaderTool\RimUnity.ShaderTool.csproj') -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'RimUnity ShaderTool 编译失败。' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'NingshaRimUnityExport.cs') -Destination (Join-Path $UnityProject 'Assets\Editor\NingshaRimUnityExport.cs') -Force
$logDirectory = 'E:\ModdevMics\Temp\NingshaRace'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$logFile = Join-Path $logDirectory 'rimunity-export.log'
#压缩贴图必须由真实图形设备读回，因此不能使用 -nographics。
$process = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode', '-quit', '-projectPath', ('"' + $UnityProject + '"'),
    '-executeMethod', 'NingshaRimUnityExport.Export', '-logFile', ('"' + $logFile + '"')) -WindowStyle Hidden -PassThru -Wait
if ($process.ExitCode -ne 0) { throw "RimUnity 导出失败：$logFile" }
Write-Host "RimUnity 资源导出完成：$logFile"
