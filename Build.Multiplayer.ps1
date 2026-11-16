param(
    [string]$GamePath = 'B:\SteamLibrary\steamapps\common\Derail Valley',
    [string]$MultiplayerApiPath = 'B:\GitHub repo\dads-decals\lib\MultiplayerAPI.dll',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'build\release-0.3.6-mp-oil-rods-20261116\runtime')
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $MultiplayerApiPath)) { throw 'Provide the API 1.1 assembly supplied by DV Multiplayer 0.1.16.0-beta.' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
& 'C:\Program Files\dotnet\dotnet.exe' build (Join-Path $PSScriptRoot 'RearrangedS060.Multiplayer\RearrangedS060.Multiplayer.csproj') -c Release "-p:GamePath=$GamePath" "-p:MultiplayerApiPath=$MultiplayerApiPath" -o $OutputDirectory --verbosity minimal 2>&1 | Tee-Object (Join-Path $OutputDirectory 'build.log')
if ($LASTEXITCODE -ne 0) { throw 'S060 core/optional multiplayer adapter build failed.' }
[ordered]@{
    version=(Get-Content (Join-Path $PSScriptRoot 'Info.json') -Raw | ConvertFrom-Json).Version
    timeUtc=[DateTime]::UtcNow.ToString('o'); installed=$false; gameLaunched=$false
    binaries=@('RearrangedS060.dll','RearrangedS060.Multiplayer.dll') | ForEach-Object { Get-FileHash (Join-Path $OutputDirectory $_) | Select-Object Path,Hash }
    api=Get-FileHash $MultiplayerApiPath | Select-Object Path,Hash
    sources=@(Get-ChildItem (Join-Path $PSScriptRoot 'RearrangedS060'),(Join-Path $PSScriptRoot 'RearrangedS060.Multiplayer') -File | Where-Object Extension -In '.cs','.csproj' | Get-FileHash | Select-Object Path,Hash)
    authoring='Verified 0.3.5 bundles reused, including baked Dockside sleeves, without rebuild; see Package.Multiplayer.ps1.'
} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $OutputDirectory 'build-receipt.json') -Encoding utf8
