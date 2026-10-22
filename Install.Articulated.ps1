param([string]$GamePath = '', [string]$Version = '')
$ErrorActionPreference = 'Stop'
if (Get-Process -Name DerailValley -ErrorAction SilentlyContinue) { throw 'Close Derail Valley before installation.' }
$repo = $PSScriptRoot
. (Join-Path $repo 'Build.Config.ps1')
$config = Get-S060BuildConfig -GamePath $GamePath
$GamePath = $config.GamePath
if (-not $Version) { $Version = $config.Version }
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid package version.' }
$output = Join-Path $repo "build\articulated-$Version"
$stage = [IO.Path]::GetFullPath((Join-Path $output 'package'))
$receiptPath = Join-Path $output 'package-receipt.json'
$package = Get-Content $receiptPath -Raw | ConvertFrom-Json
$expected = @('RearrangedS060\RearrangedS060.dll','RearrangedS060\Info.json','RearrangedS060\LICENSE','RearrangedS060\README.md','RearrangedS060\CREDITS.md')
foreach ($id in @('RS060_Meyer_0440','RS060_Franklin_Dockside_0440')) {
    foreach ($name in @('ccl_bundle','Info.json','README.md')) { $expected += "$id\$name" }
}
$actual = @(Get-ChildItem -LiteralPath $stage -Recurse -File | ForEach-Object { $_.FullName.Substring($stage.Length + 1) })
if (@(Compare-Object $expected $actual).Count) { throw 'Package contains missing or unexpected files.' }
if ($package.files.Count -ne $expected.Count) { throw 'Package receipt has an incomplete file manifest.' }
$recorded = @($package.files | ForEach-Object { [IO.Path]::GetFullPath($_.Path).Substring($stage.Length + 1) })
if (@(Compare-Object $expected $recorded).Count) { throw 'Package receipt does not match the canonical file manifest.' }
foreach ($file in $package.files) {
    if (-not [IO.Path]::GetFullPath($file.Path).StartsWith($stage + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Receipt file outside package.' }
    if ((Get-FileHash -LiteralPath $file.Path).Hash -ne $file.Hash) { throw "Package changed: $($file.Path)" }
}
$mods = [IO.Path]::GetFullPath((Join-Path $GamePath 'Mods'))
$ids = @('RearrangedS060','RS060_Meyer_0440','RS060_Franklin_Dockside_0440')
if ((Get-Content (Join-Path $stage 'RearrangedS060\Info.json') -Raw | ConvertFrom-Json).Version -ne $Version) { throw 'Package version mismatch.' }
foreach ($id in $ids) {
    if ((Get-Content (Join-Path (Join-Path $stage $id) 'Info.json') -Raw | ConvertFrom-Json).Id -ne $id) { throw 'Package identity mismatch.' }
}
$protected = @(
    (Join-Path $mods 'RearrangedS060\Settings.xml'),
    (Join-Path $mods 'RearrangedS282\RearrangedS282.dll'),
    (Join-Path $mods 'RearrangedS282\assets\s282_drivetrain'),
    (Join-Path $mods 'LocoMeshSplitter\LocoMeshSplitter.dll'),
    (Join-Path $mods 'RearrangedS060Probe\RearrangedS060Probe.dll')
)
$beforeProtected = @($protected | Where-Object { Test-Path -LiteralPath $_ } | Get-FileHash | Select-Object Path,Hash)
$priorVersion = (Get-Content (Join-Path $mods 'RearrangedS060\Info.json') -Raw | ConvertFrom-Json).Version
if ($priorVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'Unexpected installed version.' }
$before = @()
foreach ($id in $ids) {
    $source = Join-Path $mods $id
    if (-not (Test-Path -LiteralPath $source)) { throw "Expected existing install: $source" }
    $before += @(Get-ChildItem -LiteralPath $source -Recurse -File | Get-FileHash | Select-Object Path,Hash)
}
$record = [ordered]@{ utc=[DateTime]::UtcNow.ToString('o'); status='preflight verified'; backupCreated=$false; priorVersion=$priorVersion; before=$before; protected=$beforeProtected; packageReceipt=$receiptPath; gameClosed=$true; runtimeValidated=$false }
$record | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'install-receipt-latest.json') -Encoding UTF8
if (Get-Process -Name DerailValley -ErrorAction SilentlyContinue) { throw 'Game started during preparation; installation cancelled.' }
foreach ($id in $ids) {
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $stage $id) -File) {
        if ($file.Name -eq 'Settings.xml') { throw 'Package must not overwrite settings.' }
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path (Join-Path $mods $id) $file.Name) -Force
    }
}
# Only this mod's generated assembly cache. Resolve and check the exact parent
# before deleting each individual file. James requested no install backups.
$runtimeFolder = [IO.Path]::GetFullPath((Join-Path $mods 'RearrangedS060'))
foreach ($cache in Get-ChildItem -LiteralPath $runtimeFolder -File -Filter 'RearrangedS060.dll.*.cache') {
    if ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($cache.FullName)) -ne $runtimeFolder) { throw 'Cache outside intended mod folder.' }
    Remove-Item -LiteralPath $cache.FullName
}
$installed = @()
foreach ($file in $package.files) {
    $relative = $file.Path.Substring($stage.Length).TrimStart('\')
    $target = Join-Path $mods $relative
    $hash = Get-FileHash -LiteralPath $target
    if ($hash.Hash -ne $file.Hash) { throw "Installed file verification failed: $relative" }
    $installed += $hash | Select-Object Path,Hash
}
foreach ($file in $beforeProtected) { if ((Get-FileHash -LiteralPath $file.Path).Hash -ne $file.Hash) { throw "Protected file changed: $($file.Path)" } }
$record.status = 'installed and hash verified'; $record['installed'] = $installed
$record['versions'] = @($ids | ForEach-Object { Get-Content (Join-Path (Join-Path $mods $_) 'Info.json') -Raw | ConvertFrom-Json | Select-Object Id,Version })
$record | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'install-receipt-latest.json') -Encoding UTF8
Write-Output "Installed and verified: $($record.versions | ForEach-Object { $_.Id + ' ' + $_.Version }); no backup created (requested)."
