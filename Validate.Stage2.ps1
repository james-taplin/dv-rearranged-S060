param([string]$GamePath = 'B:\SteamLibrary\steamapps\common\Derail Valley')
$ErrorActionPreference = 'Stop'
# Run in a fresh pwsh process to avoid reusing stage 1's loaded catalogue type.
Add-Type -Path (Join-Path $PSScriptRoot 'RearrangedS282\ConventionalPhysics.cs')
Add-Type -Path (Join-Path $PSScriptRoot 'RearrangedS282\WheelArrangement.cs') -CompilerOptions '/define:B99_STAGE2'
$checks = [Collections.Generic.List[string]]::new()
function Check($condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
    $checks.Add($message)
}
function Near([double]$actual, [double]$expected, [string]$message) {
    Check ([math]::Abs($actual-$expected) -le [math]::Max(1e-6,[math]::Abs($expected)*1e-5)) $message
}
function Reject([scriptblock]$action, [string]$message) {
    $rejected = $false
    try { & $action | Out-Null } catch { $rejected = $true }
    Check $rejected $message
}
foreach ($radius in @(.355,.6096,.712,.8509,.9144,1.03376)) {
    $rpm = [RearrangedS282.ConventionalPhysics]::RpmPerMetrePerSecond($radius)
    Near $rpm (60/(2*[math]::PI*$radius)) "Radius ${radius}: native wheel RPM conversion"
    Near ($rpm * (3.6/$rpm)) 3.6 "Radius ${radius}: metre-per-second / km-per-hour round trip"
}
Near ([RearrangedS282.ConventionalPhysics]::FrictionForDriverWeight(.25,120000/137000,3,4)) (.25*(120000/137000)/(3/4)) 'G29 profile compensation matches Derailroader'
Near ([RearrangedS282.ConventionalPhysics]::FrictionForDriverWeight(.25,82000/92000,4,5)) (.25*(82000/92000)/(4/5)) 'C21 profile compensation matches Derailroader'
$stock = [RearrangedS282.ConventionalPhysics]::FrictionForDriverWeight(.25,4/6,4,6)
$berkshire = [RearrangedS282.ConventionalPhysics]::FrictionForDriverWeight(.25,4/7,4,7)
Near $stock .25 'Stock equal-load assumption retains native friction'
Near $berkshire .25 'Berkshire equal-load assumption retains native friction'
Near (($berkshire*4/7)/($stock*4/6)) (6/7) 'Berkshire native adhesive allocation is six-sevenths of stock at unchanged mass'
$capCases = @(
    @{ n='2-2-2'; g=@(1); t=3; kg=18000 }, @{ n='2-4-0'; g=@(2); t=3; kg=38000 }, @{ n='4-4-0'; g=@(2); t=4; kg=38000 },
    @{ n='4-4-2'; g=@(2); t=5; kg=38000 }, @{ n='4-4-4'; g=@(2); t=6; kg=37000 }, @{ n='2-6-0'; g=@(3); t=4; kg=60000 },
    @{ n='4-6-0'; g=@(3); t=5; kg=60000 }, @{ n='4-6-2'; g=@(3); t=6; kg=55500 }, @{ n='2-8-2'; g=@(4); t=6; kg=74000 },
    @{ n='0-10-0'; g=@(5); t=5; kg=111000 }, @{ n='2-2-2-2-2'; g=@(1,1,1); t=5; kg=54000 }, @{ n='2-4-4-2'; g=@(2,2); t=6; kg=74000 },
    @{ n='4-4-4-4'; g=@(2,2); t=8; kg=55500 }, @{ n='2-6-6-2'; g=@(3,3); t=8; kg=83250 }, @{ n='4-0-4'; g=@(0); t=4; kg=0 })
