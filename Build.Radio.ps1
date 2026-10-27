param([string]$DocksideEvidence = '', [Parameter(Mandatory)][string]$NativeEvidence)
$ErrorActionPreference = 'Stop'
$repo = $PSScriptRoot
. (Join-Path $repo 'Build.Config.ps1')
$config = Get-S060BuildConfig
$sdk = $config.Dotnet
$managed = Join-Path $config.GamePath 'DerailValley_Data\Managed'
$coreFiles = @('RearrangedS060.dll','Info.json','LICENSE','README.md','CREDITS.md')
function Assert-CoreStage([string]$Path, [switch]$AllowMissing) {
    if (-not (Test-Path -LiteralPath $Path)) { if ($AllowMissing) { return }; throw "Missing core stage: $Path" }
    $entries = @(Get-ChildItem -LiteralPath $Path -Force)
    if (@($entries | Where-Object { $_.PSIsContainer -or $_.Name -notin $coreFiles }).Count) { throw "Unexpected core staging entries: $Path" }
    if (-not $AllowMissing -and $entries.Count -ne $coreFiles.Count) { throw "Incomplete core staging manifest: $Path" }
}
$output = Join-Path $repo ("build\radio-$($config.Version)")
$packageRoot = Join-Path $output 'package'
if (Test-Path -LiteralPath $packageRoot) {
    if (@(Get-ChildItem -LiteralPath $packageRoot -Force | Where-Object { -not $_.PSIsContainer -or $_.Name -ne 'RearrangedS060' }).Count) { throw 'Unexpected core package directories or files.' }
    Assert-CoreStage (Join-Path $packageRoot 'RearrangedS060') -AllowMissing
}
New-Item -ItemType Directory -Path $output -Force | Out-Null
& (Join-Path $repo 'Build.Runtime.ps1')
$nativeReport = Get-Content (Join-Path $NativeEvidence 'runtime-validation.json') -Raw | ConvertFrom-Json
if (-not $nativeReport.passed -or $nativeReport.runtimeSha256 -ne (Get-FileHash (Join-Path $repo 'build\stage7\RearrangedS060.dll')).Hash) { throw 'Native runtime audits must match this exact DLL.' }
foreach ($report in $nativeReport.reports) {
    if ((Get-FileHash $report.path).Hash -ne $report.hash -or -not (Get-Content $report.path -Raw | ConvertFrom-Json).passed) { throw 'Native audit evidence changed.' }
}
foreach ($name in @('exhaust','cocks','cleanup','collision','contacts','conventional','quartering','shay-effects')) { if (@($nativeReport.reports | Where-Object name -eq $name).Count -ne 1) { throw "Missing native audit: $name" } }
$shayEffects = Get-Content ($nativeReport.reports | Where-Object name -eq 'shay-effects').path -Raw | ConvertFrom-Json
if (-not $shayEffects.actualPlayMode -or -not $shayEffects.oldDopplerFailureReproduced -or -not $shayEffects.productionInactiveAssembly -or -not $shayEffects.nativeDopplerAwake) { throw 'Native Shay audio activation and processed-CCL drip regressions are required.' }
$quartering = Get-Content ($nativeReport.reports | Where-Object name -eq 'quartering').path -Raw | ConvertFrom-Json
if (-not $quartering.actualPlayMode -or -not $quartering.productionRetention -or -not $quartering.oldResetReproduced -or
    -not $quartering.repeatedTransitions -or -not $quartering.forwardReverse -or -not $quartering.originalFlagsRestored -or -not $quartering.teardown) { throw 'Actual Unity quartering reset, retention, native return and cleanup regressions are required.' }
