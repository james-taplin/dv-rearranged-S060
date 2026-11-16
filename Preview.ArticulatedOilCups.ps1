param([Parameter(Mandatory)][string]$RuntimePath, [Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Build.Config.ps1')
$config = Get-S060BuildConfig
$project = Join-Path $PSScriptRoot 'Meyer/UnityProject'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
$env:S060_AUDIT_MANAGED = Join-Path $config.GamePath 'DerailValley_Data/Managed'
$env:S060_AUDIT_MODS = Join-Path $config.GamePath 'Mods'
$env:S060_EXHAUST_AUDIT_RUNTIME = (Resolve-Path -LiteralPath $RuntimePath).Path
$env:S060_NATIVE_PREVIEW = Join-Path $config.QAPath 'native-preview.json'
$env:S060_CONVENTIONAL_NATIVE = Join-Path $config.QAPath 'conventional-native.json'
$env:S060_CONVENTIONAL_OIL_NATIVE = Join-Path $config.QAPath 'conventional-oil-native.json'
$assetRoot = Join-Path $config.GamePath 'Mods/RearrangedS060/assets'
$env:S060_MEYER_AUDIT_BUNDLE = Join-Path $assetRoot 'Meyer/ccl_bundle'
$env:S060_DOCKSIDE_AUDIT_BUNDLE = Join-Path $assetRoot 'Dockside/ccl_bundle'
$env:S060_SHAY_AUDIT_BUNDLE = Join-Path $assetRoot 'Shay/shay_running_gear'
$env:S060_ART_OIL_OUTPUT = $OutputDirectory
foreach ($name in @('ArticulatedOilCupNativeAudit.cs','ConventionalNativeAudit.cs')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot "Meyer/Authoring/$name") -Destination (Join-Path $project 'Assets/Editor') -Force }
$process = Start-Process $config.Unity -ArgumentList @('-batchmode','-projectPath',('"' + $project + '"'),'-executeMethod','ArticulatedOilCupNativeAudit.RunBatch','-logFile',('"' + (Join-Path $OutputDirectory 'unity.log') + '"')) -WindowStyle Hidden -PassThru
Write-Output "Articulated oil-cup fixture: $OutputDirectory"
if (-not $process.WaitForExit(240000)) { $process.Kill(); throw 'Articulated oil-cup fixture timed out.' }
if ($process.ExitCode -ne 0) { throw "Articulated oil-cup fixture failed: $OutputDirectory" }
Get-Content (Join-Path $OutputDirectory 'validation.json') -Raw
