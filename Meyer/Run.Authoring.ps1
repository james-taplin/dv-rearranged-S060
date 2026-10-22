param([string]$Unity = '', [switch]$UpdateDraft, [switch]$ExportPrototype, [string]$FranklinBundle = '')
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
. (Join-Path $repo 'Build.Config.ps1')
$config = Get-S060BuildConfig
if (-not $Unity) { $Unity = $config.Unity }
if (-not $FranklinBundle) { $FranklinBundle = Join-Path $config.GamePath 'Mods\RearrangedS282\assets\s282_drivetrain' }
$env:DOCKSIDE_FRANKLIN_BUNDLE = $FranklinBundle
$project = Join-Path $PSScriptRoot 'UnityProject'
$output = Join-Path $repo ('build\meyer\authoring-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $output -Force | Out-Null
$env:MEYER_OUTPUT = $output
$env:MEYER_EXIT = '1'
$env:MEYER_UPDATE_DRAFT = if ($UpdateDraft) { '1' } else { '0' }
$env:MEYER_EXPORT = if ($ExportPrototype) { '1' } else { '0' }
$env:S060_NATIVE_PREVIEW = Join-Path $repo 'build\qa\native-preview.json'
$env:S060_PIPE_SOURCE = Join-Path $PSScriptRoot 'AuthoringData\S282BranchPipes.json'
Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'Authoring') -Filter '*.cs' -File |
    Copy-Item -Destination (Join-Path $project 'Assets\Editor')
Copy-Item -LiteralPath (Join-Path $repo 'RearrangedS060\S060PipeGeometry.cs') -Destination (Join-Path $project 'Assets\Editor') -Force
Copy-Item -LiteralPath (Join-Path $repo 'RearrangedS060\S060SteamCalibration.cs') -Destination (Join-Path $project 'Assets\Editor') -Force
foreach ($file in @('S060SteamPipes.cs','S060BodyPipes.cs','S060MeyerLights.cs','S060MeyerExhaustPipe.cs')) { Copy-Item -LiteralPath (Join-Path $repo "RearrangedS060\$file") -Destination (Join-Path $project 'Assets\Editor') -Force }
Copy-Item -LiteralPath (Join-Path $repo 'RearrangedS060\S060OilCupGeometry.cs') -Destination (Join-Path $project 'Assets\Editor') -Force
Copy-Item -LiteralPath (Join-Path $repo 'RearrangedS060\S060RodMesh.cs') -Destination (Join-Path $project 'Assets\Editor') -Force
Copy-Item -LiteralPath (Join-Path $repo 'RearrangedS060\S060MeyerCollision.cs') -Destination (Join-Path $project 'Assets\Editor') -Force
$argsUnity = @('-projectPath',('"'+$project+'"'),'-executeMethod','MeyerDraftBuilder.CreateDraft','-logFile',('"'+(Join-Path $output 'unity.log')+'"'))
$process = Start-Process -FilePath $Unity -ArgumentList $argsUnity -WindowStyle Hidden -PassThru
[ordered]@{pid=$process.Id;project=$project;output=$output;gameLaunched=$false;runtimeValidated=$false} |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'launch.json') -Encoding UTF8
Write-Output "Meyer authoring Unity PID $($process.Id); evidence $output"
if (-not $process.WaitForExit(180000)) {
    $process.Kill()
    throw "Meyer authoring editor timed out; inspect $output. No game files installed."
}
if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath (Join-Path $output 'authoring-result.json'))) {
    throw "Meyer authoring did not complete (exit $($process.ExitCode)); inspect $output."
}
Get-Content -LiteralPath (Join-Path $output 'authoring-result.json')

if ($ExportPrototype) { & (Join-Path $repo 'Complete.BundleEvidence.ps1') -Evidence $output }
