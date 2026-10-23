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
$out = Join-Path $repo "build\nexus-$Version"
$stage = Join-Path $out 'mods'
$download = Join-Path $out 'download'
New-Item -ItemType Directory -Path $download -Force | Out-Null
$entries = @(
    @{ id='RearrangedS060'; version=$Version; prefix='01'; readme='CORE_README.txt'; payload=@('RearrangedS060.dll','Info.json') },
    @{ id='RS060_Meyer_0440'; version='0.7.0'; prefix='02'; readme='MEYER_README.txt'; payload=@('ccl_bundle','Info.json') },
    @{ id='RS060_Franklin_Dockside_0440'; version='0.7.0'; prefix='03'; readme='DOCKSIDE_README.txt'; payload=@('ccl_bundle','Info.json') }
)
$inner = @()
foreach ($entry in $entries) {
    $source = Join-Path $tested "package\$($entry.id)"
    $info = Get-Content (Join-Path $source 'Info.json') -Raw | ConvertFrom-Json
    if ($info.Id -ne $entry.id -or $info.Version -ne $entry.version) { throw 'Wrong mod identity/version.' }
    $target = Join-Path $stage $entry.id
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    foreach ($name in $entry.payload) { Copy-Item -LiteralPath (Join-Path $source $name) -Destination $target -Force }
    Copy-Item -LiteralPath (Join-Path $repo "docs\nexus\$($entry.readme)") -Destination (Join-Path $target 'README.md') -Force
    foreach ($name in @('LICENSE','CREDITS.md')) { Copy-Item -LiteralPath (Join-Path $repo $name) -Destination $target -Force }
    $expected = @($entry.payload) + @('README.md','LICENSE','CREDITS.md')
    $actual = @(Get-ChildItem -LiteralPath $target -Recurse -File | ForEach-Object { $_.FullName.Substring($target.Length + 1) })
    if (@(Compare-Object $expected $actual).Count) { throw "Unexpected Nexus files for $($entry.id)" }
    $archive = Join-Path $download "$($entry.prefix)-$($entry.id)-$($entry.version).zip"
    Compress-Archive -LiteralPath $target -DestinationPath $archive -Force
    $inner += Get-FileHash -LiteralPath $archive | Select-Object Path,Hash
}
Copy-Item -LiteralPath (Join-Path $repo 'docs\nexus\INSTALL.txt') -Destination $download -Force
$downloadNames = @($inner | ForEach-Object { [IO.Path]::GetFileName($_.Path) }) + @('INSTALL.txt')
if (@(Compare-Object $downloadNames @(Get-ChildItem -LiteralPath $download -Force | ForEach-Object Name)).Count) { throw 'Unexpected download entries.' }
$archive = Join-Path $out "RearrangedS060-$Version-Initial-Beta-UMM-Bundle.zip"
Compress-Archive -LiteralPath @(Get-ChildItem -LiteralPath $download -File | ForEach-Object FullName) -DestinationPath $archive -Force
Copy-Item -LiteralPath (Join-Path $repo 'docs\nexus\NEXUS_RELEASE_COPY.txt') -Destination $out -Force
[ordered]@{
    utc=[DateTime]::UtcNow.ToString('o'); version=$Version; status='Draft initial public beta; not published'
    testedPackageReceipt=$receiptPath; testedPackageReceiptSha256=(Get-FileHash $receiptPath).Hash
    runtimeSha256=$runtimeEntry[0].Hash; binaryPayloadUnchanged=$true
    packaging='Outer download contains three individually UMM-installable archives; extract before using UMM'
    sourceDocumentHashes=@(Get-ChildItem (Join-Path $repo 'docs\nexus') -File | Get-FileHash | Select-Object Path,Hash)
    innerArchives=$inner; files=@(Get-ChildItem $stage -Recurse -File | Get-FileHash | Select-Object Path,Hash)
    zip=Get-FileHash -LiteralPath $archive | Select-Object Path,Hash
    installed=$false; published=$false; installerValidationCommand='Windows PowerShell: tests/Nexus/NativeInstaller.ps1'
} | ConvertTo-Json -Depth 7 | Set-Content (Join-Path $out 'nexus-package-receipt.json') -Encoding UTF8
Write-Output "Draft Nexus archive staged: $archive"
