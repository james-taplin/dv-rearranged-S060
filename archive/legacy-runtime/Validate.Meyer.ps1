param([Parameter(Mandatory)][string]$AuthoringEvidence)
$ErrorActionPreference = 'Stop'
$repo = $PSScriptRoot
$sdk = 'C:\Program Files\dotnet\dotnet.exe'
$evidence = (Resolve-Path -LiteralPath $AuthoringEvidence).Path
$result = Get-Content -LiteralPath (Join-Path $evidence 'authoring-result.json') -Raw | ConvertFrom-Json
$bundleCheck = Get-Content -LiteralPath (Join-Path $evidence 'bundle-validation.json') -Raw | ConvertFrom-Json
if (-not $result.bundleBuilt -or -not $bundleCheck.passed -or -not $bundleCheck.reopenedBundle) { throw 'A successfully reopened CCL bundle is required.' }
$output = Join-Path $repo 'build\meyer\validation'
New-Item -ItemType Directory -Path $output -Force | Out-Null
& $sdk build (Join-Path $repo 'tests\Meyer\Meyer.csproj') -c Release -p:BaseIntermediateOutputPath=obj/physics/ --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'Compound model test compilation failed.' }
$physics = & (Join-Path $repo 'tests\Meyer\bin\Release\net48\Meyer.exe') 2>&1
$physics | Tee-Object -FilePath (Join-Path $output 'physics.txt') | Write-Output
if ($LASTEXITCODE -ne 0) { throw 'Compound conservation tests failed.' }
$native = Get-ChildItem -LiteralPath (Join-Path $repo 'RearrangedS060\obj\Release\net48\PublicizedAssemblies') -Recurse -File -Filter DV.Simulation.dll | Select-Object -First 1 -ExpandProperty FullName
if (-not $native) { throw 'Build Stage 5 first.' }
$nativeOutput = & $sdk run --project (Join-Path $repo 'tests\Meyer\Native.csproj') -c Release "-p:PublicizedSimulation=$native" -p:BaseIntermediateOutputPath=obj/native/ -- $evidence 2>&1
$nativeOutput | Tee-Object -FilePath (Join-Path $output 'native.txt') | Write-Output
if ($LASTEXITCODE -ne 0) { throw 'Actual B99 native engine/control/save tests failed.' }
Add-Type -Path (Join-Path $repo 'lib\AssetStudioModCLI_net472_win32_64\Mono.Cecil.dll')
$dll = Join-Path $repo 'build\stage5\RearrangedS060.dll'
$resolver = [Mono.Cecil.DefaultAssemblyResolver]::new()
$resolver.AddSearchDirectory('B:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\Managed')
$resolver.AddSearchDirectory('B:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\Managed\UnityModManager')
$parameters = [Mono.Cecil.ReaderParameters]::new(); $parameters.AssemblyResolver = $resolver
$module = [Mono.Cecil.ModuleDefinition]::ReadModule($dll, $parameters)
$game = [Mono.Cecil.ModuleDefinition]::ReadModule('B:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\Managed\Assembly-CSharp.dll')
$checks = [Collections.Generic.List[string]]::new()
function Check-Meyer($condition, [string]$message) { if (-not $condition) { throw $message }; $checks.Add($message) }
try {
    $types = @($module.GetTypes()); $gameTypes = @($game.GetTypes())
    Check-Meyer ($module.Assembly.Name.Version.ToString() -eq '0.6.0.0') 'Meyer development runtime version is 0.6.0'
    foreach ($name in @('MeyerRuntime','MeyerCompoundCycle','MeyerNativeStage','MeyerStageDefinition')) {
        Check-Meyer (@($types | Where-Object Name -eq $name).Count -eq 1) "$name is compiled"
    }
    $stage = $types | Where-Object Name -eq 'MeyerNativeStage'
    $calls = @($stage.Methods | Where-Object Name -eq 'Tick' | ForEach-Object { $_.Body.Instructions } | Where-Object { $_.OpCode.Code -in @('Call','Callvirt') } | ForEach-Object { $_.Operand.FullName })
    Check-Meyer (@($calls | Where-Object { $_ -match 'ReciprocatingSteamEngine::Tick' }).Count -eq 0) 'No second native chest/steam calculation runs over the compound'
    foreach ($method in @('SimulateWaterInCylinders','SimulateDamage','CheckChuff')) {
        Check-Meyer (@($calls | Where-Object { $_ -match "ReciprocatingSteamEngine::$method" }).Count -eq 1) "Native $method is retained"
    }
    $patches = 0
    foreach ($type in $types) {
        $patch = $type.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'HarmonyLib.HarmonyPatch' }
        if (-not $patch) { continue }
        $patches++
        Check-Meyer ($patch.ConstructorArguments.Count -ge 2) "Harmony target metadata decodes: $($type.Name)"
        $targetType = $patch.ConstructorArguments[0].Value.FullName; $targetName = [string]$patch.ConstructorArguments[1].Value
        if ($targetName -eq 'Axles') { $targetName = 'get_Axles' }
        $target = @($gameTypes | Where-Object FullName -eq $targetType | ForEach-Object Methods | Where-Object Name -eq $targetName)
        Check-Meyer ($target.Count -eq 1) "Native hook resolves: ${targetType}::$targetName"
        foreach ($method in $type.Methods | Where-Object Name -in @('Prefix','Postfix')) {
            foreach ($parameter in $method.Parameters | Where-Object { $_.Name -notlike '__*' }) {
                Check-Meyer (@($target[0].Parameters | Where-Object { $_.Name -eq $parameter.Name -and $_.ParameterType.FullName -eq $parameter.ParameterType.FullName }).Count -eq 1) "Hook argument matches: $($parameter.Name)"
            }
        }
    }
    Check-Meyer ($patches -eq 10) 'Seven S060 and three scoped Meyer hook classes'
    Check-Meyer (@($module.AssemblyReferences | Where-Object Name -match 'CCL|AssetStudio|RearrangedS282').Count -eq 0) 'Runtime adds no CCL, extractor or S282 assembly dependency'
    Check-Meyer ($bundleCheck.poweredAxles -eq 4 -and $bundleCheck.cockReaders -eq 2 -and $bundleCheck.cockSoundAliases -eq 4) 'Reopened bundle has both engine cock systems and four powered axles'
    Check-Meyer ($bundleCheck.bundledGameMeshes -eq 0 -and $bundleCheck.bundledAudioClips -eq 0) 'Native meshes and sounds remain aliases'
    $report = [ordered]@{passed=$true;utc=[DateTime]::UtcNow.ToString('o');authoringEvidence=$evidence;physics=[string]$physics;native=[string]$nativeOutput;metadataChecks=$checks.Count;checks=@($checks.ToArray());runtimeDLL=(Get-FileHash -LiteralPath $dll | Select-Object Path,Hash);bundle=(Get-FileHash -LiteralPath (Join-Path $evidence 'RS060_Meyer_0440\ccl_bundle') | Select-Object Path,Hash);installed=$false;gameRuntimeValidated=$false}
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $output 'validation.json') -Encoding UTF8
    Write-Output "$($checks.Count) Meyer metadata/bundle checks passed. Actual cab operation, rail behavior and effect playback still require game testing."
} finally { $module.Dispose(); $game.Dispose(); $resolver.Dispose() }
