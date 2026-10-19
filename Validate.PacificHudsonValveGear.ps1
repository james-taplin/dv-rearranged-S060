param([string]$OutputDirectory=(Join-Path $PSScriptRoot 'build\pacific-hudson-valve-gear-validation'))
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$OutputDirectory=(Resolve-Path -LiteralPath $OutputDirectory).Path
$source=Join-Path $PSScriptRoot 'RearrangedS282\WheelRearranger.Walschaerts.cs'
$fixture=Join-Path $PSScriptRoot 'evidence\pacific-hudson-valve-gear'
$project=@"
<Project Sdk="Microsoft.NET.Sdk">
 <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><DefineConstants>B99_ALL_STANDARD</DefineConstants><NuGetAudit>false</NuGetAudit><RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources></PropertyGroup>
 <ItemGroup><Compile Include="$source"/><Compile Include="$fixture\Stubs.cs"/><Compile Include="$fixture\Program.cs"/></ItemGroup>
</Project>
"@
$projectPath=Join-Path $OutputDirectory 'PacificHudson.Validation.csproj'
Set-Content -LiteralPath $projectPath -Value $project -Encoding utf8
$report=Join-Path $OutputDirectory 'validation.json'
& 'C:/Program Files/dotnet/dotnet.exe' run --project $projectPath --configuration Release -- (Join-Path $fixture 'native-animation.json') $report
if($LASTEXITCODE -ne 0){throw 'Pacific/Hudson valve-gear validation failed.'}
$result=Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
$result|Add-Member -NotePropertyName sourceSHA256 -NotePropertyValue (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$result|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $report -Encoding utf8
Get-Content -LiteralPath $report -Raw
