param([Parameter(Mandatory)][string]$MeyerEvidence, [Parameter(Mandatory)][string]$DocksideEvidence)
$ErrorActionPreference = 'Stop'
$repo = $PSScriptRoot
. (Join-Path $repo 'Build.Config.ps1')
$config = Get-S060BuildConfig
$coreFiles = @('RearrangedS060.dll','Info.json','LICENSE','README.md','CREDITS.md')
$packFiles = @('ccl_bundle','Info.json','README.md')
$packIds = @('RearrangedS060','RS060_Meyer_0440','RS060_Franklin_Dockside_0440')
function Assert-StageFiles([string]$Path, [string[]]$Names, [switch]$AllowMissing) {
    if (-not (Test-Path -LiteralPath $Path)) { if ($AllowMissing) { return }; throw "Missing staging folder: $Path" }
    $entries = @(Get-ChildItem -LiteralPath $Path -Force)
    if (@($entries | Where-Object { $_.PSIsContainer -or $_.Name -notin $Names }).Count) { throw "Unexpected staging entries: $Path" }
    if (-not $AllowMissing -and $entries.Count -ne $Names.Count) { throw "Incomplete staging manifest: $Path" }
}
$output = Join-Path $repo ("build\articulated-$($config.Version)")
$stage = Join-Path $output 'package'
$runtime = Join-Path $repo ("build\radio-$($config.Version)\package\RearrangedS060")
Assert-StageFiles $runtime $coreFiles
if (Test-Path -LiteralPath $stage) {
    if (@(Get-ChildItem -LiteralPath $stage -Force | Where-Object { -not $_.PSIsContainer -or $_.Name -notin $packIds }).Count) { throw 'Unexpected complete-package directories or files.' }
    foreach ($id in $packIds) { Assert-StageFiles (Join-Path $stage $id) $(if ($id -eq 'RearrangedS060') { $coreFiles } else { $packFiles }) -AllowMissing }
}
$runtimeReceipt = Get-Content (Join-Path $repo ("build\radio-$($config.Version)\package-receipt.json")) -Raw | ConvertFrom-Json
if ($runtimeReceipt.runtimeVersion -ne $config.Version -or @($runtimeReceipt.files).Count -ne $coreFiles.Count) { throw 'Wrong or incomplete core validation receipt.' }
$runtimeInfo = Get-Content (Join-Path $runtime 'Info.json') -Raw | ConvertFrom-Json
if ($runtimeInfo.Id -ne 'RearrangedS060' -or $runtimeInfo.Version -ne $config.Version) { throw 'Wrong core package metadata.' }
foreach ($name in $coreFiles) {
    $expected = [IO.Path]::GetFullPath((Join-Path $runtime $name))
    if (@($runtimeReceipt.files | Where-Object { [IO.Path]::GetFullPath($_.Path) -eq $expected }).Count -ne 1) { throw "Missing/duplicate canonical core receipt entry: $name" }
}
$builtHash = ($runtimeReceipt.files | Where-Object { [IO.Path]::GetFileName($_.Path) -eq 'RearrangedS060.dll' }).Hash
if ((Get-FileHash (Join-Path $runtime 'RearrangedS060.dll')).Hash -ne $builtHash) { throw 'Runtime differs from its test receipt.' }
foreach ($file in $runtimeReceipt.files) { if ((Get-FileHash $file.Path).Hash -ne $file.Hash) { throw 'Core file differs from its validation receipt.' } }
New-Item -ItemType Directory -Path $stage -Force | Out-Null
Copy-Item -LiteralPath $runtime -Destination $stage -Recurse -Force
foreach ($file in $runtimeReceipt.files) {
    $copied = Join-Path (Join-Path $stage 'RearrangedS060') ([IO.Path]::GetFileName($file.Path))
    if ((Get-FileHash -LiteralPath $copied).Hash -ne $file.Hash) { throw 'Copied core differs from its validation receipt.' }
}
$sdk = $config.Dotnet
$simDll = Get-ChildItem (Join-Path $repo 'RearrangedS060\obj\Release\net48\PublicizedAssemblies') -Recurse -Filter DV.Simulation.dll | Select-Object -First 1 -ExpandProperty FullName
$checks = @()
foreach ($entry in @(
    @{ id='RS060_Meyer_0440'; version='0.7.0'; evidence=$MeyerEvidence; readme='Meyer\README.md'; test='Meyer'; extra=@('-p:BaseIntermediateOutputPath=obj/native/') },
    @{ id='RS060_Franklin_Dockside_0440'; version='0.7.0'; evidence=$DocksideEvidence; readme='Dockside\README.md'; test='Dockside'; extra=@() }
)) {
    & (Join-Path $repo 'Test.BundleEvidence.ps1') -Evidence $entry.evidence
    $reports = @('bundle-validation.json','motion-validation.json','steam-pipe-validation.json','oilcup-geometry-validation.json','normal-rod-validation.json','native-pin-fit-validation.json','steam-effects-validation.json')
    if ($entry.id -eq 'RS060_Meyer_0440') { $reports += 'meyer-lights-validation.json' }
    foreach ($report in $reports) {
        if (-not (Get-Content (Join-Path $entry.evidence $report) -Raw | ConvertFrom-Json).passed) { throw "Failed $report in $($entry.evidence)" }
    }
    $source = Join-Path $entry.evidence $entry.id
    $info = Get-Content (Join-Path $source 'Info.json') -Raw | ConvertFrom-Json
    if ($info.Version -ne $entry.version -or $info.Id -ne $entry.id) { throw 'Unexpected asset version/identity.' }
    $target = Join-Path $stage $entry.id
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    foreach ($file in @('ccl_bundle','Info.json')) { Copy-Item -LiteralPath (Join-Path $source $file) -Destination $target -Force }
    Copy-Item -LiteralPath (Join-Path $repo $entry.readme) -Destination (Join-Path $target 'README.md') -Force
    $testOutput = & $sdk run --project (Join-Path $repo "tests\$($entry.test)\Native.csproj") -c Release "-p:PublicizedSimulation=$simDll" "-p:ManagedPath=$(Join-Path $config.GamePath 'DerailValley_Data\Managed')" @($entry.extra) -- $entry.evidence 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Native calibration test failed: $testOutput" }
    $testOutput | Set-Content (Join-Path $output "$($entry.test)-native-checks.txt")
    $checks += [string]$testOutput
    Assert-StageFiles $target $packFiles
}
Assert-StageFiles (Join-Path $stage 'RearrangedS060') $coreFiles
if (@(Get-ChildItem -LiteralPath $stage -Force).Count -ne $packIds.Count) { throw 'Complete package must contain exactly three canonical mod folders.' }
$zip = Join-Path $output ("RearrangedS060-$($config.Version)-articulated-test.zip")
Compress-Archive -LiteralPath @((Join-Path $stage 'RearrangedS060'),(Join-Path $stage 'RS060_Meyer_0440'),(Join-Path $stage 'RS060_Franklin_Dockside_0440')) -DestinationPath $zip -Force
[ordered]@{
    utc=[DateTime]::UtcNow.ToString('o'); installed=$false; runtimeValidated=$false
    runtimeReceipt=(Join-Path $repo ("build\radio-$($config.Version)\package-receipt.json")); nativeChecks=$checks
    meyerEvidence=$MeyerEvidence; docksideEvidence=$DocksideEvidence
    files=@(Get-ChildItem $stage -Recurse -File | Get-FileHash | Select-Object Path,Hash)
    zip=Get-FileHash -LiteralPath $zip | Select-Object Path,Hash
} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'package-receipt.json') -Encoding UTF8
Write-Output $checks
Write-Output "Staged $zip. No installation performed."


