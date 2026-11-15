param(
    [string]$BuildDirectory = (Join-Path $PSScriptRoot 'build\multiplayer-0.3.6\final'),
    [string]$SeedArchive = 'B:\GitHub repo\rearranged-s060\source-0.3.5\build\nexus-0.3.5-single\RearrangedS060-0.3.5-UMM.zip',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'build\multiplayer-0.3.6\package')
)
$ErrorActionPreference = 'Stop'
$expectedSeed = '598760CC3E639A64965B53D857287A094EB91597E1A7EC2A81FEE211D7F993EA'
if ((Get-FileHash -LiteralPath $SeedArchive).Hash -ne $expectedSeed) { throw 'The preserved 0.3.5 seed archive changed.' }
$receipt = Get-Content (Join-Path $BuildDirectory 'build-receipt.json') -Raw | ConvertFrom-Json
foreach ($file in @($receipt.binaries) + @($receipt.sources)) {
    if ((Get-FileHash -LiteralPath $file.Path).Hash -ne $file.Hash) { throw "Build input/output changed: $($file.Path)" }
}
$info = Get-Content (Join-Path $PSScriptRoot 'Info.json') -Raw | ConvertFrom-Json
if ($info.Version -ne $receipt.version -or $info.MultiplayerCompatibility -ne 'All' -or 'Multiplayer' -notin $info.LoadAfter -or 'Multiplayer' -in $info.Requirements) { throw 'Optional multiplayer manifest contract is invalid.' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
$stage = Join-Path $OutputDirectory ('stage-' + [Guid]::NewGuid().ToString('N'))
Expand-Archive -LiteralPath $SeedArchive -DestinationPath $stage
$mod = Join-Path $stage 'RearrangedS060'
$assetHashes = @(Get-ChildItem (Join-Path $mod 'assets') -Recurse -File | Get-FileHash | Select-Object Path,Hash)
foreach ($name in @('RearrangedS060.dll','RearrangedS060.Multiplayer.dll')) { Copy-Item -LiteralPath (Join-Path $BuildDirectory $name) -Destination $mod -Force }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Info.json'),(Join-Path $PSScriptRoot 'MULTIPLAYER_TESTING.txt') -Destination $mod -Force
foreach ($name in @('README.md','INSTALL.txt')) {
    $path = Join-Path $mod $name
    $content = (Get-Content -LiteralPath $path -Raw).Replace('0.3.5', $info.Version).Replace(' RELEASE', ' MULTIPLAYER CANDIDATE')
    $content += @"

MULTIPLAYER

Multiplayer is optional. Every player needs this identical complete package.
Use DV Multiplayer 0.1.16.0-beta (API 1.1) for the initial multiplayer checks.
The host checks radio and UMM requests and controls guest permissions.
The host selects random arrangements. Late joiners receive the host's layouts.
Native Multiplayer owns movement and simulation. Read MULTIPLAYER_TESTING.txt.
This is an offline-validated candidate awaiting James's live peer testing.
"@
    Set-Content -LiteralPath $path -Value $content -Encoding utf8
}
$changelog = Join-Path $mod 'CHANGELOG.txt'
$history = Get-Content -LiteralPath $changelog -Raw
@"
0.3.6-mp
Added optional multiplayer support from the current Rearranged S282 integration.
Added host-checked radio and UMM requests, guest permissions and late-join state.
Kept stock and short-wheelbase 0-6-0 as separate selections.
Added host-only independent traction and replicated group slip/RPM presentation.
Added native replication of the Shay multi-turn transmission phase.
Included the 0.3.5 buffer, axle identity, support placement and switching repairs.
Included baked Dockside sleeves; retained donor hiding and saved identities.
This candidate awaits live multiplayer acceptance.

$history
"@ | Set-Content -LiteralPath $changelog -Encoding utf8
foreach ($file in $assetHashes) { if ((Get-FileHash -LiteralPath $file.Path).Hash -ne $file.Hash) { throw 'Existing asset payload changed.' } }
$expected = @('CHANGELOG.txt','CREDITS.md','Info.json','INSTALL.txt','LICENSE','README.md','MULTIPLAYER_TESTING.txt','RearrangedS060.dll','RearrangedS060.Multiplayer.dll','assets\Dockside\ccl_bundle','assets\Dockside\README.md','assets\Meyer\ccl_bundle','assets\Meyer\README.md','assets\Shay\shay_running_gear')
$actual = @(Get-ChildItem $mod -Recurse -File | ForEach-Object { $_.FullName.Substring($mod.Length + 1) })
if (@(Compare-Object $expected $actual).Count) { throw 'Unexpected files in multiplayer package.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = Join-Path $OutputDirectory "RearrangedS060-$($info.Version)-UMM.zip"
if (Test-Path -LiteralPath $archive) { throw 'Package already exists; use a new output directory to retain evidence.' }
$zip = [IO.Compression.ZipFile]::Open($archive, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem $mod -Recurse -File | Sort-Object FullName) {
        $entry = 'RearrangedS060/' + $file.FullName.Substring($mod.Length + 1).Replace('\','/')
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$file.FullName,$entry,[IO.Compression.CompressionLevel]::Optimal)
    }
} finally { $zip.Dispose() }
# Read back every archive member and compare its bytes to staging.
$zip = [IO.Compression.ZipFile]::OpenRead($archive)
try {
    if ($zip.Entries.Count -ne $expected.Count) { throw 'Archive entry count changed.' }
    $sha = [Security.Cryptography.SHA256]::Create()
    foreach ($entry in $zip.Entries) {
        if (-not $entry.FullName.StartsWith('RearrangedS060/') -or $entry.FullName.Contains('\') -or $entry.FullName.Contains('..')) { throw 'Invalid archive path.' }
        $stream = $entry.Open()
        try { $hash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','') } finally { $stream.Dispose() }
        $relative = $entry.FullName.Substring('RearrangedS060/'.Length)
        if ($hash -ne (Get-FileHash (Join-Path $mod $relative)).Hash) { throw "Archive content mismatch: $relative" }
    }
    $sha.Dispose()
} finally { $zip.Dispose() }
[ordered]@{
    version=$info.Version; status='Staged multiplayer candidate; awaiting James live peer testing'
    seedArchive=$SeedArchive; seedSha256=$expectedSeed; buildReceipt=(Join-Path $BuildDirectory 'build-receipt.json')
    stage=$mod; files=@(Get-ChildItem $mod -Recurse -File | Get-FileHash | Select-Object Path,Hash)
    assetsUnchanged=$true; entriesVerified=$expected.Count; zip=Get-FileHash $archive | Select-Object Path,Hash
    installed=$false; published=$false; gameLaunched=$false
} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $OutputDirectory 'package-receipt.json') -Encoding utf8
Write-Output $archive
