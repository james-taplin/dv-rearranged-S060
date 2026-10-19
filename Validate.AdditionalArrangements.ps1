param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'build\additional-arrangements-validation'))
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$output = (Resolve-Path -LiteralPath $OutputDirectory).Path
$run = Join-Path $output ('run-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
 <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><DefineConstants>B99_ALL_STANDARD</DefineConstants><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NuGetAudit>false</NuGetAudit><RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources></PropertyGroup>
 <ItemGroup>
  <Compile Include="$root\RearrangedS282\WheelRearranger.Additional.cs" />
  <Compile Include="$root\RearrangedS282\PoweredWheelRotater.cs" />
  <Compile Include="$root\evidence\additional-arrangements-tests\Stubs.cs" />
  <Compile Include="$root\evidence\additional-arrangements-tests\Program.cs" />
 </ItemGroup>
</Project>
"@
$projectPath = Join-Path $output 'AdditionalArrangements.Validation.csproj'
$project | Set-Content -LiteralPath $projectPath -Encoding utf8
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
& $dotnet run --project $projectPath --configuration Release -- $run
if ($LASTEXITCODE -ne 0) { throw 'Additional arrangements managed validation failed.' }
$reportPath = Join-Path $run 'validation.json'
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
$hashes = [ordered]@{}
foreach ($name in @('WheelRearranger.Additional.cs','PoweredWheelRotater.cs')) {
 $hashes[$name] = (Get-FileHash -LiteralPath (Join-Path $root "RearrangedS282\$name") -Algorithm SHA256).Hash
}
$report | Add-Member -NotePropertyName sourceSHA256 -NotePropertyValue $hashes
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding utf8
Get-Content -LiteralPath $reportPath -Raw
