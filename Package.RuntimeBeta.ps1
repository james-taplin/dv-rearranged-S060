param(
    [string]$OutputDirectory = 'C:\Users\james\Desktop\Derail Valley Mods\RearrangedS282-B99-1.3.6-Beta2'
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$version = '1.3.6'
$releaseName = 'RearrangedS282-B99-1.3.6-Beta2'
$expectedDll = '89F99281247B67B1EB806465AE6C681F73620BE9CF93ED237CBA0D581FCDC538'
$receipt = Get-Content -LiteralPath (Join-Path $root 'build\delta-compatibility-candidate\build-receipt.json') -Raw | ConvertFrom-Json
$baseline = Join-Path $root 'release\1.3.3-Beta1\RearrangedS282-B99-1.3.3-Beta1.zip'
$installed = 'B:\SteamLibrary\steamapps\common\Derail Valley\Mods\RearrangedS282\RearrangedS282.dll'
$releaseDirectory = Join-Path $root 'release\1.3.6-Beta2'
$zipPath = Join-Path $releaseDirectory ($releaseName + '.zip')
$desktopZip = Join-Path $OutputDirectory ($releaseName + '.zip')
$copyDirectory = Join-Path $root 'release-copy\1.3.6-Beta2'
if ((Test-Path -LiteralPath $zipPath) -or (Test-Path -LiteralPath $desktopZip)) { throw 'Release ZIP already exists; preserve it rather than overwriting.' }
if ($receipt.version -ne $version -or $receipt.dllSHA256 -ne $expectedDll) { throw 'Unexpected candidate receipt.' }
if ((Get-FileHash -LiteralPath $receipt.zip).Hash -ne $receipt.zipSHA256) { throw 'Existing test ZIP differs from its receipt.' }
if ((Get-FileHash -LiteralPath $installed).Hash -ne $expectedDll) { throw 'Installed binary differs from the assessed candidate.' }
if ((Get-FileHash -LiteralPath $baseline).Hash -ne '4A8F1660AAFA1A03A2607B843AB20FB6DE7C625FFD08A94369767DBD8558FCBD') { throw 'Audited baseline ZIP changed.' }
$documents = @('README_BETA.txt','CREDITS.txt','CHANGELOG.txt','TEST_STATUS.txt')
foreach ($document in ($documents + 'NEXUS_RELEASE_COPY.txt')) {
    if (-not (Test-Path -LiteralPath (Join-Path $copyDirectory $document))) { throw "Missing release text: $document" }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$sha = [Security.Cryptography.SHA256]::Create()
function EntryHash($entry) {
    $stream = $entry.Open()
    try { [Convert]::ToHexString($sha.ComputeHash($stream)) } finally { $stream.Dispose() }
}
$testZip = [IO.Compression.ZipFile]::OpenRead($receipt.zip)
try {
    foreach ($entry in $testZip.Entries) {
        if (-not $entry.FullName.StartsWith('RearrangedS282/') -or $entry.FullName -match '\.\.|:|\\') { throw "Unexpected archive path: $($entry.FullName)" }
    }
    if ((EntryHash ($testZip.GetEntry('RearrangedS282/RearrangedS282.dll'))) -ne $expectedDll) { throw 'Test ZIP binary differs from installed candidate.' }
} finally { $testZip.Dispose() }

# Repackage the existing, verified binary; never rebuild it or touch the installation.
$staging = Join-Path $root ('build\nexus-1.3.6-staging-' + [Guid]::NewGuid().ToString('N'))
Expand-Archive -LiteralPath $receipt.zip -DestinationPath $staging
$mod = Join-Path $staging 'RearrangedS282'
$infoPath = Join-Path $mod 'Info.json'
$info = Get-Content -LiteralPath $infoPath -Raw | ConvertFrom-Json
$info.DisplayName = 'Rearranged S282 - B99 Conventional Beta'
if ($info.Id -ne 'RearrangedS282' -or $info.Version -ne $version -or $info.EntryMethod -ne 'RearrangedS282.Main.Load') { throw 'Unexpected UMM identity or entry method.' }
if (@($info.Requirements).Count -ne 1 -or $info.Requirements[0] -ne 'LocoMeshSplitter') { throw 'Unexpected runtime requirements.' }
if (@($info.LoadAfter).Count -ne 3 -or @($info.LoadAfter | Where-Object { $_ -notin @('Gauge','DVCarChanger','S282ModelTweaks') }).Count -ne 0) { throw 'Optional mod ordering differs from the installed trial.' }
$info | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $infoPath -Encoding utf8
foreach ($document in $documents) { Copy-Item -LiteralPath (Join-Path $copyDirectory $document) -Destination $mod }
$checklist = Get-Content -LiteralPath (Join-Path $root 'RUNTIME_TRIAL_TESTING.txt') -Raw
$checklist = $checklist.Replace('1.3.6 test build','1.3.6 Beta 2')
$checklist = $checklist.Replace('Send the files or ask Codex to inspect them locally along with the game log.','Include the capture files and game log with your bug report.')
foreach ($name in @('RUNTIME_TRIAL_TESTING.txt','MANUAL_TEST_CHECKLIST.txt')) {
    $checklist | Set-Content -LiteralPath (Join-Path $mod $name) -Encoding utf8
}
New-Item -ItemType Directory -Path $releaseDirectory -Force | Out-Null
Compress-Archive -LiteralPath $mod -DestinationPath $zipPath

$zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
$oldZip = [IO.Compression.ZipFile]::OpenRead($baseline)
$changedNames = @('Info.json','RearrangedS282.dll','README_BETA.txt','MANUAL_TEST_CHECKLIST.txt','CREDITS.txt')
try {
    $files = @($zip.Entries | Where-Object Name -NE '')
    $oldFiles = @($oldZip.Entries | Where-Object Name -NE '')
    $expectedNames = @($oldFiles.FullName) + @('RearrangedS282/RUNTIME_TRIAL_TESTING.txt','RearrangedS282/CHANGELOG.txt','RearrangedS282/TEST_STATUS.txt')
    if ($files.Count -ne $expectedNames.Count -or @($files.FullName | Select-Object -Unique).Count -ne $files.Count) { throw 'Unexpected or duplicate packaged files.' }
    $inventory = @(foreach ($entry in $files) {
        if ($entry.FullName -notin $expectedNames -or $entry.FullName -match '\.\.|\.cache$|\.log$|\.pdb$|stage2-runtime|\.jsonl$|s282_drivetrain|Assembly-CSharp|DV\.Simulation') { throw "Unexpected release file: $($entry.FullName)" }
        $hash = EntryHash $entry
        $oldEntry = $oldZip.GetEntry($entry.FullName)
        if ($oldEntry -and $entry.Name -notin $changedNames -and $hash -ne (EntryHash $oldEntry)) { throw "Unintended baseline file change: $($entry.FullName)" }
        if ($entry.Name -eq 'RearrangedS282.dll' -and $hash -ne $expectedDll) { throw 'Release binary changed.' }
        [ordered]@{ path=$entry.FullName; bytes=$entry.Length; sha256=$hash }
    })
    $exporterFiles = @($files | Where-Object FullName -Like 'RearrangedS282/AssetStudioModCLI_net472_win64_contained/*').Count
    if ($exporterFiles -ne 13) { throw 'Unexpected exporter dependency count.' }
} finally { $zip.Dispose(); $oldZip.Dispose(); $sha.Dispose() }

$zipHash = (Get-FileHash -LiteralPath $zipPath).Hash
$manifest = [ordered]@{
    release='1.3.6 Beta 2'; version=$version; author='JimTaplin'; upstream='SatiricRug / Peter Schinske'
    createdUtc=[DateTime]::UtcNow.ToString('o'); zipFile=($releaseName+'.zip'); zipSHA256=$zipHash; dllSHA256=$expectedDll
    binaryMatchesInstalled=$true; binaryRebuilt=$false; packageValidated=$true; gameRuntimeValidated=$false
    rodPoseChecks=6720; rodScenarioRuns=56; deltaStandaloneChecks=129; diagnosticStandaloneChecks=42; arrangementPolicyChecks=1157
    earlier133StructuralPass='31 arrangements and six native oilcup bindings; does not validate 1.3.6 runtime'
    uploaded=$false; published=$false; fileCount=$inventory.Count; originalExporterFiles=$exporterFiles; files=$inventory
}
$manifest | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $releaseDirectory 'release-manifest.json') -Encoding utf8
"$zipHash  $releaseName.zip`r`n$expectedDll  RearrangedS282/RearrangedS282.dll" | Set-Content -LiteralPath (Join-Path $releaseDirectory 'CHECKSUMS.txt') -Encoding ascii
Copy-Item -LiteralPath (Join-Path $copyDirectory 'NEXUS_RELEASE_COPY.txt'),(Join-Path $copyDirectory 'TEST_STATUS.txt') -Destination $releaseDirectory
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
foreach ($file in Get-ChildItem -LiteralPath $releaseDirectory -File) { Copy-Item -LiteralPath $file.FullName -Destination $OutputDirectory }
if ((Get-FileHash -LiteralPath $desktopZip).Hash -ne $zipHash) { throw 'Desktop ZIP copy differs.' }
if ((Get-FileHash -LiteralPath $installed).Hash -ne $expectedDll) { throw 'Installed binary changed during packaging.' }
[ordered]@{zip=$desktopZip;version=$version;dllMatchesInstalled=$true;zipSHA256=$zipHash;fileCount=$inventory.Count;uploaded=$false} | ConvertTo-Json
