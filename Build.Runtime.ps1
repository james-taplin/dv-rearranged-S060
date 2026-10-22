param([string]$GamePath = '')
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Build.Config.ps1')
$config = Get-S060BuildConfig -GamePath $GamePath
$repo = $PSScriptRoot
$project = Join-Path $repo 'RearrangedS060\RearrangedS060.csproj'
$output = Join-Path $repo 'build\stage7'
New-Item -ItemType Directory -Path $output -Force | Out-Null
& $config.Dotnet build $project -c Release "-p:GamePath=$($config.GamePath)" --verbosity minimal 2>&1 |
    Tee-Object -FilePath (Join-Path $output 'build.log')
if ($LASTEXITCODE -ne 0) { throw 'Current runtime build failed.' }
$dll = Join-Path $repo 'RearrangedS060\bin\Release\net48\RearrangedS060.dll'
Copy-Item -LiteralPath $dll -Destination $output -Force
$gameAssemblies = @('Assembly-CSharp.dll','DV.Simulation.dll','UnityEngine.CoreModule.dll','UnityModManager\0Harmony.dll','UnityModManager\UnityModManager.dll')
[ordered]@{
    upstreamCommit = '65c8e8a'; configuration = 'Current S060 product (Stage7); no installation performed'
    utc = [DateTime]::UtcNow.ToString('o'); sdk = (& $config.Dotnet --version)
    output = Get-FileHash -LiteralPath (Join-Path $output 'RearrangedS060.dll') | Select-Object Path,Hash
    sources = @(Get-ChildItem (Join-Path $repo 'RearrangedS060') -Recurse -File -Filter '*.cs' |
        Where-Object FullName -NotMatch '\\(bin|obj)\\' | Get-FileHash | Select-Object Path,Hash)
    gameReferences = @($gameAssemblies | ForEach-Object {
        Get-FileHash -LiteralPath (Join-Path $config.GamePath "DerailValley_Data\Managed\$_") | Select-Object Path,Hash
    })
    meshSplitter = Get-FileHash -LiteralPath (Join-Path $config.GamePath 'Mods\LocoMeshSplitter\LocoMeshSplitter.dll') | Select-Object Path,Hash
    installed = $false; runtimeValidated = $false
} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'build-receipt.json') -Encoding UTF8
Write-Output "Built current runtime at $output. No game files installed."
