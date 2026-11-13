param([switch]$SkipBuild, [string[]]$Only = @(), [string]$RuntimePath = '', [string]$DocksideBundle = '', [int]$AuditTimeoutSeconds = 180)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Build.Config.ps1')
$config = Get-S060BuildConfig
if (-not $SkipBuild) { & (Join-Path $PSScriptRoot 'Build.Runtime.ps1') }
$runtime = if ($RuntimePath) { (Resolve-Path -LiteralPath $RuntimePath).Path } else { Join-Path $PSScriptRoot 'build\stage7\RearrangedS060.dll' }
$runtimeHash = (Get-FileHash $runtime).Hash
$env:S060_AUDIT_MANAGED = Join-Path $config.GamePath 'DerailValley_Data\Managed'
$env:S060_AUDIT_MODS = Join-Path $config.GamePath 'Mods'
$output = Join-Path $PSScriptRoot ("build\radio-$($config.Version)\runtime-audits-" + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
$project = Join-Path $PSScriptRoot 'Meyer\UnityProject'
foreach ($source in @('MeyerSmoothSteamAudit.cs','MeyerRearUnderframeAudit.cs','MeyerRunningGearAudit.cs','NativeCouplingPreview.cs','DocksideSleeveNativeAudit.cs','ExhaustNativeAudit.cs','ExhaustFailureNativeAudit.cs','PipeRuntimeAudit.cs','MeyerRefillNativeAudit.cs','MeyerRearCouplingNativeAudit.cs','CockNativeAudit.cs','CleanupNativeAudit.cs','TransactionsNativeAudit.cs','MeyerCollisionAudit.cs','ConventionalNativeAudit.cs','SteamNativePreview.cs','SteamMeshContact.cs','MeyerInwardBodyAudit.cs','MeyerInwardPhaseAudit.cs','InwardPipeRuntimeAudit.cs')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "Meyer\Authoring\$source") -Destination (Join-Path $project 'Assets\Editor') -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'RearrangedS060\S060MeyerCollision.cs') -Destination (Join-Path $project 'Assets\Editor') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'RearrangedS060\S060MeyerBodywork.cs') -Destination (Join-Path $project 'Assets\Editor') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'RearrangedS060\S060MeyerLights.cs') -Destination (Join-Path $project 'Assets\Editor') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Meyer\Authoring\MeyerPlan.cs') -Destination (Join-Path $project 'Assets\Editor') -Force
foreach ($helper in @('S060RodMesh.cs','S060MeyerLayout.cs','S060MeyerFrontMotion.cs','S060MeyerGearMounts.cs','S060MeyerFrontEquipment.cs','S060MeyerInwardPipes.cs','S060MeyerClearance.cs','S060SteamPipes.cs','S060SteamSleeve.cs','S060BodyPipes.cs','S060MeyerExhaustPipe.cs')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('RearrangedS060\'+$helper)) -Destination (Join-Path $project 'Assets\Editor') -Force }
foreach ($helper in @('S060SteamCalibration.cs','S060OilCupGeometry.cs','S060PipeGeometry.cs')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('RearrangedS060\'+$helper)) -Destination (Join-Path $project 'Assets\Editor') -Force }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Dockside\Authoring\DocksidePlan.cs') -Destination (Join-Path $project 'Assets\Editor') -Force
$contactFolder = Join-Path $project 'Assets\QA'
New-Item -ItemType Directory -Path $contactFolder -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Meyer\tools\CollisionContactPlayModeRunner.cs') -Destination $contactFolder -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Meyer\tools\QuarteringPlayModeRunner.cs') -Destination $contactFolder -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Meyer\tools\ShayEffectsPlayModeRunner.cs') -Destination $contactFolder -Force
'{"name":"RearrangedS060.ContactAudit"}' | Set-Content (Join-Path $contactFolder 'RearrangedS060.ContactAudit.asmdef') -Encoding UTF8
$env:S060_EXHAUST_AUDIT_RUNTIME = $runtime
$env:S060_EXHAUST_FAILURE_AUDIT_RUNTIME = $runtime
$env:S060_CONTACT_AUDIT_RUNTIME = $runtime
$env:S060_NATIVE_PREVIEW = Join-Path $config.QAPath 'native-preview.json'
$env:S060_DOCKSIDE_SUPPORTS = Join-Path $config.QAPath 's060-support-pivots.json'
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Meyer\Authoring\SteamBogiePoseAudit.cs') -Destination (Join-Path $project 'Assets\Editor') -Force
$env:S060_COUPLING_FIXTURE = Join-Path $config.QAPath 'meyer-rear-coupling-native.json'
$env:S060_COUPLING_BASELINE = '0'
$env:S060_DETAIL_NATIVE = Join-Path $config.QAPath 'meyer-running-gear-native.json'
if (-not $Only.Count -or $Only -contains 'meyer-rear-coupling' -or $Only -contains 'meyer-inward-pipes') {
    & $config.Python -B (Join-Path $PSScriptRoot 'Meyer\tools\Prepare.MeyerCouplingEvidence.py') --game $config.GamePath --output $env:S060_COUPLING_FIXTURE
    if ($LASTEXITCODE -ne 0) { throw 'Native rear coupling fixture export failed.' }
    & $config.Python -B (Join-Path $PSScriptRoot 'Meyer\tools\Prepare.MeyerBrakeEvidence.py') --game $config.GamePath --output $env:S060_DETAIL_NATIVE
    if ($LASTEXITCODE -ne 0) { throw 'Native running-gear fixture export failed.' }
}
$shayBundle=Get-ChildItem (Join-Path $PSScriptRoot 'build\shay') -Directory -Filter 'bundle-*' | Where-Object { Test-Path (Join-Path $_.FullName 'bundle-receipt.json') } | Sort-Object Name -Descending | Select-Object -First 1
$env:S060_SHAY_AUDIT_BUNDLE=Join-Path $shayBundle.FullName 'shay_running_gear'
$env:S060_CONVENTIONAL_NATIVE = Join-Path $config.QAPath 'conventional-native.json'
$meyerBundle=Get-ChildItem (Join-Path $PSScriptRoot 'build\meyer') -Directory -Filter 'authoring-*' | Where-Object { Test-Path (Join-Path $_.FullName 'RS060_Meyer_0440\ccl_bundle') } | Sort-Object Name -Descending | Select-Object -First 1
$env:S060_MEYER_AUDIT_BUNDLE=Join-Path $meyerBundle.FullName 'RS060_Meyer_0440\ccl_bundle'
$env:S060_DOCKSIDE_AUDIT_BUNDLE=if ($DocksideBundle) { (Resolve-Path -LiteralPath $DocksideBundle).Path } else { Join-Path $PSScriptRoot 'build\dockside\authoring-20261022-223705\RS060_Franklin_Dockside_0440\ccl_bundle' }
$entries = @(
    @{ name='dockside-sleeves'; method='DocksideSleeveNativeAudit.RunBatch'; env='S060_DOCKSIDE_SLEEVE_OUTPUT'; report='dockside-sleeve-validation.json' },
    @{ name='meyer-rear-coupling'; method='MeyerRearCouplingNativeAudit.RunBatch'; env='S060_COUPLING_OUTPUT'; report='rear-coupling-validation.json' },
    @{ name='meyer-inward-pipes'; method='MeyerSmoothSteamAudit.RunBatch'; env='S060_SMOOTH_STEAM_OUTPUT'; report='smooth-steam-validation.json' },
    @{ name='meyer-inward-body'; method='MeyerInwardBodyAudit.RunBatch'; env='S060_MEYER_INWARD_OUTPUT'; report='inward-native-cylinder-body-validation.json' },
    @{ name='meyer-inward-phase'; method='MeyerInwardPhaseAudit.RunBatch'; env='S060_MEYER_INWARD_PHASE_OUTPUT'; report='meyer-inward-phase-validation.json' },
    @{ name='exhaust'; method='ExhaustNativeAudit.RunBatch'; env='S060_EXHAUST_AUDIT_OUTPUT'; report='native-exhaust-validation.json' },
    @{ name='exhaust-failure'; method='ExhaustFailureNativeAudit.RunBatch'; env='S060_EXHAUST_FAILURE_AUDIT_OUTPUT'; report='native-exhaust-failure-validation.json' },
    @{ name='pipes'; method='PipeRuntimeAudit.RunBatch'; env='S060_PIPE_RUNTIME_AUDIT_OUTPUT'; report='pipe-runtime-validation.json' },
    @{ name='meyer-refill'; method='MeyerRefillNativeAudit.RunBatch'; env='S060_MEYER_REFILL_OUTPUT'; report='meyer-refill-validation.json' },
    @{ name='cocks'; method='CockNativeAudit.RunBatch'; env='S060_COCK_AUDIT_OUTPUT'; report='native-cock-validation.json' },
    @{ name='cleanup'; method='CleanupNativeAudit.RunBatch'; env='S060_CLEANUP_AUDIT_OUTPUT'; report='native-cleanup-validation.json' },
    @{ name='collision'; method='MeyerCollisionAudit.RunBatch'; env='S060_COLLISION_AUDIT_OUTPUT'; report='meyer-collision-validation.json' },
    @{ name='contacts'; method='CollisionContactPlayMode.RunBatch'; env='S060_CONTACT_AUDIT_OUTPUT'; report='native-contact-delivery.json' },
    @{ name='conventional'; method='ConventionalNativeAudit.RunBatch'; env='S060_CONVENTIONAL_OUTPUT'; report='conventional-validation.json' },
    @{ name='quartering'; method='QuarteringPlayMode.RunBatch'; env='S060_QUARTERING_AUDIT_OUTPUT'; report='quartering-validation.json' },
    @{ name='shay-effects'; method='ShayEffectsPlayMode.RunBatch'; env='S060_SHAY_EFFECTS_AUDIT_OUTPUT'; report='shay-effects-validation.json' }
)
$reports = @()
if ($Only.Count) {
    foreach ($name in $Only) { if ($entries.name -notcontains $name) { throw "Unknown native audit: $name" } }
    $selected = foreach ($name in $Only) { $entries | Where-Object { $_.name -eq $name } }
    $entries = @($selected)
}
foreach ($entry in $entries) {
    if ((Get-FileHash $runtime).Hash -ne $runtimeHash) { throw 'Candidate changed during native tests.' }
    $folder = Join-Path $output $entry.name
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
    [Environment]::SetEnvironmentVariable($entry.env, $folder, 'Process')
    $process = Start-Process -FilePath $config.Unity -ArgumentList @('-batchmode','-projectPath',('"'+$project+'"'),'-executeMethod',$entry.method,'-logFile',('"'+(Join-Path $folder 'unity.log')+'"')) -WindowStyle Hidden -PassThru
    Write-Output "Testing $($entry.name); evidence $folder"
    if (-not $process.WaitForExit($AuditTimeoutSeconds * 1000)) { $process.Kill(); throw "Authoring audit timed out: $folder" }
    $path = Join-Path $folder $entry.report
    if ($process.ExitCode -ne 0 -or -not (Test-Path $path)) { throw "Native audit failed: $folder" }
    $report = Get-Content $path -Raw | ConvertFrom-Json
    if (-not $report.passed) { throw "Native audit assertions failed: $folder" }
    if ((Get-FileHash $runtime).Hash -ne $runtimeHash -or ($report.runtimeSha256 -and $report.runtimeSha256 -ne $runtimeHash)) { throw 'Native report/candidate mismatch.' }
    $reports += [ordered]@{ name=$entry.name; path=$path; hash=(Get-FileHash $path).Hash; checks=$report.checks }
    Write-Output "$($entry.name): $($report.checks) checks passed"
}
[ordered]@{ passed=$true; runtimeSha256=$runtimeHash; reports=$reports; gameScene=$false; audiblePlaybackValidated=$false } |
    ConvertTo-Json -Depth 5 | Set-Content (Join-Path $output 'runtime-validation.json') -Encoding UTF8
Write-Output "Native audit suite passed. Use -NativeEvidence '$output' in Build.Radio.ps1."
