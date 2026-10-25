param([string]$Version = '')
$ErrorActionPreference = 'Stop'
$repo = $PSScriptRoot
if (-not $Version) { $Version = (Get-Content (Join-Path $repo 'Info.json') -Raw | ConvertFrom-Json).Version }
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid version.' }
if (-not (Get-Content (Join-Path $repo 'docs\nexus\CORE_README.txt') -Raw).Contains(" $Version ")) { throw 'Release documents must match the package version.' }
$tested = Join-Path $repo "build\articulated-$Version"
$receiptPath = Join-Path $tested 'package-receipt.json'
$receipt = Get-Content $receiptPath -Raw | ConvertFrom-Json
$native = Get-Content $receipt.runtimeReceipt -Raw | ConvertFrom-Json
if ($native.runtimeVersion -ne $Version -or -not $native.nativeRuntimeChecks.passed) { throw 'Missing tested runtime receipt.' }
foreach ($file in $receipt.files) {
    if ((Get-FileHash -LiteralPath $file.Path).Hash -ne $file.Hash) { throw "Tested package changed: $($file.Path)" }
}
foreach ($report in $native.nativeRuntimeChecks.reports) {
    if ((Get-FileHash -LiteralPath $report.path).Hash -ne $report.hash) { throw 'Native evidence changed.' }
}
$runtimeEntry = @($receipt.files | Where-Object { [IO.Path]::GetFileName($_.Path) -eq 'RearrangedS060.dll' })
if ($runtimeEntry.Count -ne 1 -or $runtimeEntry[0].Hash -ne $native.nativeRuntimeChecks.runtimeSha256) { throw 'Runtime/native evidence mismatch.' }
$out = Join-Path $repo "build\nexus-$Version-single"
$package = Join-Path $out 'package'
$stage = Join-Path $package 'RearrangedS060'
New-Item -ItemType Directory -Path $stage -Force | Out-Null
$info = Get-Content (Join-Path $repo 'Info.json') -Raw | ConvertFrom-Json
$oldInfo = Get-Content (Join-Path $tested 'package\RearrangedS060\Info.json') -Raw | ConvertFrom-Json
if ($info.Id -ne 'RearrangedS060' -or $info.Version -ne $Version -or $info.EntryMethod -ne $oldInfo.EntryMethod -or
    $info.Requirements -notcontains 'DVCustomCarLoader' -or $info.Requirements -notcontains 'LocoMeshSplitter') { throw 'Invalid combined-mod metadata.' }
Copy-Item -LiteralPath $runtimeEntry[0].Path -Destination $stage -Force
foreach ($name in @('Info.json','LICENSE','CREDITS.md')) { Copy-Item -LiteralPath (Join-Path $repo $name) -Destination $stage -Force }
Copy-Item -LiteralPath (Join-Path $repo 'docs\nexus\CORE_README.txt') -Destination (Join-Path $stage 'README.md') -Force
Copy-Item -LiteralPath (Join-Path $repo 'docs\nexus\INSTALL.txt') -Destination $stage -Force
$assets = @(
    @{ id='RS060_Meyer_0440'; directory='Meyer'; version='0.7.0'; readme='MEYER_README.txt' },
    @{ id='RS060_Franklin_Dockside_0440'; directory='Dockside'; version='0.7.0'; readme='DOCKSIDE_README.txt' }
)
$payload = @([ordered]@{ relative='RearrangedS060.dll'; hash=$runtimeEntry[0].Hash; kind='runtime' })
foreach ($asset in $assets) {
    $source = Join-Path $tested "package\$($asset.id)"
    $assetInfo = Get-Content (Join-Path $source 'Info.json') -Raw | ConvertFrom-Json
    if ($assetInfo.Id -ne $asset.id -or $assetInfo.Version -ne $asset.version) { throw 'Wrong asset identity/version.' }
    $target = Join-Path $stage "assets\$($asset.directory)"
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $source 'ccl_bundle') -Destination $target -Force
    Copy-Item -LiteralPath (Join-Path $repo "docs\nexus\$($asset.readme)") -Destination (Join-Path $target 'README.md') -Force
    $payload += [ordered]@{ relative="assets\$($asset.directory)\ccl_bundle"; hash=(Get-FileHash (Join-Path $source 'ccl_bundle')).Hash; kind=$asset.id }
}
foreach ($file in $payload) { if ((Get-FileHash (Join-Path $stage $file.relative)).Hash -ne $file.hash) { throw 'Runtime/asset payload changed.' } }
$expected = @('RearrangedS060.dll','Info.json','LICENSE','CREDITS.md','README.md','INSTALL.txt','assets\Meyer\ccl_bundle','assets\Meyer\README.md','assets\Dockside\ccl_bundle','assets\Dockside\README.md')
$actual = @(Get-ChildItem -LiteralPath $stage -Recurse -File | ForEach-Object { $_.FullName.Substring($stage.Length + 1) })
if (@(Compare-Object $expected $actual).Count -or @(Get-ChildItem $package -Force).Count -ne 1) { throw 'Unexpected files in the combined package.' }
$archive = Join-Path $out "RearrangedS060-$Version-UMM.zip"
Compress-Archive -LiteralPath $stage -DestinationPath $archive -Force
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($archive)
try {
    if (@($zip.Entries | Where-Object { $_.FullName -match '(?i)\.(zip|7z|rar)$' -or -not $_.FullName.StartsWith('RearrangedS060/') }).Count -or
        @($zip.Entries | Where-Object { $_.Name -eq 'Info.json' }).Count -ne 1) { throw 'Archive must have one mod root/identity and no nested archives.' }
} finally { $zip.Dispose() }
Copy-Item -LiteralPath (Join-Path $repo 'docs\nexus\NEXUS_RELEASE_COPY.txt') -Destination $out -Force
[ordered]@{
    utc=[DateTime]::UtcNow.ToString('o'); version=$Version; status='Public beta package; not published'
    testedPackageReceipt=$receiptPath; testedPackageReceiptSha256=(Get-FileHash $receiptPath).Hash
    runtimeSha256=$runtimeEntry[0].Hash; binaryPayloadUnchanged=$true; payload=$payload
    packaging='One UMM ZIP, one RearrangedS060 folder/Info.json, two recursively discovered native CCL bundles; no nested ZIPs'
    migration='Existing three-part installs: uninstall the two old standalone asset packs in UMM before installing; keep RearrangedS060 and its settings'
    sourceDocumentHashes=@(Get-ChildItem (Join-Path $repo 'docs\nexus') -File | Get-FileHash | Select-Object Path,Hash)
    files=@(Get-ChildItem $stage -Recurse -File | Get-FileHash | Select-Object Path,Hash)
    zip=Get-FileHash -LiteralPath $archive | Select-Object Path,Hash
    installed=$false; published=$false; installerValidationCommand='Windows PowerShell: tests/Nexus/NativeInstaller.ps1'
} | ConvertTo-Json -Depth 7 | Set-Content (Join-Path $out 'nexus-package-receipt.json') -Encoding UTF8
Write-Output "Single UMM release ZIP staged: $archive"
