param(
    [string]$BuildDirectory = (Join-Path $PSScriptRoot 'build\release-0.3.7-mp-vr-20261119\runtime'),
    [string]$SeedArchive = 'B:\GitHub repo\rearranged-s060\source-0.3.5\build\nexus-0.3.5-single\RearrangedS060-0.3.5-UMM.zip',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'build\release-0.3.7-mp-vr-20261119\package'),
    [string]$StereoBundleDirectory = ''
)
$ErrorActionPreference = 'Stop'
$expectedSeed = '598760CC3E639A64965B53D857287A094EB91597E1A7EC2A81FEE211D7F993EA'
# Single-pass VR Standard shader from Build.StereoShaders.ps1, checked there for stereo variants.
if (-not $StereoBundleDirectory) { $StereoBundleDirectory = (Get-ChildItem (Join-Path $PSScriptRoot 'build\stereo-shaders') -Directory -Filter 'bundle-*' | Sort-Object Name -Descending | Select-Object -First 1).FullName }
$stereoReceipt = Get-Content (Join-Path $StereoBundleDirectory 'bundle-receipt.json') -Raw | ConvertFrom-Json
$stereoCheck = Get-Content (Join-Path $StereoBundleDirectory 'stereo-validation.json') -Raw | ConvertFrom-Json
$stereoBundle = Join-Path $StereoBundleDirectory 's060_stereo_standard'
if (-not $stereoReceipt.passed -or -not $stereoCheck.passed -or (Get-FileHash -LiteralPath $stereoBundle).Hash -ne $stereoReceipt.bundle.Hash) { throw 'Validated stereo shader bundle required.' }
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
New-Item -ItemType Directory -Path (Join-Path $mod 'assets\Shared') -Force | Out-Null
Copy-Item -LiteralPath $stereoBundle -Destination (Join-Path $mod 'assets\Shared\s060_stereo_standard')
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
    if ($name -eq 'README.md') {
        $content += "`r`nVR`r`n`r`nThe Shay, Meyer and Dockside running gear now render correctly in VR. Their`r`nmaterials use a single-pass stereo build of Unity's Standard shader, included`r`nas assets/Shared/s060_stereo_standard. Textures and finishes are unchanged.`r`n"
        $content += "`r`nMEYER EFFECTS AND COUPLING`r`n`r`nWheel-slip sparks follow the replacement wheel contact positions. The physical`r`nrear coupling anchor retains the native buffer spacing. Hook and hose positions`r`nare retained. Check the coupled buffers and wheel-slip effects in game.`r`n"
        $content += "`r`n`r`nBRAKES`r`n`r`nThe 0-10-0, 0-8-0 and short-wheelbase 0-6-0 use fitted native brake shoes`r`nand calipers. Native brake heating, materials and levels of detail are retained.`r`nChanging back to stock restores the original brake meshes.`r`n"
        $content += "`r`nRODS AND OIL CUPS`r`n`r`nThe 0-4-0, 0-8-0 and 0-10-0 use fitted native S060 coupling-rod sections`r`nat both levels of detail. The three-axle layouts retain their native rods.`r`nConventional guide oil cups stay seated on the body-mounted slidebars when`r`nthe mechanism is resized or shifted. Added cup necks meet the native rod bosses.`r`nMeyer, Dockside and Shay cups were checked against their drawn supports offline.`r`n"
    }
    Set-Content -LiteralPath $path -Value $content -Encoding utf8
}
$changelog = Join-Path $mod 'CHANGELOG.txt'
$history = Get-Content -LiteralPath $changelog -Raw
@"
0.3.7-mp
Fixed Shay, Meyer and Dockside running-gear textures shifting with head movement in VR.
Corrected wheel-slip spark positions on the articulated replacement axles.
Restored native coupled-buffer spacing at the Meyer rear.
Retained the fitted hook, hose, buffer rig and native compression controls.

0.3.6-mp
Replaced generated conventional coupling rods with fitted native S060 sections.
Retained native rod profiles, texture mapping, materials and both levels of detail.
Kept conventional guide oil cups seated on the body-mounted slidebars.
Matched added oil-cup necks to the native coupling-rod boss height.
Checked cup support on every arrangement, including Meyer, Dockside and Shay.
Restored native brake shoes and calipers on 0-10-0, 0-8-0 and short-wheelbase 0-6-0.
Fitted the brake geometry to the actual wheel positions.
Retained native brake heating, materials and levels of detail.
Restoring stock restores the original brake meshes.
Changed the Shay upper exhaust pipe to follow the body and enter the smokebox diagonally.
Changed the Meyer rear upper steam pipe to use a longer sweep above the cab.
Kept the Shay compressor exhaust connection and both Meyer step brackets.
Removed the two middle brake shoes from the 0-4-0. Outer brakes remain visible.
Restoring stock restores the original brake-shoe mesh and all calipers.
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
$expected = @('CHANGELOG.txt','CREDITS.md','Info.json','INSTALL.txt','LICENSE','README.md','MULTIPLAYER_TESTING.txt','RearrangedS060.dll','RearrangedS060.Multiplayer.dll','assets\Dockside\ccl_bundle','assets\Dockside\README.md','assets\Meyer\ccl_bundle','assets\Meyer\README.md','assets\Shared\s060_stereo_standard','assets\Shay\shay_running_gear')
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
