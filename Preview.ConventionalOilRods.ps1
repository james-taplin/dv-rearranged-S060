param([Parameter(Mandatory)][string]$RuntimePath, [Parameter(Mandatory)][string]$OutputDirectory, [switch]$Baseline)
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
$env:S060_OIL_ROD_OUTPUT = $OutputDirectory
$env:S060_OIL_ROD_BASELINE = if ($Baseline) { '1' } else { '0' }
& $config.Python -B (Join-Path $PSScriptRoot 'Meyer/tools/Prepare.ConventionalOilEvidence.py') --game $config.GamePath --output $env:S060_CONVENTIONAL_OIL_NATIVE
if ($LASTEXITCODE -ne 0) { throw 'Native cup export failed.' }
foreach ($name in @('ConventionalOilRodAudit.cs','ConventionalNativeAudit.cs','ArticulatedOilCupNativeAudit.cs')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "Meyer/Authoring/$name") -Destination (Join-Path $project 'Assets/Editor') -Force
}
$process = Start-Process $config.Unity -ArgumentList @('-batchmode','-projectPath',('"' + $project + '"'),'-executeMethod','ConventionalOilRodAudit.RunBatch','-logFile',('"' + (Join-Path $OutputDirectory 'unity.log') + '"')) -WindowStyle Hidden -PassThru
Write-Output "Native oil/rod fixture: $OutputDirectory"
if (-not $process.WaitForExit(180000)) { $process.Kill(); throw 'Native oil/rod fixture timed out.' }
if ($process.ExitCode -ne 0) { throw "Native oil/rod fixture failed: $OutputDirectory" }
Get-Content (Join-Path $OutputDirectory 'validation.json') -Raw
