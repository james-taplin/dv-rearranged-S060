param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'build\power-diagnostics-validation'))
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$projectDirectory = Join-Path $OutputDirectory 'harness'
New-Item -ItemType Directory -Path $projectDirectory -Force | Out-Null
$projectDirectory = (Resolve-Path -LiteralPath $projectDirectory).Path
$runtimeDirectory = Join-Path $OutputDirectory ('run-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
$jsonPath = Join-Path $root 'lib\AssetStudioModCLI_net472_win32_64\Newtonsoft.Json.dll'
# Build a separate executable against actual production sources; never overwrite mod output.
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
    <DefineConstants>B99_ALL_STANDARD</DefineConstants><EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <NuGetAudit>false</NuGetAudit><RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$root\RearrangedS282\PowerDiagnostics.cs" />
    <Compile Include="$root\RearrangedS282\WheelArrangement.cs" />
    <Compile Include="$root\evidence\power-diagnostics-tests\Stubs.cs" />
    <Compile Include="$root\evidence\power-diagnostics-tests\Program.cs" />
    <Reference Include="Newtonsoft.Json"><HintPath>$jsonPath</HintPath></Reference>
  </ItemGroup>
</Project>
"@
$projectPath = Join-Path $projectDirectory 'PowerDiagnostics.Validation.csproj'
$project | Set-Content -LiteralPath $projectPath -Encoding utf8
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnet)) { $dotnet = (Get-Command dotnet -ErrorAction Stop).Source }
& $dotnet run --project $projectPath --configuration Release -- $runtimeDirectory
if ($LASTEXITCODE -ne 0) { throw 'Power diagnostic validation failed.' }
$reportPath = Join-Path $runtimeDirectory 'validation.json'
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
$report | Add-Member -NotePropertyName captureSourceSHA256 -NotePropertyValue (Get-FileHash -LiteralPath (Join-Path $root 'RearrangedS282\PowerDiagnostics.cs') -Algorithm SHA256).Hash
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding utf8
Get-Content -LiteralPath $reportPath -Raw
