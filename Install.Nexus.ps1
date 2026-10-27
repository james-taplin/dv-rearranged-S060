param([string]$GamePath='', [string]$Version='')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Build.Config.ps1')
$config=Get-S060BuildConfig -GamePath $GamePath
if(-not $Version){$Version=$config.Version}
if($Version -notmatch '^\d+\.\d+\.\d+$'){throw 'Invalid version.'}
if(Get-Process -Name DerailValley -ErrorAction SilentlyContinue){throw 'Close Derail Valley before installation.'}
$output=Join-Path $PSScriptRoot "build\nexus-$Version-single"
$receiptPath=Join-Path $output 'nexus-package-receipt.json'
$receipt=Get-Content $receiptPath -Raw | ConvertFrom-Json
$proof=Get-ChildItem $output -Directory -Filter 'umm-proof-*' | Where-Object {Test-Path (Join-Path $_.FullName 'native-umm-validation.json')} | Sort-Object Name -Descending | Select-Object -First 1
$native=Get-Content (Join-Path $proof.FullName 'native-umm-validation.json') -Raw | ConvertFrom-Json
if(-not $native.passed -or $native.packageReceiptSha256 -ne (Get-FileHash $receiptPath).Hash){throw 'Exact-package native UMM validation required.'}
$bundleProof=Get-Content (Join-Path $output 'combined-bundle-validation.json') -Raw | ConvertFrom-Json
if(-not $bundleProof.passed -or -not $bundleProof.shayModelsAvailable){throw 'Exact extracted bundles must pass Unity reopening checks.'}
if($bundleProof.shayBundleSha256 -ne ($receipt.payload | Where-Object relative -eq 'assets\Shay\shay_running_gear').hash){throw 'Shay bundle proof mismatch.'}
$stage=[IO.Path]::GetFullPath((Join-Path $output 'package'))
$mods=[IO.Path]::GetFullPath((Join-Path $config.GamePath 'Mods'))
$target=Join-Path $mods 'RearrangedS060'
$info=Get-Content (Join-Path $stage 'RearrangedS060\Info.json') -Raw | ConvertFrom-Json
if($info.Id -ne 'RearrangedS060' -or $info.Version -ne $Version){throw 'Package identity mismatch.'}
foreach($file in $receipt.files){
    $path=[IO.Path]::GetFullPath($file.Path)
    if(-not $path.StartsWith($stage+'\RearrangedS060\',[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($path) -eq 'Settings.xml'){throw 'Unexpected package file.'}
    if((Get-FileHash $path).Hash -ne $file.Hash){throw 'Package changed after validation.'}
}
# Native CCL scans recursively. Remove only verified previous donor folders;
# preserve the main Settings.xml, diagnostics and every unrelated mod.
$legacy=@()
foreach($id in @('RS060_Meyer_0440','RS060_Franklin_Dockside_0440')){
    $path=[IO.Path]::GetFullPath((Join-Path $mods $id))
    if(Test-Path -LiteralPath $path){
        if([IO.Path]::GetDirectoryName($path) -ne $mods -or (Get-Content (Join-Path $path 'Info.json') -Raw | ConvertFrom-Json).Id -ne $id){throw 'Legacy mod identity/path mismatch.'}
        if(@(Get-ChildItem -LiteralPath $path -Force | Where-Object {$_.PSIsContainer -or $_.Name -notin @('Info.json','README.md','LICENSE','ccl_bundle')}).Count){throw 'Legacy pack has unrecognised files; migration cannot delete it.'}
        $legacy+=$path
    }
}
$protected=@(Join-Path $target 'Settings.xml')+@(Get-ChildItem -LiteralPath $mods -Directory | Where-Object {$_.Name -notin @('RearrangedS060','RS060_Meyer_0440','RS060_Franklin_Dockside_0440')} | ForEach-Object {Get-ChildItem -LiteralPath $_.FullName -File -Filter 'Info.json'})
$before=@($protected | Where-Object {Test-Path -LiteralPath $_} | Get-FileHash | Select-Object Path,Hash)
$record=[ordered]@{utc=[DateTime]::UtcNow.ToString('o');packageReceipt=$receiptPath;gameClosed=$true;backupCreated=$false;legacyRemoved=$legacy;protected=$before;status='preflight verified'}
$record | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'install-receipt-latest.json') -Encoding UTF8
if(Get-Process -Name DerailValley -ErrorAction SilentlyContinue){throw 'Game started during preparation; installation cancelled.'}
foreach($file in $receipt.files){
    $destination=Join-Path $mods $file.Path.Substring($stage.Length+1)
    New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
    Copy-Item -LiteralPath $file.Path -Destination $destination -Force
    if((Get-FileHash $destination).Hash -ne $file.Hash){throw 'Installed bytes failed verification.'}
}
# Every recursive target above has been resolved, bounded and identified before
# mutation. The replacement bytes are verified before removing the old aliases.
foreach($path in $legacy){Remove-Item -LiteralPath $path -Recurse -Force}
foreach($cache in Get-ChildItem -LiteralPath $target -File -Filter 'RearrangedS060.dll.*.cache'){
    if([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($cache.FullName)) -ne $target){throw 'Cache escaped runtime folder.'}
    Remove-Item -LiteralPath $cache.FullName
}
foreach($file in $before){if((Get-FileHash $file.Path).Hash -ne $file.Hash){throw 'Protected file changed.'}}
$record.status='installed and hash verified';$record['installedVersion']=$Version;$record['gameplayValidated']=$false
$record | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'install-receipt-latest.json') -Encoding UTF8
Write-Output "Installed single-folder S060 $Version, no backup; settings preserved."
