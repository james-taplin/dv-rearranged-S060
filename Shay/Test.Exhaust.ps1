param([Parameter(Mandatory)][string]$Bundle, [string]$Runtime = '')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
. (Join-Path $repo 'Build.Config.ps1')
$config=Get-S060BuildConfig
$project=Join-Path $repo 'Meyer\UnityProject'
Copy-Item (Join-Path $PSScriptRoot 'Authoring\ShayExhaustNativeAudit.cs') (Join-Path $project 'Assets\Editor') -Force
Copy-Item (Join-Path $repo 'Meyer\Authoring\ExhaustNativeAudit.cs') (Join-Path $project 'Assets\Editor') -Force
$env:S060_AUDIT_MANAGED=Join-Path $config.GamePath 'DerailValley_Data\Managed'
$env:S060_AUDIT_MODS=Join-Path $config.GamePath 'Mods'
$env:S060_EXHAUST_AUDIT_RUNTIME=if($Runtime){(Resolve-Path $Runtime).Path}else{Join-Path $repo 'build\stage7\RearrangedS060.dll'}
$env:S060_SHAY_EXHAUST_BUNDLE=(Resolve-Path -LiteralPath $Bundle).Path
$output=Join-Path $repo ('build\shay\exhaust-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $output -Force | Out-Null
$env:S060_SHAY_EXHAUST_OUTPUT=$output
$process=Start-Process $config.Unity -ArgumentList @('-projectPath',('"'+$project+'"'),'-executeMethod','ShayExhaustNativeAudit.RunBatch','-logFile',('"'+(Join-Path $output 'unity.log')+'"')) -WindowStyle Hidden -PassThru
Write-Output "Testing native Shay exhaust: $output"
if(-not $process.WaitForExit(180000)){$process.Kill();throw 'Shay exhaust fixture timed out.'}
$path=Join-Path $output 'shay-exhaust-validation.json'
if($process.ExitCode -ne 0 -or -not(Test-Path $path)){throw "Shay exhaust fixture failed: $output"}
$report=Get-Content -LiteralPath $path -Raw|ConvertFrom-Json
if(-not $report.passed -or $report.runtimeSha256 -ne (Get-FileHash $env:S060_EXHAUST_AUDIT_RUNTIME).Hash){throw 'Shay exhaust evidence/runtime mismatch.'}
Write-Output "Shay native exhaust: $($report.checks) checks passed. Audible acceptance remains James's."
