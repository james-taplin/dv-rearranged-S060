param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Build.Config.ps1')
$config = Get-S060BuildConfig
if (-not $SkipBuild) { & (Join-Path $PSScriptRoot 'Build.Runtime.ps1') }
$runtime = Join-Path $PSScriptRoot 'build\stage7\RearrangedS060.dll'
$runtimeHash = (Get-FileHash $runtime).Hash
$env:S060_AUDIT_MANAGED = Join-Path $config.GamePath 'DerailValley_Data\Managed'
$env:S060_AUDIT_MODS = Join-Path $config.GamePath 'Mods'
$output = Join-Path $PSScriptRoot ("build\radio-$($config.Version)\runtime-audits-" + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
$project = Join-Path $PSScriptRoot 'Meyer\UnityProject'
foreach ($source in @('ExhaustNativeAudit.cs','CockNativeAudit.cs','CleanupNativeAudit.cs','TransactionsNativeAudit.cs','MeyerCollisionAudit.cs','ConventionalNativeAudit.cs','SteamNativePreview.cs','SteamMeshContact.cs')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "Meyer\Authoring\$source") -Destination (Join-Path $project 'Assets\Editor') -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'RearrangedS060\S060MeyerCollision.cs') -Destination (Join-Path $project 'Assets\Editor') -Force
$contactFolder = Join-Path $project 'Assets\QA'
New-Item -ItemType Directory -Path $contactFolder -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Meyer\tools\CollisionContactPlayModeRunner.cs') -Destination $contactFolder -Force
'{"name":"RearrangedS060.ContactAudit"}' | Set-Content (Join-Path $contactFolder 'RearrangedS060.ContactAudit.asmdef') -Encoding UTF8
$env:S060_EXHAUST_AUDIT_RUNTIME = $runtime
$env:S060_CONTACT_AUDIT_RUNTIME = $runtime
$env:S060_NATIVE_PREVIEW = Join-Path $config.QAPath 'native-preview.json'
$env:S060_CONVENTIONAL_NATIVE = Join-Path $config.QAPath 'conventional-native.json'
$entries = @(
    @{ name='exhaust'; method='ExhaustNativeAudit.RunBatch'; env='S060_EXHAUST_AUDIT_OUTPUT'; report='native-exhaust-validation.json' },
    @{ name='cocks'; method='CockNativeAudit.RunBatch'; env='S060_COCK_AUDIT_OUTPUT'; report='native-cock-validation.json' },
    @{ name='cleanup'; method='CleanupNativeAudit.RunBatch'; env='S060_CLEANUP_AUDIT_OUTPUT'; report='native-cleanup-validation.json' },
    @{ name='collision'; method='MeyerCollisionAudit.RunBatch'; env='S060_COLLISION_AUDIT_OUTPUT'; report='meyer-collision-validation.json' },
    @{ name='contacts'; method='CollisionContactPlayMode.RunBatch'; env='S060_CONTACT_AUDIT_OUTPUT'; report='native-contact-delivery.json' },
    @{ name='conventional'; method='ConventionalNativeAudit.RunBatch'; env='S060_CONVENTIONAL_OUTPUT'; report='conventional-validation.json' }
)
$reports = @()
foreach ($entry in $entries) {
    if ((Get-FileHash $runtime).Hash -ne $runtimeHash) { throw 'Candidate changed during native tests.' }
    $folder = Join-Path $output $entry.name
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
    [Environment]::SetEnvironmentVariable($entry.env, $folder, 'Process')
    $process = Start-Process -FilePath $config.Unity -ArgumentList @('-projectPath',('"'+$project+'"'),'-executeMethod',$entry.method,'-logFile',('"'+(Join-Path $folder 'unity.log')+'"')) -WindowStyle Hidden -PassThru
    Write-Output "Testing $($entry.name); evidence $folder"
    if (-not $process.WaitForExit(180000)) { $process.Kill(); throw "Authoring audit timed out: $folder" }
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