$conventional = Get-Content ($nativeReport.reports | Where-Object name -eq 'conventional').path -Raw | ConvertFrom-Json
if (-not $conventional.shayNativeWheelReuse -or -not $conventional.shayBundleSha256) { throw 'Combined Shay native-wheel and articulation validation is required.' }
if (-not $conventional.candidateGeometry -or -not $conventional.fiveDrivenAxles -or -not $conventional.fullSizeCylinders -or -not $conventional.tenOilSites -or -not $conventional.cleanup -or -not $conventional.crankSweep -or -not $conventional.tyreClearance) { throw 'Compiled conventional five-axle fit, motion, clearance and cleanup regressions are required.' }
$cockReport = ($nativeReport.reports | Where-Object name -eq 'cocks').path
if (-not (Get-Content $cockReport -Raw | ConvertFrom-Json).ownedFixedCrankFlowAndReverser) { throw 'Owned cylinder-cock fixed-crank flow/reverser regression is required.' }
$cockDetails = Get-Content $cockReport -Raw | ConvertFrom-Json
if (-not $cockDetails.ownedInletPublicationOrder -or -not $cockDetails.nativeLowFlowThreshold -or -not $cockDetails.ownedWetDripGatesAndReuse) { throw 'Native inlet-order, low-flow and wet-drip regressions are required.' }
$cleanupReport = ($nativeReport.reports | Where-Object name -eq 'cleanup').path
if (-not (Get-Content $cleanupReport -Raw | ConvertFrom-Json).lpReturnCandidateLifetime) { throw 'Compiled LP exhaust-return lifetime regression is required.' }
$collisionReport = Get-Content ($nativeReport.reports | Where-Object name -eq 'collision').path -Raw | ConvertFrom-Json
if (-not $collisionReport.rearBogieRigidbodyOwnership -or -not $collisionReport.nativeCarAutomaticMassFramePreserved -or -not $collisionReport.nativeBogieReadinessChecked) { throw 'Native rear-bogie collision ownership and mass-frame/readiness regressions are required.' }
$contactReport = Get-Content ($nativeReport.reports | Where-Object name -eq 'contacts').path -Raw | ConvertFrom-Json
if (-not $contactReport.actualPlayMode -or -not $contactReport.actualNativeCID -or -not $contactReport.productionRelayUsed -or
    -not $contactReport.nativeEnterStayExit -or -not $contactReport.pauseFlagPreserved -or -not $contactReport.unownedContactsIgnored -or
    -not $contactReport.relayTeardown -or -not $contactReport.sameFrameRecovery -or
    -not $contactReport.directParentDestructionReleasesBindings) { throw 'Compiled rear-contact delivery, filtering and teardown regressions are required.' }
