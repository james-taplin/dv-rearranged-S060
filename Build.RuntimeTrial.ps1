param(
    [string]$Version = '1.10.0',
    [string]$GamePath = 'B:\SteamLibrary\steamapps\common\Derail Valley',
    [string]$OutputDirectory,
    [string]$CandidateDirectory,
    [switch]$Install
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$candidate = if ($CandidateDirectory) { $CandidateDirectory } else { Join-Path $root "build\runtime-trial-$Version" }
if (-not $OutputDirectory) { $OutputDirectory = "C:\Users\james\Desktop\Derail Valley Mods\RearrangedS282-B99-$Version-Test" }
$bin = Join-Path $candidate 'bin'
$seed = Join-Path $root 'release\1.3.3-Beta1\RearrangedS282-B99-1.3.3-Beta1.zip'
if ((Get-FileHash -LiteralPath $seed).Hash -ne '4A8F1660AAFA1A03A2607B843AB20FB6DE7C625FFD08A94369767DBD8558FCBD') { throw 'Audited package baseline changed.' }
& 'C:\Program Files\dotnet\dotnet.exe' build (Join-Path $root 'RearrangedS282\RearrangedS282.csproj') -c Release '-p:Stage=3' "-p:Version=$Version" "-p:GamePath=$GamePath" -o $bin --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Runtime trial build failed.' }
$staging = Join-Path $candidate ('package-' + [Guid]::NewGuid().ToString('N'))
Expand-Archive -LiteralPath $seed -DestinationPath $staging
$mod = Join-Path $staging 'RearrangedS282'
Copy-Item -LiteralPath (Join-Path $bin 'RearrangedS282.dll') -Destination $mod -Force
$infoPath = Join-Path $mod 'Info.json'
$info = Get-Content -LiteralPath $infoPath -Raw | ConvertFrom-Json
$info.Version = $Version
$info.DisplayName = 'Rearranged S282 - B99 Expanded Trial'
$info.LoadAfter = @('Gauge','DVCarChanger','S282ModelTweaks','MultiCylinderChuffFix')
$info | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $infoPath -Encoding utf8
Copy-Item -LiteralPath (Join-Path $root 'RUNTIME_TRIAL_TESTING.txt') -Destination $mod
Copy-Item -LiteralPath (Join-Path $root 'RUNTIME_TRIAL_TESTING.txt') -Destination (Join-Path $mod 'README_BETA.txt') -Force
Copy-Item -LiteralPath (Join-Path $root 'RUNTIME_TRIAL_TESTING.txt') -Destination (Join-Path $mod 'MANUAL_TEST_CHECKLIST.txt') -Force
Copy-Item -LiteralPath (Join-Path $root 'ARTICULATED_TEST_STATUS.txt') -Destination $mod
$bundle = Join-Path $root 's282_drivetrain\ExportedProject\Assets\AssetBundles\s282_drivetrain'
$bundleHash = '44A1B9EAE0DDA58BE97B9AEDF3877522FBFFAEC060FE02A8385158E095BDB22D'
if ((Get-FileHash -LiteralPath $bundle).Hash -ne $bundleHash) { throw 'Original articulated drivetrain bundle differs from the assessed asset.' }
New-Item -ItemType Directory -Path (Join-Path $mod 'assets') -Force | Out-Null
Copy-Item -LiteralPath $bundle -Destination (Join-Path $mod 'assets\s282_drivetrain')
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$zip = Join-Path $OutputDirectory "RearrangedS282-B99-$Version-Test.zip"
Compress-Archive -LiteralPath $mod -DestinationPath $zip -Force
Copy-Item -LiteralPath (Join-Path $root 'RUNTIME_TRIAL_TESTING.txt') -Destination $OutputDirectory -Force
$receipt = [ordered]@{
    version=$Version; utc=[DateTime]::UtcNow.ToString('O'); installed=$false; runtimeValidated=$false
    dllSHA256=(Get-FileHash -LiteralPath (Join-Path $mod 'RearrangedS282.dll')).Hash
    zipSHA256=(Get-FileHash -LiteralPath $zip).Hash; package=$mod; zip=$zip
    bundleSHA256=$bundleHash
    features=@('40 choices: 34 conventional (incl. 4-14-4), 4 articulated (incl. 2-6-6-2 Mallet, 2-2-2-2-2 triplex), unpowered 4-0-4 and experimental giant-driver 2-2-2','Pacific/Hudson Walschaerts geometry alignment with native animation','New 2-6-0 Mogul and 2-4-0','Adjustable 2.1-3.6m tread-diameter collider-free 2-2-2 clearance visuals','Native explosion materials for added and inactive running gear','Native DV pin/aim constraints and bogies','Two native steam engines with boiler/torque/exhaust accounting','Per-engine diagnostics including failed native updates','Native rolling-audio axle-cache refresh','Optional random spawns and saved arrangements','LateUpdate rod synchronization','Delta trailing truck compatibility')
    sourceHashes=@(Get-ChildItem (Join-Path $root 'RearrangedS282') -Recurse -File -Filter '*.cs' | Where-Object FullName -NotMatch '\\(bin|obj)\\' | Get-FileHash | Select-Object Path,Hash)
}
if ($Install) {
    if (Get-Process -Name DerailValley -ErrorAction SilentlyContinue) { throw 'Close Derail Valley before installing the prepared test ZIP.' }
    $destination = Join-Path $GamePath 'Mods\RearrangedS282'
    $backup = Join-Path $candidate ('installed-backup-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    $absoluteMod = [IO.Path]::GetFullPath($mod).TrimEnd('\') + '\'
    $absoluteDestination = [IO.Path]::GetFullPath($destination).TrimEnd('\') + '\'
    foreach ($file in Get-ChildItem -LiteralPath $mod -File -Recurse) {
        $relative = $file.FullName.Substring($absoluteMod.Length)
        $target = [IO.Path]::GetFullPath((Join-Path $destination $relative))
        if (-not $target.StartsWith($absoluteDestination,[StringComparison]::OrdinalIgnoreCase)) { throw 'Installation path escaped the named mod folder.' }
        if (Test-Path -LiteralPath $target) {
            $saved = Join-Path $backup $relative
            New-Item -ItemType Directory -Path (Split-Path $saved -Parent) -Force | Out-Null
            Copy-Item -LiteralPath $target -Destination $saved
        }
        New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
    }
    if ((Get-FileHash -LiteralPath (Join-Path $destination 'RearrangedS282.dll')).Hash -ne $receipt.dllSHA256) { throw 'Installed DLL hash differs from candidate.' }
    if ((Get-FileHash -LiteralPath (Join-Path $destination 'assets\s282_drivetrain')).Hash -ne $bundleHash) { throw 'Installed articulated bundle hash differs.' }
    $receipt.installed=$true
    $receipt.backup=$backup
    $receipt.installedPath=$destination
}
$receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $candidate 'build-receipt.json') -Encoding utf8
$receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'build-receipt.json') -Encoding utf8
Write-Output ([ordered]@{version=$Version;installed=$receipt.installed;dllSHA256=$receipt.dllSHA256;zip=$zip;backup=$receipt.backup} | ConvertTo-Json)
