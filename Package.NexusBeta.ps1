param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'release\1.3.3-Beta1'),
    [string]$OriginalArchive = 'C:\Users\james\Desktop\new mods\Rearranged S282-752-1-2-9-1710020512.zip'
)
$ErrorActionPreference = 'Stop'
$testedHash = 'DA6184725FFB341D13BDAFCC04048F677B973937066144361B5DCF671DABC1DD'
$dll = Join-Path $PSScriptRoot 'build\stage3\RearrangedS282.dll'
if ((Get-FileHash -LiteralPath $dll).Hash -ne $testedHash) { throw 'DLL differs from the runtime-tested 1.3.3 binary.' }
if ((Get-FileHash -LiteralPath $OriginalArchive).Hash -ne '18B2CA6B73EBFECE33401DB924DDFECDACFEBCFDF193E69707CB436AB56E778C') { throw 'Original exporter archive differs from the assessed release.' }
$run = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'evidence\all-standard-1.3.3-passed-20261017-005225\review.json') -Raw | ConvertFrom-Json
if (-not $run.passed -or $run.passingArrangements -ne 31 -or $run.nativeOilcups -ne 6) { throw 'Expected structural pass evidence is missing.' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$stage = Join-Path $PSScriptRoot ('build\nexus-staging-' + [Guid]::NewGuid().ToString('N'))
$mod = Join-Path $stage 'RearrangedS282'
New-Item -ItemType Directory -Path $mod | Out-Null
Copy-Item -LiteralPath $dll,(Join-Path $PSScriptRoot 'LICENSE'),(Join-Path $PSScriptRoot 'MANUAL_TEST_CHECKLIST.txt') -Destination $mod
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'release-copy\README_BETA.txt'),(Join-Path $PSScriptRoot 'release-copy\CREDITS.txt') -Destination $mod
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'release-copy\THIRD_PARTY_NOTICES.txt') -Destination $mod
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'release-copy\licenses') -Destination $mod -Recurse
$toolAllow = @('AssetStudio.dll','AssetStudioUtility.dll','AssetStudioModCLI.exe','AssetStudioModCLI.exe.config',
    'K4os.Compression.LZ4.dll','Mono.Cecil.dll','Newtonsoft.Json.dll','System.Buffers.dll','System.Memory.dll',
    'System.Numerics.Vectors.dll','System.Runtime.CompilerServices.Unsafe.dll','System.Text.Encoding.CodePages.dll','System.ValueTuple.dll')
$info = [ordered]@{
    Id = 'RearrangedS282'; Version = '1.3.3'; DisplayName = 'Rearranged S282 - B99 Conventional Beta'
    Author = 'SatiricRug (original); JimTaplin (B99 port)'; EntryMethod = 'RearrangedS282.Main.Load'
    ManagerVersion = '0.27.3'; Requirements = @('LocoMeshSplitter'); LoadAfter = @('Gauge')
}
$info | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $mod 'Info.json') -Encoding utf8
Add-Type -AssemblyName System.IO.Compression.FileSystem
$original = [IO.Compression.ZipFile]::OpenRead($OriginalArchive)
$toolFiles = 0
try {
    foreach ($entry in $original.Entries) {
        if (-not $entry.FullName.StartsWith('AssetStudioModCLI_net472_win64_contained/') -or $entry.Name -eq '') { continue }
        if ($entry.Name -notin $toolAllow) { continue }
        $target = [IO.Path]::GetFullPath((Join-Path $mod $entry.FullName))
        if (-not $target.StartsWith([IO.Path]::GetFullPath($mod)+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Archive path escaped package.' }
        New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,$target,$false)
        $toolFiles++
    }
} finally { $original.Dispose() }
if ($toolFiles -ne $toolAllow.Count) { throw 'Unexpected mesh exporter file count.' }
$zipPath = Join-Path $OutputDirectory 'RearrangedS282-B99-1.3.3-Beta1.zip'
Compress-Archive -LiteralPath $mod -DestinationPath $zipPath -Force
$zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
$original = [IO.Compression.ZipFile]::OpenRead($OriginalArchive)
$sha = [Security.Cryptography.SHA256]::Create()
function EntryHash($entry) {
    $stream = $entry.Open()
    try { [Convert]::ToHexString($sha.ComputeHash($stream)) } finally { $stream.Dispose() }
}
try {
    $files = @($zip.Entries | Where-Object Name -NE '')
    $expectedFiles = 7 + $toolAllow.Count + @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'release-copy\licenses') -File -Recurse).Count
    if ($files.Count -ne $expectedFiles) { throw 'Unexpected beta archive file count.' }
    $inventory = @(foreach ($entry in $files) {
        if (-not $entry.FullName.StartsWith('RearrangedS282/') -or $entry.FullName -match '\.\.|\.cache$|\.log$|\.obj$|stage2-runtime|s282_drivetrain|Assembly-CSharp|DV\.Simulation') { throw "Unexpected release file: $($entry.FullName)" }
        $hash = EntryHash $entry
        $relative = $entry.FullName.Substring('RearrangedS282/'.Length)
        if ($relative.StartsWith('AssetStudioModCLI_net472_win64_contained/')) {
            if ($entry.Name -notin $toolAllow) { throw 'Unreviewed exporter dependency in package.' }
            if ($hash -ne (EntryHash ($original.GetEntry($relative)))) { throw "Exporter file changed: $relative" }
        }
        if ($relative -eq 'RearrangedS282.dll' -and $hash -ne $testedHash) { throw 'Packaged DLL changed.' }
        if ($relative -eq 'LICENSE' -and $hash -ne (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'LICENSE')).Hash) { throw 'Original license changed.' }
        [ordered]@{path=$entry.FullName; bytes=$entry.Length; sha256=$hash}
    })
} finally { $zip.Dispose(); $original.Dispose(); $sha.Dispose() }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'release-copy\NEXUS_RELEASE_COPY.txt') -Destination $OutputDirectory -Force
$zipHash = (Get-FileHash -LiteralPath $zipPath).Hash
"$zipHash  RearrangedS282-B99-1.3.3-Beta1.zip`r`n$testedHash  RearrangedS282/RearrangedS282.dll" | Set-Content -LiteralPath (Join-Path $OutputDirectory 'CHECKSUMS.txt') -Encoding ascii
$manifest = [ordered]@{
    release='1.3.3 Beta 1'; author='JimTaplin'; upstream='SatiricRug / Peter Schinske'
    dllSHA256=$testedHash; zipSHA256=$zipHash; fileCount=$inventory.Count; originalExporterFiles=$toolFiles
    binaryUnchangedFromSuccessfulRuntimeTest=$true; stationaryArrangementsPassed=31; oilcupBindingsCaptured=6
    uploaded=$false; published=$false; files=$inventory
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'release-manifest.json') -Encoding utf8
Write-Output "Prepared verified beta: $zipPath"
Write-Output "$($inventory.Count) files; $toolFiles unchanged exporter files with third-party notices; tested DLL unchanged. SHA256 $zipHash"
