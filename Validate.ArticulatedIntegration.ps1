param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot 'build\runtime-trial-1.4.4\bin\RearrangedS282.dll'),
    [string]$OutputPath = (Join-Path $PSScriptRoot 'build\articulated-validation\integration-1.4.4.json')
)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'lib\AssetStudioModCLI_net472_win32_64\Mono.Cecil.dll')
$modules = @()
$checks = [Collections.Generic.List[string]]::new()
function Check([bool]$condition,[string]$label) { if (-not $condition) { throw $label }; $checks.Add($label) }
function OpenModule([string]$path) {
    $module = [Mono.Cecil.ModuleDefinition]::ReadModule($path)
    $script:modules += $module
    return $module
}
function TypeOf($module,[string]$name) { return $module.GetTypes() | Where-Object FullName -EQ $name }
function MethodBody($method) {
    if (-not $method.HasBody) { return '' }
    return (@($method.Body.Instructions | ForEach-Object { $_.OpCode.Name + ' ' + [string]$_.Operand }) -join "`n")
}
function MethodOf($type,[string]$name) {
    $matches = @($type.Methods | Where-Object Name -EQ $name)
    if ($matches.Count -ne 1) { throw "Expected one $($type.FullName)::$name method; found $($matches.Count)." }
    return $matches[0]
}
# Follow only a straight-line switch arm (including an unconditional jump).
# The first call identifies the selected geometry operation without executing
# Unity code or relying on compiler-generated instruction offsets.
function FirstArmCall($instruction) {
    $seen = [Collections.Generic.HashSet[int]]::new()
    while ($null -ne $instruction -and $seen.Add($instruction.Offset)) {
        $code = $instruction.OpCode.Name
        if ($code -in @('call','callvirt')) { return $instruction.Operand.Name }
        if ($code -in @('br','br.s')) { $instruction = $instruction.Operand; continue }
        if ($instruction.OpCode.FlowControl.ToString() -in @('Cond_Branch','Return','Throw')) { return $null }
        $instruction = $instruction.Next
    }
    return $null
}
try {
    $candidate = OpenModule $AssemblyPath
    $baseline = OpenModule (Join-Path $PSScriptRoot 'build\delta-compatibility-candidate\bin\RearrangedS282.dll')
    $managed = 'B:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\Managed'
    $native = @(OpenModule (Join-Path $managed 'Assembly-CSharp.dll'); OpenModule (Join-Path $managed 'DV.Simulation.dll'))
    foreach ($name in @('ArticulatedSimulation','ArticulatedRearEngine','ArticulatedRearEngineDefinition','ArticulatedEffects','WheelRearranger')) {
        Check ($null -ne (TypeOf $candidate ('RearrangedS282.'+$name))) ("Compiled integration contains $name")
    }
    Check ($candidate.GetTypes().FullName -notcontains 'RearrangedS282.Sim.DuplexSimController') 'Obsolete duplicate simulation controller remains excluded'
    $nativeSimulationSubclasses = @($candidate.GetTypes() | Where-Object {
        $_.BaseType -and ($_.BaseType.FullName -like 'LocoSim.*' -or $_.BaseType.FullName -like 'DV.Simulation*')
    } | ForEach-Object { $_.FullName + ':' + $_.BaseType.FullName } | Sort-Object)
    $approvedSimulationSubclasses = @(
        'RearrangedS282.ArticulatedRearEngine:LocoSim.Implementations.ReciprocatingSteamEngine',
        'RearrangedS282.ArticulatedRearEngineDefinition:LocoSim.Definitions.ReciprocatingSteamEngineDefinition'
    ) | Sort-Object
    Check (($nativeSimulationSubclasses -join ',') -ceq ($approvedSimulationSubclasses -join ',')) 'Only the existing articulated rear engine and definition extend native simulation types'
    foreach ($name in @('ConventionalPhysics','PoweredWheelRotater')) {
        $old = TypeOf $baseline ('RearrangedS282.'+$name)
        $new = TypeOf $candidate ('RearrangedS282.'+$name)
        foreach ($method in $old.Methods) {
            $match = @($new.Methods | Where-Object FullName -EQ $method.FullName)
            Check ($match.Count -eq 1 -and (MethodBody $match[0]) -ceq (MethodBody $method)) ("Existing native conversion/rod method unchanged: $($method.FullName)")
        }
    }
    $catalogue = TypeOf $candidate 'RearrangedS282.WheelArrangement'
    Check (($catalogue.Fields | Where-Object Name -EQ 'SupportedCount').Constant -eq 38) 'Compiled catalogue contains 33 normal conventional, three articulated (incl. 2-6-6-2), the zero-driver novelty and the giant-driver prototype'
    $oldEnum = TypeOf $baseline 'RearrangedS282.WheelArrangementType'
    $newEnum = TypeOf $candidate 'RearrangedS282.WheelArrangementType'
    $oldIds = @($oldEnum.Fields | Where-Object HasConstant)
    Check ($oldIds.Count -eq 34) 'Compatibility baseline contains the complete original 0-33 ID range'
    foreach ($oldId in $oldIds) {
        $newId = @($newEnum.Fields | Where-Object Name -CEQ $oldId.Name)
        Check ($newId.Count -eq 1 -and $newId[0].Constant -eq $oldId.Constant) ("Saved arrangement ID remains unchanged: $($oldId.Name)=$($oldId.Constant)")
    }
    foreach ($addedId in @(@('s260',34),@('s240',35),@('s222',36),@('s2662',37))) {
        Check (($newEnum.Fields | Where-Object Name -CEQ $addedId[0]).Constant -eq $addedId[1]) ("New arrangement appends its stable ID: $($addedId[0])=$($addedId[1])")
    }
    $compiledNames = @((MethodOf $catalogue '.cctor').Body.Instructions | Where-Object { $_.OpCode.Name -eq 'ldstr' } | ForEach-Object Operand)
    Check ($compiledNames.Count -eq 38) 'Compiled name table contains exactly 38 arrangements'
    foreach ($entry in @(@(34,'2-6-0 Mogul'),@(35,'2-4-0'),@(36,'2-2-2 Giant-driver clearance prototype'),@(37,'2-6-6-2 Articulated Mallet'))) {
        Check ($compiledNames[$entry[0]] -ceq $entry[1]) ("New catalogue name remains parseable at ID $($entry[0]): $($entry[1])")
    }
    Check ($null -ne (TypeOf $candidate 'RearrangedS282.B99ExplosionMaterials')) 'Added gear integrates with native explosion material swaps'
    # B99PhysicsState now deliberately supports zero powered axles. Its executed
    # transition/restore regression fixture is Validate.RestoredFeatures.ps1.
    $wheel = TypeOf $candidate 'RearrangedS282.WheelRearranger'
    foreach ($helper in @('ApplyLegacyGeometry','ClampGiantDriverDiameter','ApplyGiantSingleDriverPrototype',
        'AlignAdditionalPoweredAxles','AlignAdditionalAxle','SuspendAdditionalGeometry','DisposeAdditionalGeometry',
        'SetupPacificHudsonValveGear','ApplyPacificHudsonValveGear','DisposePacificHudsonValveGear')) {
        Check ($null -ne (MethodOf $wheel $helper)) ("Compiled geometry helper is present: $helper")
    }
    $switchOps = MethodBody (MethodOf $wheel 'TrySwitchConventional')
    Check ($switchOps.Contains('ApplyLegacyGeometry(') -and $switchOps.Contains('AlignAdditionalPoweredAxles(')) 'Conventional switch invokes the legacy geometry dispatch and additional native axle alignment'
    Check ($switchOps.IndexOf('AlignAdditionalPoweredAxles(') -lt $switchOps.IndexOf('B99PhysicsState::Apply(')) 'Additional powered axles align before native physics registers the selected axle list'
    Check ($switchOps.Contains('polishedPacificHudsonValveGear') -and $switchOps.Contains('ApplyPacificHudsonValveGear(')) 'Saved polished Pacific/Hudson option is wired into the conventional switch'
    Check ($switchOps.Contains('SetupPacificHudsonValveGear(')) 'Prepared geometry initializes the optional Pacific/Hudson gear'
    Check ($null -ne (MethodOf $wheel 'ApplyMalletGeometry') -and $switchOps.Contains('ApplyMalletGeometry(')) 'Conventional switch routes the 2-6-6-2 to its Mallet geometry'
    Check ($switchOps.IndexOf('ApplyMalletGeometry(') -lt $switchOps.IndexOf('B99PhysicsState::Apply(')) 'Mallet axles align before native physics registers the selected axle list'
    Check ((MethodBody (MethodOf $wheel 'SuspendArticulatedGeometry')).Contains('SuspendMalletGeometry(')) 'Articulated suspension also hides Mallet rear assets'
    foreach ($transitionName in @('TrySwitchConventional','RestoreConventionalStock')) {
        $ops = MethodBody (MethodOf $wheel $transitionName)
        Check ($ops.IndexOf('SuspendAdditionalGeometry(') -ge 0 -and $ops.IndexOf('SuspendAdditionalGeometry(') -lt $ops.IndexOf('GeometryState::Restore(')) ("Lazy prototype renderers are suspended before geometry restoration: $transitionName")
    }
    $destroyOps = MethodBody (MethodOf $wheel 'OnDestroy')
    Check ($destroyOps.Contains('DisposeAdditionalGeometry(') -and $destroyOps.Contains('DisposePacificHudsonValveGear(')) 'Native car deletion disposes both additional visual owners'

    $legacy = MethodOf $wheel 'ApplyLegacyGeometry'
    $driverTables = @($legacy.Body.Instructions | Where-Object {
        $_.OpCode.Name -eq 'switch' -and $_.Operand.Count -eq 37 -and
        (FirstArmCall $_.Operand[36]) -ceq 'ApplyGiantSingleDriverPrototype'
    })
    Check ($driverTables.Count -eq 1) 'Compiled legacy switch has one unambiguous 0-36 driver dispatch table'
    foreach ($route in @(@(33,'ShowNoDrivers'),@(34,'Show6Drivers'),@(35,'Show4Drivers'),@(36,'ApplyGiantSingleDriverPrototype'))) {
        Check ((FirstArmCall $driverTables[0].Operand[$route[0]]) -ceq $route[1]) ("Compiled driver dispatch ID $($route[0]) selects $($route[1])")
    }
    $leadingTables = @($legacy.Body.Instructions | Where-Object {
        $_.OpCode.Name -eq 'switch' -and $_.Operand.Count -eq 37 -and
        (FirstArmCall $_.Operand[36]) -ceq 'ShowTwoLeadingWheels'
    })
    Check ($leadingTables.Count -eq 1) 'Compiled legacy switch has one unambiguous 0-36 leading-wheel dispatch table'
    foreach ($id in @(34,35,36)) {
        Check ((FirstArmCall $leadingTables[0].Operand[$id]) -ceq 'ShowTwoLeadingWheels') ("New conventional ID $id retains its native two-wheel leading truck")
    }

    $prototype = MethodOf $wheel 'ApplyGiantSingleDriverPrototype'
    $prototypeOps = MethodBody $prototype
    $addedComponents = @($prototype.Body.Instructions | Where-Object {
        $_.Operand -is [Mono.Cecil.GenericInstanceMethod] -and $_.Operand.Name -eq 'AddComponent'
    } | ForEach-Object { $_.Operand.GenericArguments[0].FullName })
    $expectedComponents = @('RearrangedS282.PoweredWheelRotater','UnityEngine.MeshFilter','UnityEngine.MeshRenderer')
    Check ((($addedComponents | Sort-Object) -join ',') -CEQ (($expectedComponents | Sort-Object) -join ',')) 'Giant-driver prototype adds only mesh/filter presentation and the unchanged rotation follower'
    Check (-not $prototypeOps.Contains('UnityEngine.Object::Instantiate')) 'Giant-driver prototype does not clone the source gameplay/component hierarchy'
    Check ($prototypeOps.Contains('giant222DriverDiameterMetres') -and $prototypeOps.Contains('ClampGiantDriverDiameter(') -and $prototypeOps.Contains('setWheelRadius(')) 'Configured giant-driver diameter is sanitized and committed through the existing native radius path'
    $clamp = MethodOf $wheel 'ClampGiantDriverDiameter'
    $clampOps = MethodBody $clamp
    Check ($clampOps.Contains('System.Single::IsNaN(') -and $clampOps.Contains('System.Single::IsInfinity(') -and $clampOps.Contains('UnityEngine.Mathf::Clamp(')) 'Prototype diameter rejects non-finite values and uses the native numeric clamp'
    $clampConstants = @($clamp.Body.Instructions | Where-Object { $_.OpCode.Name -eq 'ldc.r4' } | ForEach-Object { [double]$_.Operand })
    foreach ($diameter in @(2.1,2.8,3.6)) {
        Check (@($clampConstants | Where-Object { [Math]::Abs($_ - $diameter) -lt .00001 }).Count -ge 1) ("Compiled diameter limit/default is $diameter metres")
    }
    $alignmentOps = MethodBody (MethodOf $wheel 'AlignAdditionalAxle')
    Check ($alignmentOps.Contains('LocoSim.Implementations.Wheels.PoweredWheel>') -and $alignmentOps.Contains('PoweredWheel::wheelTransform')) 'Additional visuals bind the retained native powered wheel rather than replacing traction'
    $additionalOps = @('ApplyGiantSingleDriverPrototype','AlignAdditionalPoweredAxles','AlignAdditionalAxle') | ForEach-Object { MethodBody (MethodOf $wheel $_) }
    Check (-not (($additionalOps -join "`n") -match '(newobj|AddComponent<).*?(SimulationFlow|SimController|ReciprocatingSteamEngine|Rigidbody|Collider)')) 'Additional rigid conventional arrangements introduce no simulator, rigidbody or collider components'
    $articulationOps = @($wheel.Methods | Where-Object Name -in @('SetupArticulatedGeometry','ConfigureNativeArticulation') | ForEach-Object { MethodBody $_ }) -join "`n"
    Check ($articulationOps.Contains('UnityEngine.Animations.PositionConstraint')) 'Native position constraint supplies the articulation pin'
    Check ($articulationOps.Contains('UnityEngine.Animations.AimConstraint')) 'Native aim constraint follows the bogie support'
    Check (-not ($articulationOps -match 'AddComponent<UnityEngine\.(Rigidbody|HingeJoint|ConfigurableJoint)>')) 'No parallel articulated rigidbody/joint solver is introduced'
    Check ($wheel.Methods.Name -contains 'RefreshConventionalRollingAudio') 'Compiled conversion contains the native rolling-audio cache refresh'
    foreach ($transition in @(@('TrySwitchConventional','B99PhysicsState::Apply('),@('RestoreConventionalStock','B99PhysicsState::Restore('))) {
        $ops = MethodBody ($wheel.Methods | Where-Object Name -EQ $transition[0])
        Check ($ops.IndexOf($transition[1]) -ge 0 -and $ops.IndexOf('RefreshConventionalRollingAudio(') -gt $ops.IndexOf($transition[1])) ("Rolling audio refresh follows committed native physics: $($transition[0])")
    }
    $allBodies = @($candidate.GetTypes() | ForEach-Object { $_.Methods } | ForEach-Object { MethodBody $_ }) -join "`n"
    Check ($allBodies.Contains('CarRollingAudioModule::ResetJointSoundValues(')) 'Compiled helper invokes the existing native rolling cache reset'
    $diagnosticsHook = TypeOf $candidate 'RearrangedS282.PowerDiagnosticsTick'
    Check ($diagnosticsHook.Methods.Name -contains 'Finalizer' -and $diagnosticsHook.Methods.Name -notcontains 'Postfix') 'Existing power capture hook observes native completion and failure'
    $finalizer = $diagnosticsHook.Methods | Where-Object Name -EQ 'Finalizer'
    Check ($finalizer.ReturnType.FullName -eq 'System.Exception' -and (MethodBody $finalizer).Contains('PowerDiagnostics::Capture(')) 'Diagnostic finalizer captures state and returns an exception for native propagation'
    $hooks = @(
        @('DV.Simulation.Cars.SimController','Initialize',@('trainCar','damageController')),
        @('DV.Simulation.Cars.SimController','Update',@()),
        @('LocoSim.Implementations.SimulationFlow','SetSaveStateData',@('savedData')),
        @('DV.Simulation.Controllers.AudioClipSimReadersController','Init',@('car','simFlow')),
        @('DV.Simulation.Controllers.AudioClipSimReadersController','Deinit',@()),
        @('DV.Simulation.Controllers.LayeredAudioSimReadersController','Init',@('car','simFlow')),
        @('DV.Simulation.Controllers.LayeredAudioSimReadersController','Deinit',@()),
        @('DV.Simulation.Controllers.ParticlesPortReadersController','Init',@('simFlow')),
        @('DV.Simulation.Controllers.ParticlesPortReadersController','OnDestroy',@())
    )
    foreach ($hook in $hooks) {
        $type = @($native | ForEach-Object { TypeOf $_ $hook[0] })
        $method = @($type.Methods | Where-Object Name -EQ $hook[1])
        Check ($type.Count -eq 1 -and $method.Count -eq 1) ("Native hook target is unambiguous: $($hook[0]).$($hook[1])")
        Check ((@($method[0].Parameters.Name) -join ',') -ceq (@($hook[2]) -join ',')) ("Native hook argument names match: $($hook[0]).$($hook[1])")
    }
    $expectedBundle = '44A1B9EAE0DDA58BE97B9AEDF3877522FBFFAEC060FE02A8385158E095BDB22D'
    Check ((Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 's282_drivetrain\ExportedProject\Assets\AssetBundles\s282_drivetrain')).Hash -eq $expectedBundle) 'Original authored drivetrain bundle is unchanged'
    $report = [ordered]@{passed=$true;version=$candidate.Assembly.Name.Version.ToString();dllSHA256=(Get-FileHash -LiteralPath $AssemblyPath).Hash;checks=$checks.ToArray();limitations='Compiled integration/native metadata inspection and baseline IL comparison. No actual Harmony dispatch, Unity constraint evaluation, game driving or visual/audio acceptance.'}
    New-Item -ItemType Directory -Path (Split-Path $OutputPath -Parent) -Force | Out-Null
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $OutputPath -Encoding utf8
    [ordered]@{passed=$true;checks=$checks.Count;report=$OutputPath} | ConvertTo-Json
} finally { foreach ($module in $modules) { $module.Dispose() } }
