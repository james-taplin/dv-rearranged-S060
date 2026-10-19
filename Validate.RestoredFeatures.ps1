param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'build\restored-features-validation'))
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$native = 'B:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\Managed\Assembly-CSharp.dll'
$ilspy = 'C:\Users\james\.dotnet\tools\ilspycmd.exe'
function Extract([string]$source,[string]$marker) {
    $start=$source.IndexOf($marker,[StringComparison]::Ordinal)
    if ($start -lt 0) { throw "Missing method/property $marker" }
    $opening=$source.IndexOf('{',$start); $depth=1; $end=$opening+1
    while ($depth -gt 0 -and $end -lt $source.Length) {
        if ($source[$end] -eq '{') { $depth++ }; if ($source[$end] -eq '}') { $depth-- }; $end++
    }
    if ($depth -ne 0) { throw 'Unbalanced source' }
    return $source.Substring($start,$end-$start)
}
$explosion=(& $ilspy -t ExplosionModelHandler $native) -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'Native explosion decompilation failed' }
# Visibility changes only, equivalent to the production build-local publicizer.
$explosion=$explosion.Replace('private ','internal ')
$force=(& $ilspy -t DV.Simulation.Cars.DrivingForce $native) -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'Native force decompilation failed' }
$force=$force.Replace('public class DrivingForce','public partial class DrivingForce').Replace('private ','internal ')
$slip=(& $ilspy -t DV.Wheels.WheelslipController $native) -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'Native wheelslip decompilation failed' }
$properties=@('public bool IsEngineBraking','public float EngineBrakingForcePerAxle','public int NumberOfPoweredAxles','public void ResetState()') | ForEach-Object { Extract $slip $_ }
$slip="using System; using UnityEngine; using DV.ECS.Train; using LocoSim.Implementations; namespace DV.Wheels { public partial class WheelslipController {`n" + ($properties -join "`n") + "`n} }"
$geometry=Extract ([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'RearrangedS282\WheelRearranger.cs'))) 'private void ShowNoDrivers()'
$fields=@([regex]::Matches($geometry,'\b(\w+)\.gameObject.SetActive') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
$geometry="using UnityEngine; namespace RearrangedS282 { internal class GeometryFixture {`n" +
    (($fields | ForEach-Object { "internal Transform $_ = new GameObject().transform;" }) -join "`n") +
    "`n$geometry`ninternal void Apply()=>ShowNoDrivers(); internal Transform[] Parts=>new Transform[]{" + ($fields -join ',') + '}; } }'
$nativePath=Join-Path $OutputDirectory 'NativeAlgorithms.cs'
# Separate namespace-bearing files preserve the installed source algorithms.
$explosion | Set-Content (Join-Path $OutputDirectory 'NativeExplosion.cs') -Encoding utf8
$force | Set-Content (Join-Path $OutputDirectory 'NativeDrivingForce.cs') -Encoding utf8
$slip | Set-Content $nativePath -Encoding utf8
$geometry | Set-Content (Join-Path $OutputDirectory 'ActualNoDrivers.cs') -Encoding utf8
$fixture=Join-Path $PSScriptRoot 'evidence\restored-features-tests'
$project=@"
<Project Sdk="Microsoft.NET.Sdk">
 <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><DefineConstants>B99_ALL_STANDARD</DefineConstants><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NuGetAudit>false</NuGetAudit></PropertyGroup>
 <ItemGroup>
  <Compile Include="$PSScriptRoot\RearrangedS282\B99PhysicsState.cs" />
  <Compile Include="$PSScriptRoot\RearrangedS282\ConventionalPhysics.cs" />
  <Compile Include="$PSScriptRoot\RearrangedS282\B99ExplosionMaterials.cs" />
  <Compile Include="$fixture\*.cs" />
  <Compile Include="$OutputDirectory\Native*.cs" />
  <Compile Include="$OutputDirectory\ActualNoDrivers.cs" />
 </ItemGroup>
</Project>
"@
$projectPath=Join-Path $OutputDirectory 'RestoredFeatures.csproj'
$project | Set-Content $projectPath -Encoding utf8
$reportPath=Join-Path $OutputDirectory 'validation.json'
& 'C:\Program Files\dotnet\dotnet.exe' run --project $projectPath -c Release -- $reportPath
if ($LASTEXITCODE -ne 0) { throw 'Restored features fixture failed' }
$report=Get-Content $reportPath -Raw | ConvertFrom-Json
$report | Add-Member sourceHashes @(Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'RearrangedS282\B99PhysicsState.cs'),(Join-Path $PSScriptRoot 'RearrangedS282\B99ExplosionMaterials.cs') | Select-Object Path,Hash)
$report | Add-Member nativeAssemblySHA256 (Get-FileHash -LiteralPath $native).Hash
$report | ConvertTo-Json -Depth 6 | Set-Content $reportPath -Encoding utf8
Get-Content $reportPath -Raw