$exhaustReport = Get-Content ($nativeReport.reports | Where-Object name -eq 'exhaust').path -Raw | ConvertFrom-Json
if (-not $exhaustReport.smokeBeforeOwnerRegistration -or -not $exhaustReport.detachedPooledAudio) { throw 'Real startup and detached audio regressions are required.' }
$simDll = Get-ChildItem (Join-Path $repo 'RearrangedS060\obj\Release\net48\PublicizedAssemblies') -Recurse -Filter DV.Simulation.dll | Select-Object -First 1 -ExpandProperty FullName
$oiling = & $sdk run --project (Join-Path $repo 'tests\Oiling\Native.csproj') -c Release "-p:PublicizedSimulation=$simDll" "-p:ManagedPath=$managed" 2>&1
if ($LASTEXITCODE -ne 0) { throw "Native oiling checks failed: $oiling" }
$oiling | Set-Content (Join-Path $output 'oiling-checks.txt')
$trace = & $sdk run --project (Join-Path $repo 'tests\Trace\Trace.csproj') -c Release 2>&1
if ($LASTEXITCODE -ne 0) { throw "Bounded diagnostic capture checks failed: $trace" }
$trace | Set-Content (Join-Path $output 'trace-checks.txt')
$evidence = if ($DocksideEvidence) { $DocksideEvidence } else { Get-ChildItem (Join-Path $repo 'build\dockside') -Directory -Filter 'authoring-*' | Where-Object { Test-Path (Join-Path $_.FullName 'authoring-result.json') } | Sort-Object Name -Descending | Select-Object -First 1 -ExpandProperty FullName }
if (-not $evidence) { throw 'A completed Dockside asset export is required.' }
& $sdk build (Join-Path $repo 'tests\Functional\Functional.csproj') -c Release -p:ArticulatedTest=true "-p:GamePath=$($config.GamePath)" --nologo -v:q
if ($LASTEXITCODE -ne 0) { throw 'Functional check build failed.' }
$functional = & (Join-Path $repo 'tests\Functional\bin\Release\net48\Functional.exe') 2>&1
if ($LASTEXITCODE -ne 0) { throw "Functional checks failed: $functional" }
$functional | Set-Content (Join-Path $output 'functional-checks.txt')
$engines = & $sdk run --project (Join-Path $repo 'tests\Articulated\Native.csproj') -c Release "-p:PublicizedSimulation=$simDll" "-p:ManagedPath=$managed" -- (Join-Path $evidence 'steamEngine-calibration.json') 2>&1
if ($LASTEXITCODE -ne 0) { throw "Native engine-bank checks failed: $engines" }
$engines | Set-Content (Join-Path $output 'engine-checks.txt')
$transitions = & $sdk run --project (Join-Path $repo 'tests\Transitions\Transitions.csproj') -c Release "-p:ManagedPath=$managed" 2>&1
if ($LASTEXITCODE -ne 0) { throw "Axle transition checks failed: $transitions" }
$transitions | Set-Content (Join-Path $output 'transition-checks.txt')
$transactions = & $sdk run --project (Join-Path $repo 'tests\Transactions\Transactions.csproj') -c Release 2>&1
if ($LASTEXITCODE -ne 0) { throw "Conversion failure-boundary checks failed: $transactions" }
$transactions | Set-Content (Join-Path $output 'transaction-checks.txt')
$compound = & $sdk run --project (Join-Path $repo 'tests\Meyer\Meyer.csproj') -c Release 2>&1
if ($LASTEXITCODE -ne 0) { throw "Compound physics checks failed: $compound" }
$compound | Set-Content (Join-Path $output 'compound-checks.txt')
$geometry = & $sdk run --project (Join-Path $repo 'tests\MeyerLayout\NativeGeometry.csproj') -c Release "-p:ManagedPath=$managed" -- (Join-Path $config.QAPath 'native-preview.json') 2>&1
if ($LASTEXITCODE -ne 0) { throw "Native Meyer geometry checks failed: $geometry" }
$geometry | Set-Content (Join-Path $output 'meyer-geometry-checks.txt')
$dll = Join-Path $repo 'build\stage7\RearrangedS060.dll'
Add-Type -Path (Join-Path $repo 'lib\AssetStudioModCLI_net472_win32_64\Mono.Cecil.dll')
$module = [Mono.Cecil.ModuleDefinition]::ReadModule($dll)
try {
    if ($module.Assembly.Name.Version.ToString() -ne ($config.Version + '.0')) { throw 'Wrong runtime version.' }
    $types = @($module.GetTypes())
    if (@($types | Where-Object Name -in @('WheelRearranger','CommsRadioS282Rearranger','ArrangementSaveManager','Settings','ConventionalSpawn','S060SpikeWindow','Stage2RuntimeChecks')).Count) { throw 'Archived prototype/S282 entry points must not ship in the current runtime.' }
    $allCalls = @($types | ForEach-Object Methods | Where-Object HasBody | ForEach-Object { $_.Body.Instructions } | Where-Object { $_.OpCode.Code -in @('Call','Callvirt') } | ForEach-Object { $_.Operand.FullName })
    $conversionType = $types | Where-Object Name -eq 'S060Conversion'
    $articulated = $types | Where-Object Name -eq 'S060Articulated'
    $applyCalls = @($articulated.Methods | Where-Object Name -eq 'Apply' | ForEach-Object { $_.Body.Instructions } | Where-Object { $_.OpCode.Code -in @('Call','Callvirt','Newobj') } | ForEach-Object { $_.Operand.FullName })
    $retainIndex = [Array]::FindIndex($applyCalls, [Predicate[object]]{ param($call) $call -match 'S060NativeQuartering::Retain' })
    $hideIndex = [Array]::FindIndex($applyCalls, [Predicate[object]]{ param($call) $call -match 'S060Articulated::Hide' })
    if ($retainIndex -lt 0 -or $hideIndex -le $retainIndex) { throw 'Native phase retention must precede hiding the original mechanisms.' }
    $phaseCalls = @($types | Where-Object Name -eq 'S060NativeQuartering' | ForEach-Object Methods | Where-Object HasBody | ForEach-Object { $_.Body.Instructions } | Where-Object { $_.OpCode.Code -in @('Call','Callvirt') } | ForEach-Object { $_.Operand.FullName })
    if (@($phaseCalls | Where-Object { $_ -match 'Animator::(Play|Update|Rebind)' }).Count) { throw 'Native quartering retention must not seek or drive Animator playback.' }
    $oilingRegistration = $types | Where-Object Name -eq 'S060Oiling' | ForEach-Object Methods | Where-Object Name -eq 'Register'
    $oilingTypes = @($oilingRegistration.Body.Instructions | Where-Object { $_.OpCode.Code -eq 'Ldtoken' } | ForEach-Object { $_.Operand.FullName })
    if ($oilingTypes -contains 'LocoSim.Implementations.SimulationFlow' -or $oilingTypes -notcontains 'LocoSim.Implementations.ManualOilingPoints') { throw 'Oil migration must use the native per-component loader.' }
    foreach ($methodName in @('AlignAxlesToWheels','RestoreAxlePositions')) {
        $methodCalls = @($conversionType.Methods | Where-Object Name -eq $methodName | ForEach-Object { $_.Body.Instructions } | Where-Object { $_.OpCode.Code -in @('Call','Callvirt') } | ForEach-Object { $_.Operand.FullName })
        if (@($methodCalls | Where-Object { $_ -match 'S060RollingAudio::Refresh' }).Count -ne 1) { throw "Missing native rolling-audio cache refresh in $methodName." }
    }
    if (@($allCalls | Where-Object { $_ -match 'CarRollingAudioModule::ResetJointSoundValues' }).Count -ne 1) { throw 'Rolling audio must rebuild its native axle cache.' }
    if (@($allCalls | Where-Object { $_ -match 'UnityEngine.GUILayout::HorizontalSlider' }).Count) { throw 'Release arrangement controls must not expose driver-size sliders.' }
    foreach ($name in @('S060Meyer','S060Dockside','S060EngineBank','S060EngineSlot','S060ArticulatedSimulation','S060ArticulatedPresentation','S060ExhaustPresentation','S060StockCocks','S060EightLayout','S060MeyerSupport','S060LogicLength','S060CylinderFittings','S060BodyPipes','S060SteamPipes','S060MeyerExhaustPipe','S060NativeCylinders','S060MeyerLights','S060OilCups','S060Oiling')) {
        if (@($types | Where-Object Name -eq $name).Count -ne 1) { throw "Missing runtime component: $name" }
    }
    if (@($module.AssemblyReferences | Where-Object Name -Match 'CCL|AssetStudio|RearrangedS282').Count) { throw 'Unexpected runtime dependency.' }
    foreach ($name in @('S060Shay','S060ShayAssets','S060ShayEngine','S060ShayMotion','S060ShayTransmission','S060ShayEffects','S060ShayPlumbing')) {
        if (@($types | Where-Object Name -eq $name).Count -ne 1) { throw "Missing Shay runtime component: $name" }
    }
    $implementation = @($types | Where-Object { $_.Name -in @('S060Conversion','S060Articulated','S060Shay','S060EngineBank','S060EngineSlot','S060ArticulatedSimulation','S060MeyerSupport','S060LogicLength','S060Lifecycle') })
    $instructions = @($implementation | ForEach-Object Methods | Where-Object HasBody | ForEach-Object { $_.Body.Instructions })
    $calls = @($instructions | Where-Object { $_.OpCode.Code -in @('Call','Callvirt','Newobj') } | ForEach-Object { $_.Operand.FullName })
    if (@($calls | Where-Object { $_ -match 'CarSpawner::|SimController::Initialize\(|TrainCar::InitializeNewLogicCar|TrainCar::set_carLivery' }).Count) {
        throw 'In-place switching must not spawn/reinitialize/replace the native car.'
    }
    $identityWrites = @($instructions | Where-Object { $_.OpCode.Code -eq 'Stfld' -and $_.Operand.FullName -match 'TrainCar::(logicCar|carLivery|ID|CarGUID)' })
    if ($identityWrites.Count) { throw 'Switching must retain native car identity.' }
} finally { $module.Dispose() }
$stage = Join-Path $output 'package\RearrangedS060'
New-Item -ItemType Directory -Path $stage -Force | Out-Null
Copy-Item -LiteralPath $dll -Destination $stage -Force
$info = Get-Content (Join-Path $repo 'Info.json') -Raw | ConvertFrom-Json
$info.Version = $config.Version
$info | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $stage 'Info.json') -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $repo 'LICENSE') -Destination $stage -Force
Copy-Item -LiteralPath (Join-Path $repo 'CREDITS.md') -Destination $stage -Force
Copy-Item -LiteralPath (Join-Path $repo 'docs\RADIO-ARTICULATED-TESTING.md') -Destination (Join-Path $stage 'README.md') -Force
Assert-CoreStage $stage
$zip = Join-Path $output ("RearrangedS060-$($config.Version)-radio-test.zip")
Compress-Archive -LiteralPath $stage -DestinationPath $zip -Force
$receipt = [ordered]@{
    utc = [DateTime]::UtcNow.ToString('o'); runtimeVersion = $config.Version; installed = $false; gameRuntimeValidated = $false
    functionalChecks = [string]$functional; engineChecks = [string]$engines; transitionChecks = [string]$transitions
    meyerGeometryChecks = [string]$geometry
    oilingChecks = [string]$oiling
    traceChecks = [string]$trace
    transactionChecks = [string]$transactions
    compoundChecks = [string]$compound
    nativeCockReaderChecks = Get-Content $cockReport -Raw | ConvertFrom-Json
    nativeExhaustReaderChecks = $exhaustReport
    nativeEvidence = $NativeEvidence; nativeRuntimeChecks = $nativeReport
    identityILAudit = 'No spawn/reinitialize/car-identity writes in switching implementation'
    arrangementUIAudit = 'No driver-size slider calls; stock and short-wheelbase 0-6-0 are separate fixed choices'
    requiredAssetPacks = @('RS060_Meyer_0440 0.7.0','RS060_Franklin_Dockside_0440 0.7.0')
    files = @(Get-ChildItem $stage -File | Get-FileHash | Select-Object Path,Hash)
    zip = Get-FileHash -LiteralPath $zip | Select-Object Path,Hash
}
$receipt | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'package-receipt.json') -Encoding UTF8
Write-Output $functional
Write-Output $engines
Write-Output $transitions
Write-Output $geometry
Write-Output $oiling
Write-Output $transactions
Write-Output $compound
Write-Output "Staged $zip. Installed build left untouched."


