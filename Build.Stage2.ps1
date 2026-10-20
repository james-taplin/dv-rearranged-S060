param([string]$GamePath = 'B:\SteamLibrary\steamapps\common\Derail Valley')
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Build.Stage1.ps1') -GamePath $GamePath -Stage 2
$out = Join-Path $PSScriptRoot 'build\stage2'
$info = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Info.json') -Raw | ConvertFrom-Json
$info.DisplayName = 'Rearranged S060 - B99 Stage 2 Trial'
$info | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $out 'Info.json') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LICENSE') -Destination $out -Force
$package = Join-Path $out 'package\RearrangedS060'
New-Item -ItemType Directory -Path $package -Force | Out-Null
foreach ($name in @('RearrangedS060.dll','Info.json','LICENSE')) {
    Copy-Item -LiteralPath (Join-Path $out $name) -Destination $package -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'STAGE2_TESTING.md') -Destination $package -Force
Compress-Archive -LiteralPath $package -DestinationPath (Join-Path $PSScriptRoot 'build\RearrangedS060-B99-stage2-trial.zip') -Force
Write-Output 'Created a development trial ZIP; no installation or publication performed.'
