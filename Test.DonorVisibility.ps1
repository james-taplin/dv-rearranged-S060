param([string]$Runtime = '', [string]$Assets = '', [switch]$Baseline)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Build.Config.ps1')
$config=Get-S060BuildConfig
$project=Join-Path $PSScriptRoot 'Meyer\UnityProject'
foreach($file in @('DonorVisibilityNativeAudit.cs','ExhaustNativeAudit.cs')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('Meyer\Authoring\'+$file)) -Destination (Join-Path $project 'Assets\Editor') -Force
}
$env:S060_AUDIT_MANAGED=Join-Path $config.GamePath 'DerailValley_Data\Managed'
$env:S060_AUDIT_MODS=Join-Path $config.GamePath 'Mods'
$env:S060_DONOR_RUNTIME=if($Runtime){(Resolve-Path -LiteralPath $Runtime).Path}else{Join-Path $PSScriptRoot 'build\stage7\RearrangedS060.dll'}
$assetRoot=Join-Path $PSScriptRoot ("build\nexus-$($config.Version)-single\package\RearrangedS060")
if(-not(Test-Path -LiteralPath $assetRoot)){$assetRoot=Join-Path $PSScriptRoot 'build\nexus-0.3.3-single\package\RearrangedS060'}
$env:S060_DONOR_ASSETS=(Resolve-Path -LiteralPath $(if($Assets){$Assets}else{$assetRoot})).Path
$env:S060_DONOR_BASELINE=if($Baseline){'1'}else{'0'}
$output=Join-Path $PSScriptRoot ('build\release-0.3.4\donors-'+$(if($Baseline){'baseline-'}else{'candidate-'})+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $output -Force | Out-Null
$env:S060_DONOR_OUTPUT=$output
$process=Start-Process $config.Unity -ArgumentList @('-batchmode','-projectPath',('"'+$project+'"'),'-executeMethod','DonorVisibilityNativeAudit.RunBatch','-logFile',('"'+(Join-Path $output 'unity.log')+'"')) -WindowStyle Hidden -PassThru
Write-Output "Checking donor visibility: $output"
if(-not $process.WaitForExit(180000)){$process.Kill();throw 'Donor visibility check timed out.'}
$path=Join-Path $output 'validation.json'
if($process.ExitCode -ne 0 -or -not(Test-Path -LiteralPath $path)){throw "Donor visibility fixture failed: $output"}
$report=Get-Content -LiteralPath $path -Raw|ConvertFrom-Json
if(-not $report.passed -or $report.runtimeSha256 -ne (Get-FileHash $env:S060_DONOR_RUNTIME).Hash){throw 'Donor visibility evidence mismatch.'}
Write-Output "Donor visibility: $($report.checks) native checks passed."
