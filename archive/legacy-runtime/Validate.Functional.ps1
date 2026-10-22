param([string]$GamePath = 'B:\SteamLibrary\steamapps\common\Derail Valley')
$ErrorActionPreference = 'Stop'
$sdk = 'C:\Program Files\dotnet\dotnet.exe'
$output = Join-Path $PSScriptRoot 'build\functional'
New-Item -ItemType Directory -Path $output -Force | Out-Null
& $sdk build (Join-Path $PSScriptRoot 'tests\Functional\Functional.csproj') -c Release "-p:GamePath=$GamePath" --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'Functional behavior test build failed.' }
$behavior = & (Join-Path $PSScriptRoot 'tests\Functional\bin\Release\net48\Functional.exe') 2>&1
$behavior | Tee-Object -FilePath (Join-Path $output 'behavior-tests.txt') | Write-Output
if ($LASTEXITCODE -ne 0) { throw 'Functional behavior checks failed.' }

Add-Type -Path (Join-Path $PSScriptRoot 'lib\AssetStudioModCLI_net472_win32_64\Mono.Cecil.dll')
$resolver = [Mono.Cecil.DefaultAssemblyResolver]::new()
$resolver.AddSearchDirectory((Join-Path $GamePath 'DerailValley_Data\Managed'))
$resolver.AddSearchDirectory((Join-Path $GamePath 'DerailValley_Data\Managed\UnityModManager'))
$parameters = [Mono.Cecil.ReaderParameters]::new()
$parameters.AssemblyResolver = $resolver
$module = [Mono.Cecil.ModuleDefinition]::ReadModule((Join-Path $output 'RearrangedS060\RearrangedS060.dll'), $parameters)
$game = [Mono.Cecil.ModuleDefinition]::ReadModule((Join-Path $GamePath 'DerailValley_Data\Managed\Assembly-CSharp.dll'))
$checks = [Collections.Generic.List[string]]::new()
function Assert-Functional($condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
    $checks.Add($message)
}
try {
    $types = @($module.GetTypes())
    $gameTypes = @($game.GetTypes())
    $packageInfo = Get-Content -LiteralPath (Join-Path $output 'RearrangedS060\Info.json') -Raw | ConvertFrom-Json
    Assert-Functional ($module.Assembly.Name.Version.ToString() -eq ($packageInfo.Version + '.0')) 'Functional DLL version matches package metadata'
    Assert-Functional (@($types | Where-Object Name -in @('WheelRearranger','CommsRadioS282Rearranger','ArrangementSaveManager','Settings','ConventionalSpawn','S060SpikeWindow')).Count -eq 0) 'Legacy S282 entry points and spike window excluded'
    Assert-Functional (@($types | Where-Object Name -eq 'S060Radio').Count -eq 1) 'Independent S060 radio compiled'
    $targeting = $types | Where-Object Name -eq 'S060Targeting'
    $targetCalls = @($targeting.Methods | Where-Object HasBody | ForEach-Object { $_.Body.Instructions } |
        Where-Object { $_.OpCode.Code -in @('Call','Callvirt') } | ForEach-Object { $_.Operand.FullName })
    Assert-Functional (@($targetCalls | Where-Object { $_ -match 'CarSpawner::get_AllCars' }).Count -gt 0) 'UMM targeting uses the native spawned-car registry'
    Assert-Functional (@($targetCalls | Where-Object { $_ -match 'FindObjectsOfType|FindObjectsByType|OrderBy|Sort' }).Count -eq 0) 'UMM targeting performs no scene-wide search or sorting'
    Assert-Functional (@($types | Where-Object Name -eq 'S060SixTwo').Count -eq 1) '0-6-2 conversion compiled'
    $pony = $types | Where-Object Name -eq 'S060PonyAssets'
    $ponyCalls = @($pony.Methods | Where-Object HasBody | ForEach-Object { $_.Body.Instructions } |
        Where-Object { $_.OpCode.Code -in @('Call','Callvirt') } | ForEach-Object { $_.Operand.FullName })
    Assert-Functional (@($ponyCalls | Where-Object { $_ -match 'TransitionHelpers::ToV2' }).Count -eq 1) 'Pony donor uses native stock livery lookup'
    Assert-Functional (@($ponyCalls | Where-Object { $_ -match 'System.IO|Process::|Instantiate' }).Count -eq 0) 'Pony donor resolution exports no assets and spawns no donor car'
    $radio = $types | Where-Object Name -eq 'S060Radio'
    $contract = $gameTypes | Where-Object Name -eq 'ICommsRadioMode'
    foreach ($method in $contract.Methods) {
        Assert-Functional (@($radio.Methods | Where-Object { $_.Name -eq $method.Name -and $_.ReturnType.FullName -eq $method.ReturnType.FullName -and $_.Parameters.Count -eq $method.Parameters.Count }).Count -eq 1) "Radio implements $($method.Name)"
    }
    $targetCount = 0
    foreach ($type in $types) {
        $patch = $type.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'HarmonyLib.HarmonyPatch' }
        if (-not $patch) { continue }
        $targetCount++
        $targetType = $patch.ConstructorArguments[0].Value.FullName
        $targetName = [string]$patch.ConstructorArguments[1].Value
        if ($targetName -eq 'Axles') { $targetName = 'get_Axles' }
        $actualType = $gameTypes | Where-Object FullName -eq $targetType
        $target = @($actualType.Methods | Where-Object Name -eq $targetName)
        Assert-Functional ($target.Count -eq 1) "B99 patch target is unambiguous: ${targetType}::$targetName"
        foreach ($method in $type.Methods | Where-Object Name -in @('Prefix','Postfix')) {
            foreach ($param in $method.Parameters | Where-Object { $_.Name -notlike '__*' }) {
                Assert-Functional (@($target[0].Parameters | Where-Object { $_.Name -eq $param.Name -and $_.ParameterType.FullName -eq $param.ParameterType.FullName }).Count -eq 1) "Harmony argument $($param.Name) matches native signature"
            }
        }
    }
    Assert-Functional ($targetCount -eq 7) 'Exactly seven scoped patch classes compiled'
    $controller = $gameTypes | Where-Object Name -eq 'CommsRadioController'
    Assert-Functional (@($controller.Fields | Where-Object Name -eq 'allModes').Count -eq 1) 'Native radio mode list exists'
    Assert-Functional (@($module.AssemblyReferences | Where-Object Name -match 'CCL|AssetStudio|RearrangedS282').Count -eq 0) 'No CCL, extractor or S282 DLL dependency'
    $receipt = Get-Content -LiteralPath (Join-Path $output 'build-receipt.json') -Raw | ConvertFrom-Json
    $dllHash = (Get-FileHash -LiteralPath (Join-Path $output 'RearrangedS060\RearrangedS060.dll') -Algorithm SHA256).Hash
    Assert-Functional ($receipt.output.Hash -eq $dllHash) 'Staged DLL matches build receipt'
    Assert-Functional ($receipt.zip.Hash -eq (Get-FileHash -LiteralPath $receipt.zip.Path -Algorithm SHA256).Hash) 'ZIP matches build receipt'
    [ordered]@{behavior = [string]$behavior; metadataChecks = $checks.Count; checks = @($checks.ToArray()); installed = $false; runtimeValidated = $false} |
        ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'validation.json') -Encoding UTF8
    Write-Output "$($checks.Count) metadata/package checks passed. Radio interactions, driving and save/load still require in-game testing."
} finally { $module.Dispose(); $game.Dispose(); $resolver.Dispose() }
