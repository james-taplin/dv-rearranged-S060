param([string]$Unity = '', [string]$FranklinBundle = '')
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
. (Join-Path $repo 'Build.Config.ps1')
$config = Get-S060BuildConfig
if (-not $Unity) { $Unity = $config.Unity }
if (-not $FranklinBundle) { $FranklinBundle = Join-Path $config.GamePath 'Mods\RearrangedS282\assets\s282_drivetrain' }
$project = Join-Path $repo 'Meyer\UnityProject'
if (-not (Test-Path -LiteralPath (Join-Path $project 'Assets\CarCreator\Bin\CCL.Types.dll'))) { throw 'Create the existing Meyer authoring project first.' }
if (-not (Test-Path -LiteralPath $FranklinBundle)) { throw 'Existing Franklin custom source bundle is missing.' }
$output = Join-Path $repo ('build\dockside\authoring-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $output -Force | Out-Null
$env:DOCKSIDE_OUTPUT = $output
$env:DOCKSIDE_FRANKLIN_BUNDLE = $FranklinBundle
$env:S060_NATIVE_PREVIEW = Join-Path $repo 'build\qa\native-preview.json'
$env:DOCKSIDE_WHEEL_COMPARISON = Join-Path $repo 'build\qa\s282-wheels-comparison.json'
$env:S060_PIPE_SOURCE = Join-Path $repo 'Meyer\AuthoringData\S282BranchPipes.json'
$sourceFolder = Join-Path $project 'Assets\DocksideSource'
New-Item -ItemType Directory -Path $sourceFolder -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repo 's282_drivetrain\ExportedProject\Assets\Prefabs\LocoS282A_AnimationDriveGear.anim') -Destination (Join-Path $sourceFolder 'FranklinSource.anim') -Force
foreach ($source in @((Join-Path $repo 'Meyer\Authoring'), (Join-Path $PSScriptRoot 'Authoring'))) {
    Get-ChildItem -LiteralPath $source -Filter '*.cs' -File | Copy-Item -Destination (Join-Path $project 'Assets\Editor') -Force
}
Copy-Item -LiteralPath (Join-Path $repo 'RearrangedS060\S060PipeGeometry.cs') -Destination (Join-Path $project 'Assets\Editor') -Force
Copy-Item -LiteralPath (Join-Path $repo 'RearrangedS060\S060SteamCalibration.cs') -Destination (Join-Path $project 'Assets\Editor') -Force
foreach ($file in @('S060SteamPipes.cs','S060BodyPipes.cs')) { Copy-Item -LiteralPath (Join-Path $repo "RearrangedS060\$file") -Destination (Join-Path $project 'Assets\Editor') -Force }
Copy-Item -LiteralPath (Join-Path $repo 'RearrangedS060\S060OilCupGeometry.cs') -Destination (Join-Path $project 'Assets\Editor') -Force
Copy-Item -LiteralPath (Join-Path $repo 'RearrangedS060\S060RodMesh.cs') -Destination (Join-Path $project 'Assets\Editor') -Force
Copy-Item -LiteralPath (Join-Path $repo 'RearrangedS060\S060MeyerCollision.cs') -Destination (Join-Path $project 'Assets\Editor') -Force
$argsUnity = @('-projectPath',('"'+$project+'"'),'-executeMethod','DocksideDraftBuilder.CreateDraft','-logFile',('"'+(Join-Path $output 'unity.log')+'"'))
$process = Start-Process -FilePath $Unity -ArgumentList $argsUnity -WindowStyle Hidden -PassThru
[ordered]@{pid=$process.Id;project=$project;output=$output;sourceFranklin=(Get-FileHash -LiteralPath $FranklinBundle | Select-Object Path,Hash);gameLaunched=$false;runtimeValidated=$false} |
    ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $output 'launch.json') -Encoding UTF8
Write-Output "Dockside authoring Unity PID $($process.Id); evidence $output"
if (-not $process.WaitForExit(180000)) { $process.Kill(); throw "Authoring editor timed out; inspect $output." }
if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath (Join-Path $output 'authoring-result.json'))) { throw "Dockside authoring failed (exit $($process.ExitCode)); inspect $output." }
Get-Content -LiteralPath (Join-Path $output 'authoring-result.json')

& (Join-Path $repo 'Complete.BundleEvidence.ps1') -Evidence $output
