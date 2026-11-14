param([Parameter(Mandatory)][string]$ShayBundle, [Parameter(Mandatory)][string]$DocksideBundle)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Build.Config.ps1')
$config=Get-S060BuildConfig
$project=Join-Path $PSScriptRoot 'Meyer\UnityProject'
$runtime=Join-Path $PSScriptRoot 'build\stage7\RearrangedS060.dll'
$hash=(Get-FileHash $runtime).Hash
$output=Join-Path $PSScriptRoot ('build\integration-0.3.3\presentation-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
foreach($entry in @(
    'Meyer\Authoring\FourCoupledAirEquipmentAudit.cs',
    'Meyer\Authoring\GearedPolishNativeAudit.cs',
    'Shay\Authoring\ShayExhaustNativeAudit.cs')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $entry) -Destination (Join-Path $project 'Assets\Editor') -Force
}
$env:S060_AUDIT_MANAGED=Join-Path $config.GamePath 'DerailValley_Data\Managed'
$env:S060_AUDIT_MODS=Join-Path $config.GamePath 'Mods'
$env:S060_EXHAUST_AUDIT_RUNTIME=$runtime
$env:S060_SHAY_EXHAUST_BUNDLE=(Resolve-Path -LiteralPath $ShayBundle).Path
$env:S060_DOCKSIDE_AUDIT_BUNDLE=(Resolve-Path -LiteralPath $DocksideBundle).Path
$env:S060_NATIVE_PREVIEW=Join-Path $config.QAPath 'native-preview.json'
$env:S060_CONVENTIONAL_NATIVE=Join-Path $config.QAPath 'conventional-native.json'
$env:S060_040_PRESENTATION_ONLY=$null
$reports=@()
foreach($entry in @(
    @{name='shay-audio';method='ShayExhaustNativeAudit.RunBatch';env='S060_SHAY_EXHAUST_OUTPUT';report='shay-exhaust-validation.json'},
    @{name='040-air';method='FourCoupledAirEquipmentAudit.RunBatch';env='S060_CONVENTIONAL_OUTPUT';report='validation.json'},
    @{name='franklin';method='GearedPolishNativeAudit.RunBatch';env='S060_GEARED_POLISH_OUTPUT';report='geared-polish-validation.json'})) {
    $folder=Join-Path $output $entry.name
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
    [Environment]::SetEnvironmentVariable($entry.env,$folder,'Process')
    $process=Start-Process $config.Unity -ArgumentList @('-batchmode','-projectPath',('"'+$project+'"'),'-executeMethod',$entry.method,'-logFile',('"'+(Join-Path $folder 'unity.log')+'"')) -WindowStyle Hidden -PassThru
    Write-Output "Testing $($entry.name): $folder"
    if(-not $process.WaitForExit(300000)) {$process.Kill();throw "Fixture timeout: $folder"}
    $path=Join-Path $folder $entry.report
    if($process.ExitCode -ne 0 -or -not(Test-Path $path)) {throw "Fixture failed: $folder"}
    $report=Get-Content $path -Raw|ConvertFrom-Json
    if(-not $report.passed -or $report.runtimeSha256 -ne $hash -or (Get-FileHash $runtime).Hash -ne $hash) {throw 'Failed or mismatched presentation evidence.'}
    $reports+=@{name=$entry.name;path=$path;hash=(Get-FileHash $path).Hash;checks=$report.checks}
    Write-Output "$($entry.name): $($report.checks) passed"
}
@{passed=$true;runtimeSha256=$hash;reports=$reports;gameplayValidated=$false;audiblePlaybackValidated=$false} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $output 'validation.json') -Encoding utf8