foreach ($c in $capCases) {
    $f = [RearrangedS282.ConventionalPhysics]::DriverWeightFraction([int[]]$c.g, $c.t, 111000)
    Near ($f*111000) $c.kg "$($c.n): capped driver weight $($c.kg) kg"
    $equal = ($c.g | Measure-Object -Sum).Sum / $c.t
    Check ($f -le $equal + 1e-6) "$($c.n): cap never raises driver weight"
}
Reject { [RearrangedS282.ConventionalPhysics]::DriverWeightFraction([int[]]@(2),1,111000) } 'Cap rejects more powered than total axles'
Reject { [RearrangedS282.ConventionalPhysics]::DriverWeightFraction([int[]]@(2),4,0) } 'Cap rejects zero mass'
foreach ($radius in @(0,-1,[float]::NaN,[float]::PositiveInfinity)) {
    Reject { [RearrangedS282.ConventionalPhysics]::RpmPerMetrePerSecond($radius) } "Invalid radius $radius rejected"
}
Reject { [RearrangedS282.ConventionalPhysics]::FrictionForDriverWeight(.25,1,0,6) } 'Zero powered axles rejected'
Reject { [RearrangedS282.ConventionalPhysics]::FrictionForDriverWeight(.25,1.01,4,6) } 'Impossible driver weight fraction rejected'
Reject { [RearrangedS282.ConventionalPhysics]::FrictionForDriverWeight(.25,.5,4,3) } 'More powered axles than total rejected'
for ($id = -1; $id -le 34; $id++) {
    Check ([RearrangedS282.WheelArrangement]::IsRuntimeId($id) -eq ($id -in @(10,11))) "Runtime selection $id is limited to stock / Berkshire"
}
foreach ($direction in @(-1,1)) {
    Check ([RearrangedS282.WheelArrangement]::NextRuntimeId(10,$direction) -eq 11) "Radio direction $direction stock to Berkshire"
    Check ([RearrangedS282.WheelArrangement]::NextRuntimeId(11,$direction) -eq 10) "Radio direction $direction Berkshire to stock"
}
Add-Type -Path (Join-Path $PSScriptRoot 'lib\AssetStudioModCLI_net472_win32_64\Mono.Cecil.dll')
$dll = Join-Path $PSScriptRoot 'build\stage2\RearrangedS282.dll'
$module = [Mono.Cecil.ModuleDefinition]::ReadModule($dll)
try {
    $types = @($module.GetTypes())
    $main = $types | Where-Object FullName -EQ 'RearrangedS282.Main'
    $load = $main.Methods | Where-Object Name -EQ 'Load'
    $loadInstructions = @($load.Body.Instructions | ForEach-Object { [string]$_.Operand })
    Check (@($loadInstructions | Where-Object { $_ -match 'Harmony::PatchAll|ArrangementSaveManager|Settings::Load|Spawner' }).Count -eq 0) 'Entry point excludes broad patching, spawn initialization and persistence'
    Check (@($loadInstructions | Where-Object { $_ -eq 'RearrangedS282.CommsRadio_Awake_Patch' }).Count -eq 1) 'Only the radio patch class is selected'
    Check (@($types | Where-Object FullName -Match 'DuplexSim|DuplexTraction|DuplexDriving|AssetBundleLoader').Count -eq 0) 'No duplex simulation or asset loader compiled'
    $physics = $types | Where-Object FullName -EQ 'RearrangedS282.B99PhysicsState'
    Check (@($physics.Fields | Where-Object { $_.IsStatic -and -not $_.IsLiteral }).Count -eq 0) 'Physics snapshots contain no shared mutable fields'
    $operands = @($physics.Methods | Where-Object HasBody | ForEach-Object { $_.Body.Instructions } | ForEach-Object { [string]$_.Operand })
    Check (@($operands | Where-Object { $_ -match 'TrainCarType::|TrainMassController::set_CarMass|ReciprocatingSteamEngine::' }).Count -eq 0) 'Physics adapter never writes shared car type, mass or steam-engine dimensions'
    Check (@($operands | Where-Object { $_ -match 'DVConvertToEntity::SetComponentData' }).Count -gt 0) 'Physics updates use the selected car native entity'
    $controller = $types | Where-Object FullName -EQ 'RearrangedS282.WheelRearranger'
    $start = $controller.Methods | Where-Object Name -EQ 'Start'
    Check (@($start.Body.Instructions | Where-Object { $_.OpCode.Code -in @('Call','Callvirt','Newobj') }).Count -eq 0) 'Automatic Start does not initialize or alter unselected locomotives'
    $active = @($controller.Methods | Where-Object { $_.Name -in @('TrySwitchStage2','InitializeStage2','SetStage2Geometry') })
    $activeOps = @($active | ForEach-Object { $_.Body.Instructions } | ForEach-Object { [string]$_.Operand })
    Check (@($activeOps | Where-Object { $_ -match 'ArrangementSaveManager|MeshFinder|SetupDrivers|ResetBody|ResetToFourCoupled' }).Count -eq 0) 'Stage 2 path avoids B98 reset constants, mesh extraction and save writes'
    Check (@($module.AssemblyReferences | Where-Object Name -Match '^CCL').Count -eq 0) 'No new CCL runtime dependency'
} finally { $module.Dispose() }
$report = [ordered]@{ passed=$checks.Count; failed=0; checks=@($checks.ToArray()); runtimeValidated=$false; limitations='Pure conversions/catalogue executed; binary paths inspected. Native Unity state transitions and second-car isolation still require an in-game test.' }
$report | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $PSScriptRoot 'build\stage2\validation.json') -Encoding utf8
Write-Output "$($checks.Count) stage 2 offline checks passed. Native runtime validation remains outstanding."
