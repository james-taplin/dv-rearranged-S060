param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'build\rolling-audio-validation'))
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
$nativeAssembly = 'B:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\Managed\Assembly-CSharp.dll'
$ilspy = 'C:\Users\james\.dotnet\tools\ilspycmd.exe'
function Extract-Method([string]$source,[string]$marker) {
    $start = $source.IndexOf($marker, [StringComparison]::Ordinal)
    if ($start -lt 0) { throw "Native method was not found: $marker" }
    $opening = $source.IndexOf('{', $start)
    $depth = 0
    for ($index = $opening; $index -lt $source.Length; $index++) {
        if ($source[$index] -eq '{') { $depth++ }
        if ($source[$index] -eq '}') {
            $depth--
            if ($depth -eq 0) { return $source.Substring($start, $index - $start + 1) }
        }
    }
    throw "Native method had no closing brace: $marker"
}
$rollingSource = (& $ilspy -t DV.ModularAudioCar.CarRollingAudioModule $nativeAssembly) -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'Could not inspect the actual installed native rolling module.' }
$modularSource = (& $ilspy -t DV.ModularAudioCar.CarModularAudio $nativeAssembly) -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'Could not inspect the actual installed native modular-audio update.' }
# The real mod uses its existing build-local publicizer for this native method.
# Change visibility only in the fixture; retain the native algorithm verbatim.
$reset = (Extract-Method $rollingSource 'private void ResetJointSoundValues()').Replace('private void ResetJointSoundValues()', 'internal void ResetJointSoundValues()')
$update = Extract-Method $rollingSource 'public override void UpdateModule(float deltaTime)'
$dispatch = Extract-Method $modularSource 'public void DoUpdate(float deltaTime)'
$actual = "using System;`nusing System.Collections.Generic;`nusing UnityEngine;`nnamespace DV.ModularAudioCar { public partial class CarRollingAudioModule {`n" + $reset + "`n" + $update + "`n} public partial class CarModularAudio {`n" + $dispatch + "`n} }`n"
$nativePath = Join-Path $OutputDirectory 'ActualNativeAudio.cs'
$actual | Set-Content -LiteralPath $nativePath -Encoding utf8
$fixture = Join-Path $root 'evidence\rolling-audio-tests'
$sourcePath = Join-Path $root 'RearrangedS282\WheelRearranger.Audio.cs'
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
    <DefineConstants>B99_ALL_STANDARD</DefineConstants><EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <NuGetAudit>false</NuGetAudit><RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$sourcePath" />
    <Compile Include="$nativePath" />
    <Compile Include="$fixture\Stubs.cs" />
    <Compile Include="$fixture\Program.cs" />
  </ItemGroup>
</Project>
"@
$projectPath = Join-Path $OutputDirectory 'RollingAudio.Validation.csproj'
$project | Set-Content -LiteralPath $projectPath -Encoding utf8
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnet)) { $dotnet = (Get-Command dotnet -ErrorAction Stop).Source }
$reportPath = Join-Path $OutputDirectory 'validation.json'
& $dotnet run --project $projectPath --configuration Release -- $reportPath
if ($LASTEXITCODE -ne 0) { throw 'Native rolling audio validation failed.' }
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
$report | Add-Member -NotePropertyName sourceSHA256 -NotePropertyValue (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
$report | Add-Member -NotePropertyName nativeAssemblySHA256 -NotePropertyValue (Get-FileHash -LiteralPath $nativeAssembly -Algorithm SHA256).Hash
$report | Add-Member -NotePropertyName extractedNativeSHA256 -NotePropertyValue (Get-FileHash -LiteralPath $nativePath -Algorithm SHA256).Hash
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding utf8
Get-Content -LiteralPath $reportPath -Raw
