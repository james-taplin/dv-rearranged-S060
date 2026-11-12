param([switch]$SkipAuthoring, [string]$MeyerEvidence = '', [string]$DocksideEvidence = '')
$ErrorActionPreference = 'Stop'
$repo = $PSScriptRoot
. (Join-Path $repo 'Build.Config.ps1')
$config = Get-S060BuildConfig
function Get-FreshEvidence([string]$Root, [string]$Pattern, [string]$Receipt, [DateTime]$Since) {
    $match = Get-ChildItem -LiteralPath $Root -Directory -Filter $Pattern | Where-Object {
        $path = Join-Path $_.FullName $Receipt
        (Test-Path -LiteralPath $path) -and (Get-Item -LiteralPath $path).LastWriteTimeUtc -ge $Since
    } | Sort-Object Name -Descending | Select-Object -First 1
    if (-not $match) { throw "No fresh $Receipt was produced by this release run." }
    $match.FullName
}
& (Join-Path $repo 'Prepare.QA.ps1')
if (-not (Test-Path (Join-Path $repo 'Meyer\UnityProject'))) {
    & (Join-Path $repo 'Meyer\Setup.Authoring.ps1') -ReferenceProject $config.ReferenceProject
}
if (-not $SkipAuthoring) {
    $started = [DateTime]::UtcNow
    & (Join-Path $repo 'Meyer\Run.Authoring.ps1') -Unity $config.Unity -UpdateDraft -ExportPrototype -FranklinBundle (Join-Path $config.GamePath 'Mods\RearrangedS282\assets\s282_drivetrain')
    $MeyerEvidence = Get-FreshEvidence (Join-Path $repo 'build\meyer') 'authoring-*' 'bundle-evidence.json' $started
    $started = [DateTime]::UtcNow
    & (Join-Path $repo 'Dockside\Run.Authoring.ps1') -Unity $config.Unity -FranklinBundle (Join-Path $config.GamePath 'Mods\RearrangedS282\assets\s282_drivetrain')
    $DocksideEvidence = Get-FreshEvidence (Join-Path $repo 'build\dockside') 'authoring-*' 'bundle-evidence.json' $started
}
if (-not $MeyerEvidence -or -not $DocksideEvidence) { throw 'Provide both fresh hash-bound authoring evidence paths with -SkipAuthoring.' }
& (Join-Path $repo 'Test.BundleEvidence.ps1') -Evidence $MeyerEvidence
& (Join-Path $repo 'Test.BundleEvidence.ps1') -Evidence $DocksideEvidence
& (Join-Path $repo 'tests\Installer\Run.ps1')
& (Join-Path $repo 'Build.Shay.ps1')
$started = [DateTime]::UtcNow
& (Join-Path $repo 'Test.Runtime.ps1')
$NativeEvidence = Get-FreshEvidence (Join-Path $repo "build\radio-$($config.Version)") 'runtime-audits-*' 'runtime-validation.json' $started
$started = [DateTime]::UtcNow
& (Join-Path $repo 'Test.IndependentSlip.ps1')
$IndependentEvidence = Join-Path (Get-FreshEvidence (Join-Path $repo "build\independent-slip-$($config.Version)") 'native-*' 'independent-slip-validation.json' $started) 'independent-slip-validation.json'
& (Join-Path $repo 'Build.Radio.ps1') -DocksideEvidence $DocksideEvidence -NativeEvidence $NativeEvidence -IndependentEvidence $IndependentEvidence
& (Join-Path $repo 'Package.Articulated.ps1') -MeyerEvidence $MeyerEvidence -DocksideEvidence $DocksideEvidence
& (Join-Path $repo 'Package.Nexus.ps1')
Write-Output 'Single-folder release ZIP staged. Validate it with the native UMM and bundle-reopen fixtures before Install.Nexus.ps1; game must be closed.'
