param([switch]$Force)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Build.Config.ps1')
$config = Get-S060BuildConfig
$scripts = @('Meyer\tools\Prepare.GeometryEvidence.py','Meyer\tools\Prepare.OilCupFixtures.py','Meyer\tools\Prepare.MeyerLightsEvidence.py','Dockside\tools\Prepare.WheelComparison.py')
$inputs = @((Join-Path $config.GamePath 'DerailValley_Data\resources.assets')) + @($scripts | ForEach-Object { Join-Path $PSScriptRoot $_ })
$hashes = @($inputs | Get-FileHash | Select-Object Path,Hash)
$receiptPath = Join-Path $config.QAPath 'fixture-receipt.json'
if (-not $Force -and (Test-Path $receiptPath)) {
    $old = Get-Content $receiptPath -Raw | ConvertFrom-Json
    $same = ($hashes | ConvertTo-Json -Compress) -eq ($old.inputs | ConvertTo-Json -Compress)
    foreach ($file in $old.files) { if (-not (Test-Path $file.Path) -or (Get-FileHash $file.Path).Hash -ne $file.Hash) { $same = $false } }
    if ($same) { Write-Output "Native QA fixtures verified: $($config.QAPath)"; return }
}
New-Item -ItemType Directory -Path $config.QAPath -Force | Out-Null
foreach ($script in $scripts) {
    & $config.Python (Join-Path $PSScriptRoot $script) --output $config.QAPath --game $config.GamePath
    if ($LASTEXITCODE -ne 0) { throw "Native QA preparation failed: $script" }
}
$outputs = @('native-preview.json','native-preview-atlas.png','native-oilcups.json','meyer-lights-native.json','s282-wheels-comparison.json')
[ordered]@{ inputs=$hashes; nativeAssetsPackaged=$false; files=@($outputs | ForEach-Object { Get-FileHash (Join-Path $config.QAPath $_) } | Select-Object Path,Hash) } |
    ConvertTo-Json -Depth 5 | Set-Content $receiptPath -Encoding UTF8
Write-Output "Prepared local-only native QA fixtures: $($config.QAPath)"
