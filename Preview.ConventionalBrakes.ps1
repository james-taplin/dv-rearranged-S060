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
$env:S060_BRAKE_NATIVE = Join-Path $config.QAPath 'conventional-brakes-native.json'
$env:S060_BRAKE_OUTPUT = $OutputDirectory
$env:S060_BRAKE_BASELINE = if ($Baseline) { '1' } else { '0' }
if (-not (Test-Path -LiteralPath $env:S060_BRAKE_NATIVE)) {
    & $config.Python -B (Join-Path $PSScriptRoot 'Meyer/tools/Prepare.ConventionalBrakeEvidence.py') --game $config.GamePath --output $env:S060_BRAKE_NATIVE
    if ($LASTEXITCODE -ne 0) { throw 'Native brake evidence export failed.' }
}
foreach ($name in @('ConventionalBrakeNativeAudit.cs', 'ConventionalNativeAudit.cs', 'SteamMeshContact.cs')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "Meyer/Authoring/$name") -Destination (Join-Path $project 'Assets/Editor') -Force
}
$process = Start-Process $config.Unity -ArgumentList @('-batchmode', '-projectPath', ('"' + $project + '"'), '-executeMethod', 'ConventionalBrakeNativeAudit.RunBatch', '-logFile', ('"' + (Join-Path $OutputDirectory 'unity.log') + '"')) -WindowStyle Hidden -PassThru
Write-Output "Rendering and validating conventional brakes: $OutputDirectory"
if (-not $process.WaitForExit(180000)) { $process.Kill(); throw "Native brake fixture timed out: $OutputDirectory" }
if ($process.ExitCode -ne 0) { throw "Native brake fixture failed: $OutputDirectory" }
$report = Get-Content (Join-Path $OutputDirectory 'validation.json') -Raw | ConvertFrom-Json
if (-not $report.passed -or $report.runtimeSha256 -ne (Get-FileHash $RuntimePath).Hash) { throw 'Failed or mismatched native brake evidence.' }
Write-Output "Conventional brakes: $($report.checks) checks passed. Gameplay not tested."
