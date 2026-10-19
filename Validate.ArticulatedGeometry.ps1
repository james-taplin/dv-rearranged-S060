param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'build\articulated-geometry-validation'))
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
$sourcePath = Join-Path $root 'RearrangedS282\WheelRearranger.Articulated.cs'
$source = Get-Content -LiteralPath $sourcePath -Raw
$start = $source.IndexOf('    internal static class ArticulatedFrameClipper', [StringComparison]::Ordinal)
$end = $source.LastIndexOf("`n}", [StringComparison]::Ordinal)
if ($start -lt 0 -or $end -le $start) { throw 'Actual articulated frame clipper class was not found.' }
$helper = "using System;`nusing System.Collections.Generic;`nusing UnityEngine;`nusing Object = UnityEngine.Object;`nnamespace RearrangedS282 {`n" + $source.Substring($start, $end - $start) + "`n}`n"
$helperPath = Join-Path $OutputDirectory 'ActualFrameClipper.cs'
$helper | Set-Content -LiteralPath $helperPath -Encoding utf8
$fixture = Join-Path $root 'evidence\articulated-geometry-tests'
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <NuGetAudit>false</NuGetAudit><RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$helperPath" />
    <Compile Include="$fixture\Stubs.cs" />
    <Compile Include="$fixture\Program.cs" />
  </ItemGroup>
</Project>
"@
$projectPath = Join-Path $OutputDirectory 'ArticulatedGeometry.Validation.csproj'
$project | Set-Content -LiteralPath $projectPath -Encoding utf8
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnet)) { $dotnet = (Get-Command dotnet -ErrorAction Stop).Source }
$reportPath = Join-Path $OutputDirectory 'validation.json'
& $dotnet run --project $projectPath --configuration Release -- $reportPath
if ($LASTEXITCODE -ne 0) { throw 'Articulated frame clipper validation failed.' }
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
$report | Add-Member -NotePropertyName sourceSHA256 -NotePropertyValue (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
$report | Add-Member -NotePropertyName extractedHelperSHA256 -NotePropertyValue (Get-FileHash -LiteralPath $helperPath -Algorithm SHA256).Hash
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding utf8
Get-Content -LiteralPath $reportPath -Raw
