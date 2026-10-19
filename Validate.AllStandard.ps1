param([string]$GamePath = 'B:\SteamLibrary\steamapps\common\Derail Valley')
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'RearrangedS282\ConventionalPhysics.cs')
Add-Type -Path (Join-Path $PSScriptRoot 'RearrangedS282\WheelArrangement.cs') -CompilerOptions '/define:B99_ALL_STANDARD'
$checks = [Collections.Generic.List[string]]::new()
function Check($condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
    $checks.Add($message)
}
function Near([double]$actual, [double]$expected, [string]$message) {
    Check ([math]::Abs($actual-$expected) -le [math]::Max(1e-6,[math]::Abs($expected)*1e-5)) $message
}
for ($id=-1; $id -le 34; $id++) {
    Check ([RearrangedS282.WheelArrangement]::IsRuntimeId($id) -eq ($id -ge 0 -and $id -le 30)) "ID ${id}: only conventional arrangements selectable"
}
foreach ($direction in @(-1,1)) {
    $visited = [Collections.Generic.HashSet[int]]::new()
    $id = 10
    for ($i=0; $i -lt 31; $i++) {
        Check ($visited.Add($id)) "Radio direction ${direction}: unique selection $i"
        $id = [RearrangedS282.WheelArrangement]::NextRuntimeId($id,$direction)
    }
    Check ($id -eq 10 -and $visited.Count -eq 31) "Radio direction ${direction}: complete catalogue cycle"
}
foreach ($radius in @(.6096,.712,.8509,.9144,1.03376)) {
    Near ([RearrangedS282.ConventionalPhysics]::RpmPerMetrePerSecond($radius)) (60/(2*[math]::PI*$radius)) "Radius ${radius}: native RPM conversion"
}
Near ([RearrangedS282.ConventionalPhysics]::FrictionForDriverWeight(.25,120000/137000,3,4)) (.25*(120000/137000)/(3/4)) 'G29 compensation matches Derailroader convention'
Near ([RearrangedS282.ConventionalPhysics]::FrictionForDriverWeight(.25,82000/92000,4,5)) (.25*(82000/92000)/(4/5)) 'C21 compensation matches Derailroader convention'
for ($id=0; $id -le 30; $id++) {
    $wheels = ([RearrangedS282.WheelArrangement]::WheelArrangementNames[$id].Split(' ')[0].Split('-') | ForEach-Object { [int]$_ })
    $powered = $wheels[1]/2; $total = ($wheels | Measure-Object -Sum).Sum/2
    Check ($powered -ge 2 -and $powered -le 6 -and $total -ge $powered) "Arrangement ${id}: valid physical axle profile"
    Near ([RearrangedS282.ConventionalPhysics]::FrictionForDriverWeight(.24,$powered/$total,$powered,$total)) .24 "Arrangement ${id}: equal-load assumption preserves material friction"
}
Add-Type -Path (Join-Path $PSScriptRoot 'lib\AssetStudioModCLI_net472_win32_64\Mono.Cecil.dll')
$module = [Mono.Cecil.ModuleDefinition]::ReadModule((Join-Path $PSScriptRoot 'build\stage3\RearrangedS282.dll'))
try {
    $types=@($module.GetTypes())
    $load=($types | Where-Object FullName -EQ 'RearrangedS282.Main').Methods | Where-Object Name -EQ Load
    $loadOps=@($load.Body.Instructions | ForEach-Object { [string]$_.Operand })
    Check (@($loadOps | Where-Object { $_ -match 'Harmony::PatchAll|ArrangementSaveManager|Settings::Load|Spawner' }).Count -eq 0) 'Entry point excludes global patches, persistence and random spawning'
    Check (@($loadOps | Where-Object { $_ -eq 'RearrangedS282.CommsRadio_Awake_Patch' }).Count -eq 1) 'Entry point selects only the radio patch'
    Check (@($types | Where-Object FullName -Match 'DuplexSim|DuplexTraction|DuplexDriving|AssetBundleLoader').Count -eq 0) 'No duplex simulation or drivetrain asset loader compiled'
    $controller=$types | Where-Object FullName -EQ 'RearrangedS282.WheelRearranger'
    $start=$controller.Methods | Where-Object Name -EQ Start
    Check (@($start.Body.Instructions | Where-Object { $_.OpCode.Code -in @('Call','Callvirt','Newobj') }).Count -eq 0) 'Automatic Start leaves unselected cars untouched'
    $entry=$controller.Methods | Where-Object Name -EQ TrySwitchStage2
    Check (@($entry.Body.Instructions | Where-Object { [string]$_.Operand -match 'TrySwitchConventional' }).Count -eq 1) 'Radio delegates to the conventional B99 path'
    # Traverse controller methods actually reachable from the conventional entry point.
    $queue=[Collections.Generic.Queue[object]]::new(); $queue.Enqueue(($controller.Methods | Where-Object Name -EQ TrySwitchConventional))
    $seen=[Collections.Generic.HashSet[string]]::new(); $active=[Collections.Generic.List[object]]::new()
    while ($queue.Count -gt 0) {
        $method=$queue.Dequeue()
        if (-not $seen.Add($method.FullName) -or -not $method.HasBody) { continue }
        foreach ($ins in $method.Body.Instructions) {
            $active.Add($ins)
            if ($ins.Operand -is [Mono.Cecil.MethodReference] -and $ins.Operand.DeclaringType.FullName -eq $controller.FullName) {
                $next=$controller.Methods | Where-Object FullName -EQ $ins.Operand.FullName
                if ($null -ne $next) { $queue.Enqueue($next) }
            }
        }
    }
    $activeOps=@($active | ForEach-Object { [string]$_.Operand })
    Check ($seen.Contains(($controller.Methods | Where-Object Name -EQ SetupDrivers).FullName)) 'Original driver geometry is reused'
    Check (-not ($activeOps -match 'ArrangementSaveManager|TrainMassController::set_CarMass|TrainCar::UpdateMass|SetMassOnTrainPlate')) 'Active conversion preserves native mass and avoids save writes'
    Check (@($active | Where-Object { $_.OpCode.Code -eq 'Stfld' -and [string]$_.Operand -match 'ReciprocatingSteamEngine::' }).Count -eq 0) 'Active conversion never rewrites native engine dimensions/cutoff'
    Check (-not ($activeOps -match 'ShowNoDrivers|FixExplodedModel')) 'Diagnostic no-driver and legacy explosion paths remain inactive'
    $binding=$controller.Methods | Where-Object Name -EQ LoadExternalInteractables
    $bindingOps=@($binding.Body.Instructions | ForEach-Object { [string]$_.Operand })
    Check (-not ($bindingOps -match 'HingeJoint|connectedAnchor|Interactables/C_Lubricator$')) 'External binding no longer assumes retired hinge joints or the old lubricator path'
    Check (@($bindingOps | Where-Object { $_ -eq 'Interactables/lubricator pivot' }).Count -eq 1) 'External binding uses the current native lubricator pivot'
    $placement=$controller.Methods | Where-Object Name -EQ UpdateExternalControlPositions
    $placementOps=@($placement.Body.Instructions | ForEach-Object { [string]$_.Operand })
    Check (@($placementOps | Where-Object { $_ -match 'Transform::set_localPosition' }).Count -eq 5) 'All five external transforms receive placement from captured baselines'
    Check (@($placementOps | Where-Object { $_ -match 'Transform::get_localPosition' }).Count -eq 2) 'Placement reads only body offsets rather than accumulating control positions'
    Check (@($module.AssemblyReferences | Where-Object Name -Match '^CCL').Count -eq 0) 'No new CCL dependency'
} finally { $module.Dispose() }
$nativeHierarchy=Join-Path $PSScriptRoot 'build\b99-external-controls\hierarchy-components.txt'
if (Test-Path -LiteralPath $nativeHierarchy) {
    $native=Get-Content -LiteralPath $nativeHierarchy
    foreach($path in @('LocoS282A_ExternalInteractables_Labels/Lubricator','Gauges','Interactables/lubricator pivot','Interactables/lubricator pivot/C_Lubricator','LocoS282A_ExternalInteractables_Labels/AirPump','Interactables/C_BrakeCompressor')) {
        Check (@($native | Where-Object { $_.StartsWith("LocoS282A_ExternalInteractables/$path | ",[StringComparison]::Ordinal) }).Count -eq 1) "Installed native S282 contains required external path: $path"
    }
    Check (@($native | Where-Object { $_.StartsWith('LocoS282A_ExternalInteractables/Interactables/C_Lubricator | ') }).Count -eq 0) 'Installed native S282 confirms the old direct lubricator path is absent'
    Check (@($native | Where-Object { $_ -match '^LocoS282A_ExternalInteractables/Interactables/(lubricator pivot/C_Lubricator|C_BrakeCompressor) \|' -and $_ -match 'HingeJoint' }).Count -eq 0) 'Installed native lubricator and compressor controls have no hinge joints'
}
$geometryHierarchy=Join-Path $PSScriptRoot 'build\b99-external-controls\full-hierarchy.txt'
if (Test-Path -LiteralPath $geometryHierarchy) {
    $native=Get-Content -LiteralPath $geometryHierarchy
    $source=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'RearrangedS282\WheelRearranger.cs') -Raw
    foreach($side in @('L','R')) {
        $prefix="LocoS282A/LocoS282A_Body/MovingParts_LOD0/LocoS282A_Drivetrain $side"
        Check (@($native | Where-Object { $_.StartsWith($prefix+' | ') -and $_ -match ',Animator,' }).Count -eq 1) "Native $side drivetrain has the expected animator"
        # Brake-shoe objects are generated by the installed LMS loader, not native prefab children.
        $relativePaths=@([regex]::Matches($source,'drive[LR]\.Find\("([^"]+)"\)') | ForEach-Object {$_.Groups[1].Value} | Where-Object { $_ -ne 's282a_brake_shoes(Clone)' } | Sort-Object -Unique)
        foreach($path in $relativePaths) {
            Check (@($native | Where-Object {$_.StartsWith($prefix+'/'+$path+' | ')}).Count -eq 1) "Native $side drivetrain child lookup exists: $path"
        }
        foreach($path in @('Oiling0','s282_mech_push_rod/Oiling1','s282_mech_wheels_connect/s282_mech_push_rod_to_connect/Oiling2')) {
            Check (@($native | Where-Object {$_.StartsWith($prefix+'/'+$path+' | ') -and $_ -match 'PositionSyncProvider'}).Count -eq 1) "Native $side oilcup provider exists: $path"
        }
    }
    Check ($source -notmatch 'DriveMechanism [LR]|lubricatorHandle') 'Removed obsolete drivetrain names and static-body lubricator handle'
    for($cup=0;$cup -lt 6;$cup++) {
        Check (@($native | Where-Object {$_.StartsWith("LocoS282A_ExternalInteractables/ManualOilingPoints/OilingPoint$cup | ") -and $_ -match 'PositionSyncConsumer'}).Count -eq 1) "Native oilcup $cup has a position consumer"
    }
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::OpenRead((Join-Path $PSScriptRoot 'build\RearrangedS282-B99-all-standard-trial.zip'))
try {
    $names=@($zip.Entries.FullName)
    Check (@($names | Where-Object { $_ -match 'AssetStudioModCLI_net472_win64_contained/AssetStudioModCLI.exe$' }).Count -eq 1) 'ZIP includes original rod exporter'
    Check (@($names | Where-Object { $_ -match '\.obj$|s282_drivetrain|Assembly-CSharp.dll|DV.Simulation.dll' }).Count -eq 0) 'ZIP excludes extracted game meshes, game assemblies and duplex bundle'
    Check (@($names | Where-Object { $_ -match '/ALL_STANDARD_TESTING.md$' }).Count -eq 1) 'ZIP includes explicit structural button/test instructions'
} finally { $zip.Dispose() }
$probe=Join-Path $PSScriptRoot 'build\stage3\mesh-probe\s282_mech_wheels_connect.obj'
if (Test-Path -LiteralPath $probe) {
    $obj=Get-Content -LiteralPath $probe
    Check (@($obj | Where-Object { $_ -match '^v ' }).Count -eq 508) 'Installed B99 rod export: 508 source vertices'
    Check (@($obj | Where-Object { $_ -match '^f ' }).Count -eq 524) 'Installed B99 rod export: 524 source faces'
}
$report=[ordered]@{passed=$checks.Count; failed=0; checks=@($checks.ToArray()); runtimeValidated=$false; limitations='Offline catalogue/numerics, active binary path and packaging checked. Native runtime, appearance and driving await user testing.'}
$report | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $PSScriptRoot 'build\stage3\validation.json') -Encoding utf8
Write-Output "$($checks.Count) all-standard offline checks passed; runtime validation remains outstanding."
