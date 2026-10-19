param([string]$GamePath = 'B:\SteamLibrary\steamapps\common\Derail Valley', [ValidateSet(1,2,3)][int]$Stage = 1)
$ErrorActionPreference = 'Stop'
$sdk = 'C:\Program Files\dotnet\dotnet.exe'
$project = Join-Path $PSScriptRoot 'RearrangedS282\RearrangedS282.csproj'
$output = Join-Path $PSScriptRoot "build\stage$Stage"
New-Item -ItemType Directory -Path $output -Force | Out-Null
$argsForBuild = @($project, '-c', 'Release', "-p:GamePath=$GamePath", "-p:Stage=$Stage", '--verbosity', 'minimal')
& $sdk build @argsForBuild 2>&1 | Tee-Object -FilePath (Join-Path $output 'build.log')
if ($LASTEXITCODE -ne 0) { throw "Stage $Stage build failed." }
$dll = Join-Path $PSScriptRoot 'RearrangedS282\bin\Release\net48\RearrangedS282.dll'
Copy-Item -LiteralPath $dll -Destination $output -Force
$gameAssemblies = @('Assembly-CSharp.dll', 'DV.Simulation.dll', 'UnityEngine.CoreModule.dll', 'UnityModManager\0Harmony.dll', 'UnityModManager\UnityModManager.dll')
$receipt = [ordered]@{
    upstreamCommit = '24e24d5'
    configuration = "Stage $Stage development build; no installation performed"
    utc = [DateTime]::UtcNow.ToString('o')
    sdk = (& $sdk --version)
    output = Get-FileHash -LiteralPath (Join-Path $output 'RearrangedS282.dll') -Algorithm SHA256 | Select-Object Path, Hash
    sources = @(Get-ChildItem (Join-Path $PSScriptRoot 'RearrangedS282') -Recurse -File -Filter '*.cs' |
        Where-Object FullName -NotMatch '\\(bin|obj)\\' | Get-FileHash -Algorithm SHA256 | Select-Object Path, Hash)
    gameReferences = @($gameAssemblies | ForEach-Object {
        Get-FileHash -LiteralPath (Join-Path $GamePath "DerailValley_Data\Managed\$_") -Algorithm SHA256 | Select-Object Path, Hash
    })
    meshSplitter = Get-FileHash -LiteralPath (Join-Path $GamePath 'Mods\LocoMeshSplitter\LocoMeshSplitter.dll') -Algorithm SHA256 | Select-Object Path, Hash
    installed = $false
    runtimeValidated = $false
}
$receipt | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'build-receipt.json') -Encoding utf8
Write-Output "Built stage $Stage at $output. No game files installed."
