param(
    [string]$GamePath = 'B:\SteamLibrary\steamapps\common\Derail Valley',
    [string]$OriginalArchive = 'C:\Users\james\Desktop\new mods\Rearranged S282-752-1-2-9-1710020512.zip'
)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Build.Stage1.ps1') -GamePath $GamePath -Stage 3
$out = Join-Path $PSScriptRoot 'build\stage3'
$package = Join-Path $out 'package\RearrangedS282'
New-Item -ItemType Directory -Path $package -Force | Out-Null
$info = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Info.json') -Raw | ConvertFrom-Json
$info.DisplayName = 'Rearranged S282 - B99 All Standard Trial'
$info.Version = '1.3.3'
$info | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $out 'Info.json') -Encoding utf8
foreach ($name in @('RearrangedS282.dll','Info.json')) {
    Copy-Item -LiteralPath (Join-Path $out $name) -Destination $package -Force
}
foreach ($name in @('LICENSE','ALL_STANDARD_TESTING.md','MANUAL_TEST_CHECKLIST.txt')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $package -Force
}
# Reuse the original release's existing mesh exporter, without game meshes or the duplex bundle.
if ((Get-FileHash -LiteralPath $OriginalArchive -Algorithm SHA256).Hash -ne '18B2CA6B73EBFECE33401DB924DDFECDACFEBCFDF193E69707CB436AB56E778C') {
    throw 'Original release archive hash does not match the assessed release.'
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($OriginalArchive)
try {
    foreach ($entry in $archive.Entries) {
        if (-not $entry.FullName.StartsWith('AssetStudioModCLI_net472_win64_contained/') -or $entry.Name -eq '') { continue }
        $destination = [IO.Path]::GetFullPath((Join-Path $package $entry.FullName))
        if (-not $destination.StartsWith([IO.Path]::GetFullPath($package) + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Archive path escaped package.' }
        New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destination, $true)
    }
} finally { $archive.Dispose() }
Compress-Archive -LiteralPath $package -DestinationPath (Join-Path $PSScriptRoot 'build\RearrangedS282-B99-all-standard-trial.zip') -Force
Write-Output 'All-standard development ZIP prepared; no installation or game launch performed.'
