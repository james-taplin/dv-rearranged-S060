param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'build\articulated-effects-validation'))
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
$sourcePath = Join-Path $root 'RearrangedS282\ArticulatedEffects.cs'
$source = Get-Content -LiteralPath $sourcePath -Raw
function Extract-Method([string]$marker) {
    $start = $source.IndexOf($marker, [StringComparison]::Ordinal)
    if ($start -lt 0) { throw "Actual method was not found: $marker" }
    $opening = $source.IndexOf('{', $start)
    $depth = 0
    for ($index = $opening; $index -lt $source.Length; $index++) {
        if ($source[$index] -eq '{') { $depth++ }
        if ($source[$index] -eq '}') {
            $depth--
            if ($depth -eq 0) { return $source.Substring($start, $index - $start + 1) }
        }
    }
    throw "Actual method had no closing brace: $marker"
}
$discovery = Extract-Method 'private static IEnumerable<T> AudioControllers<T>'
$identity = Extract-Method 'private static bool AudioBindingMatches'
$helper = "using System;`nusing System.Collections.Generic;`nusing UnityEngine;`nusing LocoSim.Implementations;`nnamespace RearrangedS282 { internal static partial class ArticulatedEffects {`n" + $discovery + "`n" + $identity + "`n} }`n"
$helperPath = Join-Path $OutputDirectory 'ActualAudioDiscovery.cs'
$helper | Set-Content -LiteralPath $helperPath -Encoding utf8
$fixture = Join-Path $root 'evidence\articulated-effects-tests'
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
$projectPath = Join-Path $OutputDirectory 'ArticulatedEffects.Validation.csproj'
$project | Set-Content -LiteralPath $projectPath -Encoding utf8
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnet)) { $dotnet = (Get-Command dotnet -ErrorAction Stop).Source }
$reportPath = Join-Path $OutputDirectory 'validation.json'
& $dotnet run --project $projectPath --configuration Release -- $reportPath
if ($LASTEXITCODE -ne 0) { throw 'Articulated audio discovery validation failed.' }
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
$report | Add-Member -NotePropertyName sourceSHA256 -NotePropertyValue (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
$report | Add-Member -NotePropertyName extractedMethodsSHA256 -NotePropertyValue (Get-FileHash -LiteralPath $helperPath -Algorithm SHA256).Hash
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding utf8
Get-Content -LiteralPath $reportPath -Raw
