param(
    [string]$Version = '1.9.2',
    [string]$DesktopDirectory = 'C:\Users\james\Desktop\Derail Valley Mods'
)
# Repackages the runtime-trial build of $Version as a player-facing Nexus archive:
# same DLL, bundle, exporter and licences; test-only notes replaced by README,
# CHANGELOG and CREDITS from release-copy\$Version. Uploads nothing.
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$candidate = Join-Path $root "build\runtime-trial-$Version"
$receipt = Get-Content -LiteralPath (Join-Path $candidate 'build-receipt.json') -Raw | ConvertFrom-Json
$source = $receipt.package
if (-not (Test-Path -LiteralPath (Join-Path $source 'RearrangedS282.dll'))) { throw "Built package not found: $source" }
if ((Get-FileHash -LiteralPath (Join-Path $source 'RearrangedS282.dll')).Hash -ne $receipt.dllSHA256) { throw 'Built DLL differs from its receipt.' }
$copy = Join-Path $root "release-copy\$Version"
$out = Join-Path $root "release\$Version"
New-Item -ItemType Directory -Path $out -Force | Out-Null
$stage = Join-Path $root ('build\nexus-staging-' + [Guid]::NewGuid().ToString('N'))
$mod = Join-Path $stage 'RearrangedS282'
New-Item -ItemType Directory -Path $stage | Out-Null
Copy-Item -LiteralPath $source -Destination $mod -Recurse
foreach ($test in 'RUNTIME_TRIAL_TESTING.txt','MANUAL_TEST_CHECKLIST.txt','README_BETA.txt','ARTICULATED_TEST_STATUS.txt')
{ Remove-Item -LiteralPath (Join-Path $mod $test) -ErrorAction SilentlyContinue }
foreach ($doc in 'README.txt','CHANGELOG.txt','CREDITS.txt') { Copy-Item -LiteralPath (Join-Path $copy $doc) -Destination $mod -Force }
$infoPath = Join-Path $mod 'Info.json'
$info = Get-Content -LiteralPath $infoPath -Raw | ConvertFrom-Json
if ($info.Version -ne $Version) { throw "Info.json version $($info.Version) is not $Version." }
$info.DisplayName = 'Rearranged S282 - B99'
$info | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $infoPath -Encoding utf8
$bad = Get-ChildItem -LiteralPath $mod -Recurse -File | Where-Object { $_.Name -match '\.(log|cache|obj|pdb|jsonl)$|^Settings\.xml$|stage2-runtime' }
if ($bad) { throw "Unexpected files: $($bad.FullName -join ', ')" }
foreach ($need in 'RearrangedS282.dll','Info.json','LICENSE','THIRD_PARTY_NOTICES.txt','assets\s282_drivetrain','AssetStudioModCLI_net472_win64_contained\AssetStudioModCLI.exe')
{ if (-not (Test-Path -LiteralPath (Join-Path $mod $need))) { throw "Missing $need" } }
$zipName = "RearrangedS282-B99-$Version.zip"
$zip = Join-Path $out $zipName
# Standard forward-slash entry names (Windows PowerShell's Compress-Archive writes backslashes).
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
$writer = [IO.Compression.ZipFile]::Open($zip, [IO.Compression.ZipArchiveMode]::Create)
try {
    $base = [IO.Path]::GetFullPath($stage).TrimEnd([char]92) + [char]92
    foreach ($file in Get-ChildItem -LiteralPath $mod -Recurse -File | Sort-Object FullName) {
        $name = $file.FullName.Substring($base.Length).Replace([char]92, [char]47)
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($writer, $file.FullName, $name, [IO.Compression.CompressionLevel]::Optimal)
    }
} finally { $writer.Dispose() }
Remove-Item -LiteralPath $stage -Recurse -Force
$zipHash = (Get-FileHash -LiteralPath $zip).Hash
"$zipHash  $zipName`r`n$($receipt.dllSHA256)  RearrangedS282/RearrangedS282.dll" | Set-Content -LiteralPath (Join-Path $out 'CHECKSUMS.txt') -Encoding ascii
Copy-Item -LiteralPath (Join-Path $copy 'NEXUS_RELEASE_COPY.txt'),(Join-Path $copy 'CHANGELOG.txt') -Destination $out -Force
$desk = Join-Path $DesktopDirectory "RearrangedS282-B99-$Version-Nexus"
New-Item -ItemType Directory -Path $desk -Force | Out-Null
Copy-Item -LiteralPath $zip,(Join-Path $out 'CHECKSUMS.txt'),(Join-Path $out 'NEXUS_RELEASE_COPY.txt'),(Join-Path $out 'CHANGELOG.txt') -Destination $desk -Force
$archive = [IO.Compression.ZipFile]::OpenRead($zip)
try { $files = @($archive.Entries | Where-Object Name -NE '' | ForEach-Object FullName) } finally { $archive.Dispose() }
if ($files | Where-Object { $_ -match '\\' -or -not $_.StartsWith('RearrangedS282/') }) { throw 'Archive entries must be RearrangedS282/... with forward slashes.' }
[ordered]@{ version=$Version; zip=$zip; desktop=(Join-Path $desk $zipName); zipSHA256=$zipHash; dllSHA256=$receipt.dllSHA256; files=$files.Count; uploaded=$false } | ConvertTo-Json
