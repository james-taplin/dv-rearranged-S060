param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'build\delta-compatibility-validation'), [switch]$ActualHarmony)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$mode = if ($ActualHarmony) { 'actual-harmony' } else { 'harness' }
$projectDirectory = Join-Path $OutputDirectory $mode
New-Item -ItemType Directory -Path $projectDirectory -Force | Out-Null
$projectDirectory = (Resolve-Path -LiteralPath $projectDirectory).Path
$runDirectory = Join-Path $OutputDirectory ($mode + '-run-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
$jsonPath = Join-Path $root 'lib\AssetStudioModCLI_net472_win32_64\Newtonsoft.Json.dll'
$constants = if ($ActualHarmony) { 'B99_ALL_STANDARD;ACTUAL_HARMONY' } else { 'B99_ALL_STANDARD' }
$harmonyReference = if ($ActualHarmony) { '<Reference Include="0Harmony"><HintPath>B:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\Managed\UnityModManager\0Harmony.dll</HintPath></Reference>' } else { '' }
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
    <DefineConstants>$constants</DefineConstants><EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <NuGetAudit>false</NuGetAudit><RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$root\RearrangedS282\DeltaTrailingTruckCompatibility.cs" />
    <Compile Include="$root\RearrangedS282\DeltaTrailingTruckVisuals.cs" />
    <Compile Include="$root\evidence\delta-compatibility-tests\Stubs.cs" />
    <Compile Include="$root\evidence\delta-compatibility-tests\Program.cs" />
    <Compile Include="$root\evidence\delta-compatibility-tests\HarmonyDispatch.cs" />
    <Reference Include="Newtonsoft.Json"><HintPath>$jsonPath</HintPath></Reference>
    $harmonyReference
  </ItemGroup>
</Project>
"@
$projectPath = Join-Path $projectDirectory 'DeltaCompatibility.Validation.csproj'
$project | Set-Content -LiteralPath $projectPath -Encoding utf8
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnet)) { $dotnet = (Get-Command dotnet -ErrorAction Stop).Source }
& $dotnet run --project $projectPath --configuration Release -- $runDirectory
if ($LASTEXITCODE -ne 0) { throw 'Delta compatibility validation failed.' }
$reportPath = Join-Path $runDirectory 'validation.json'
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
$hashes = [ordered]@{}
foreach ($name in @('DeltaTrailingTruckCompatibility.cs','DeltaTrailingTruckVisuals.cs')) {
    $hashes[$name] = (Get-FileHash -LiteralPath (Join-Path $root "RearrangedS282\$name") -Algorithm SHA256).Hash
}
$report | Add-Member -NotePropertyName sourceSHA256 -NotePropertyValue $hashes
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding utf8
Get-Content -LiteralPath $reportPath -Raw
