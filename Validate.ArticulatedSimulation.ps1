param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'build\articulated-simulation-validation'))
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$managed = 'B:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\Managed'
$publicized = Get-ChildItem -LiteralPath (Join-Path $root 'RearrangedS282\obj\Release\net48\PublicizedAssemblies') -Recurse -File -Filter 'DV.Simulation.dll' | Select-Object -First 1 -ExpandProperty FullName
if (-not $publicized) { throw 'Run the native compilation first to create its build-local publicized reference.' }
$projectDirectory = Join-Path $OutputDirectory 'native-probe'
New-Item -ItemType Directory -Path $projectDirectory -Force | Out-Null
$projectDirectory = (Resolve-Path -LiteralPath $projectDirectory).Path
$runDirectory = Join-Path $OutputDirectory ('native-run-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
$sourcePath = Join-Path $root 'RearrangedS282\ArticulatedSimulation.cs'
$source = Get-Content -LiteralPath $sourcePath -Raw
$start = $source.IndexOf('    internal sealed class ArticulatedRearEngineDefinition')
$end = $source.LastIndexOf('#endif')
if ($start -lt 0 -or $end -lt $start) { throw 'Actual production native-engine gate section was not found.' }
# Compile the exact production subtype bodies, with the real native engine,
# definitions, ports and Unity's own managed math. No native physics stand-ins.
$gate = 'using LocoSim.Definitions; using LocoSim.Implementations; namespace RearrangedS282 {' + [Environment]::NewLine + $source.Substring($start, $end - $start)
$gate | Set-Content -LiteralPath (Join-Path $projectDirectory 'ActualEngineGate.cs') -Encoding utf8
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NuGetAudit>false</NuGetAudit><RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources><NoWarn>SYSLIB0050</NoWarn></PropertyGroup>
  <ItemGroup>
    <Compile Include="$projectDirectory\ActualEngineGate.cs" />
    <Compile Include="$root\evidence\articulated-simulation-tests\NativeProbe.cs" />
    <Reference Include="DV.Simulation"><HintPath>$publicized</HintPath></Reference>
    <Reference Include="DV.Utils"><HintPath>$managed\DV.Utils.dll</HintPath></Reference>
    <Reference Include="UnityEngine"><HintPath>$managed\UnityEngine.dll</HintPath></Reference>
    <Reference Include="UnityEngine.CoreModule"><HintPath>$managed\UnityEngine.CoreModule.dll</HintPath></Reference>
    <Reference Include="Newtonsoft.Json"><HintPath>$managed\Newtonsoft.Json.dll</HintPath></Reference>
  </ItemGroup>
</Project>
"@
$projectPath = Join-Path $projectDirectory 'NativeArticulated.Validation.csproj'
$project | Set-Content -LiteralPath $projectPath -Encoding utf8
& 'C:\Program Files\dotnet\dotnet.exe' run --project $projectPath --configuration Release -- $runDirectory (Join-Path $root 'build\b99-power-inspection\ReciprocatingSteamEngineDefinition.json')
if ($LASTEXITCODE -ne 0) { throw 'Actual native articulated engine validation failed.' }
$reportPath = Join-Path $runDirectory 'validation.json'
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
$report | Add-Member -NotePropertyName sourceSHA256 -NotePropertyValue (Get-FileHash -LiteralPath $sourcePath).Hash
$report | Add-Member -NotePropertyName nativeSimulationSHA256 -NotePropertyValue (Get-FileHash -LiteralPath (Join-Path $managed 'DV.Simulation.dll')).Hash
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding utf8
Get-Content -LiteralPath $reportPath -Raw
$graphDirectory = Join-Path $OutputDirectory 'graph-probe'
New-Item -ItemType Directory -Path $graphDirectory -Force | Out-Null
$graphDirectory = (Resolve-Path -LiteralPath $graphDirectory).Path
$graphProject = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><DefineConstants>B99_ALL_STANDARD</DefineConstants><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NuGetAudit>false</NuGetAudit><RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources></PropertyGroup>
  <ItemGroup>
    <Compile Include="$sourcePath" />
    <Compile Include="$root\evidence\articulated-simulation-tests\GraphStubs.cs" />
    <Compile Include="$root\evidence\articulated-simulation-tests\GraphProbe.cs" />
    <Reference Include="Newtonsoft.Json"><HintPath>$managed\Newtonsoft.Json.dll</HintPath></Reference>
  </ItemGroup>
</Project>
"@
$graphProjectPath = Join-Path $graphDirectory 'GraphArticulated.Validation.csproj'
$graphProject | Set-Content -LiteralPath $graphProjectPath -Encoding utf8
& 'C:\Program Files\dotnet\dotnet.exe' run --project $graphProjectPath --configuration Release -- $runDirectory
if ($LASTEXITCODE -ne 0) { throw 'Actual source graph/lifecycle validation failed.' }
$graphReportPath = Join-Path $runDirectory 'graph-validation.json'
$graphReport = Get-Content -LiteralPath $graphReportPath -Raw | ConvertFrom-Json
$graphReport | Add-Member -NotePropertyName sourceSHA256 -NotePropertyValue (Get-FileHash -LiteralPath $sourcePath).Hash
$graphReport | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $graphReportPath -Encoding utf8
Get-Content -LiteralPath $graphReportPath -Raw
