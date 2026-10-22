$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$version = (Get-Content (Join-Path $repo 'Info.json') -Raw | ConvertFrom-Json).Version
$output = Join-Path $repo ('build\installer-proof-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
$source = Join-Path $output 'source'
$game = Join-Path $output 'fake-game'
$stage = Join-Path $source "build\articulated-$version\package"
New-Item -ItemType Directory -Path $source -Force | Out-Null
foreach ($name in @('Install.Articulated.ps1','Build.Config.ps1','Info.json')) {
    Copy-Item -LiteralPath (Join-Path $repo $name) -Destination $source
}
$ids = @('RearrangedS060','RS060_Meyer_0440','RS060_Franklin_Dockside_0440')
foreach ($id in $ids) {
    $folder = Join-Path $stage $id
    $installed = Join-Path $game "Mods\$id"
    New-Item -ItemType Directory -Path $folder,$installed -Force | Out-Null
    [ordered]@{Id=$id;Version=$version} | ConvertTo-Json | Set-Content (Join-Path $folder 'Info.json')
    [ordered]@{Id=$id;Version='0.14.0'} | ConvertTo-Json | Set-Content (Join-Path $installed 'Info.json')
    $names = if ($id -eq 'RearrangedS060') { @('RearrangedS060.dll','LICENSE','README.md','CREDITS.md') } else { @('ccl_bundle','README.md') }
    foreach ($name in $names) { Set-Content -LiteralPath (Join-Path $folder $name) -Value "Non-executable fixture $id $name" }
}
$protected = @('RearrangedS060\Settings.xml','RearrangedS282\RearrangedS282.dll','RearrangedS282\assets\s282_drivetrain','LocoMeshSplitter\LocoMeshSplitter.dll','RearrangedS060Probe\RearrangedS060Probe.dll')
foreach ($relative in $protected) {
    $path = Join-Path $game "Mods\$relative"
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($path)) -Force | Out-Null
    Set-Content -LiteralPath $path -Value "Protected sentinel $relative"
}
$protectedHashes = @($protected | ForEach-Object { Get-FileHash -LiteralPath (Join-Path $game "Mods\$_") })
$priorInfo = (Get-FileHash (Join-Path $game 'Mods\RearrangedS060\Info.json')).Hash
$packageFiles = @(Get-ChildItem -LiteralPath $stage -Recurse -File | Get-FileHash | Select-Object Path,Hash)
[ordered]@{files=$packageFiles} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $source "build\articulated-$version\package-receipt.json")
$fixtureGate = @{Running=$true}
function Get-Process {
    [CmdletBinding()] param([string]$Name)
    if ($Name -ne 'DerailValley') { throw 'Unexpected fixture process query.' }
    if ($fixtureGate.Running) { [pscustomobject]@{ProcessName='DerailValley'} }
}
$checks = 0
function Check([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:checks++
}
try {
    $rejected = $false
    try { & (Join-Path $source 'Install.Articulated.ps1') -GamePath $game -Version $version }
    catch { if ($_.Exception.Message -notmatch 'Close Derail Valley') { throw }; $rejected = $true }
    Check $rejected 'Running-game gate did not reject installation.'
    Check ((Get-FileHash (Join-Path $game 'Mods\RearrangedS060\Info.json')).Hash -eq $priorInfo) 'Running-game gate changed installed files.'
    $fixtureGate.Running = $false
    $dll = Join-Path $stage 'RearrangedS060\RearrangedS060.dll'
    $bytes = [IO.File]::ReadAllBytes($dll)
    Set-Content -LiteralPath $dll -Value 'Tampered non-executable fixture'
    $rejected = $false
    try { & (Join-Path $source 'Install.Articulated.ps1') -GamePath $game -Version $version }
    catch { if ($_.Exception.Message -notmatch 'Package changed') { throw }; $rejected = $true }
    Check $rejected 'Changed package was not rejected.'
    Check ((Get-FileHash (Join-Path $game 'Mods\RearrangedS060\Info.json')).Hash -eq $priorInfo) 'Changed-package rejection changed installed files.'
    [IO.File]::WriteAllBytes($dll, $bytes)
    $result = & (Join-Path $source 'Install.Articulated.ps1') -GamePath $game -Version $version
    $receipt = Get-Content (Join-Path $source "build\articulated-$version\install-receipt-latest.json") -Raw | ConvertFrom-Json
    Check ($receipt.status -eq 'installed and hash verified') 'Valid fixture installation did not finish.'
    Check ($receipt.backupCreated -eq $false) 'Installer created or claimed a backup.'
    Check (@($receipt.installed).Count -eq 11) 'Wrong installed-file count.'
    foreach ($file in $protectedHashes) { Check ((Get-FileHash $file.Path).Hash -eq $file.Hash) "Protected fixture changed: $($file.Path)" }
    foreach ($file in $receipt.installed) { Check ((Get-FileHash $file.Path).Hash -eq $file.Hash) "Installed fixture differs: $($file.Path)" }
    Check (@(Get-ChildItem -LiteralPath $output -Recurse -Directory | Where-Object Name -Match 'backup|rollback').Count -eq 0) 'Unexpected backup folder.'
    [ordered]@{passed=$true;checks=$checks;scenarios=@('running game rejected before mutation','changed package rejected before mutation','valid package installed without backup; protected files retained');fixtureOnly=$true;realInstallationTouched=$false;installerSha256=(Get-FileHash (Join-Path $repo 'Install.Articulated.ps1')).Hash} |
        ConvertTo-Json -Depth 4 | Set-Content (Join-Path $output 'installer-validation.json') -Encoding UTF8
    Write-Output "$checks installer assertions passed across three fixture scenarios; no real installation touched. Evidence: $output"
}
finally { Remove-Item -LiteralPath Function:\Get-Process -ErrorAction SilentlyContinue }
