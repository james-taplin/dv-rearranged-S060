param([string]$GamePath = 'B:\SteamLibrary\steamapps\common\Derail Valley')
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
# Execute the actual catalogue logic without loading Unity or touching any save.
if (-not ('RearrangedS060.WheelArrangement' -as [type])) {
    Add-Type -Path (Join-Path $root 'RearrangedS060\WheelArrangement.cs')
}
$checks = [Collections.Generic.List[string]]::new()
function Assert-Stage1($condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
    $checks.Add($message)
}
$expectedNames = 's440,s442,s444,s460,s462,s464,s080,s082,s084,s280,s282,s284,s280Big,s282Big,s284Big,s480,s482,s484,s0100,s0102,s0104,s2100,s2102,s2104,s4100,s4102,s4104,s0120,s2120,s2122,s4122,s2442,s4444,s404'.Split(',')
for ($i = 0; $i -lt $expectedNames.Length; $i++) {
    $value = [Enum]::Parse([RearrangedS060.WheelArrangementType], $expectedNames[$i])
    Assert-Stage1 ([int]$value -eq $i) "Legacy ID $i remains $($expectedNames[$i])"
}
for ($i = 0; $i -le 30; $i++) {
    Assert-Stage1 ([RearrangedS060.WheelArrangement]::NormalizeSavedId($i) -eq $i) "Conventional ID $i survives load normalization"
}
foreach ($id in @(-1,31,32,33,34,[int]::MaxValue,[int]::MinValue)) {
    Assert-Stage1 ([RearrangedS060.WheelArrangement]::NormalizeSavedId($id) -eq 10) "Unsupported ID $id falls back to stock 2-8-2"
}
foreach ($direction in @(-1,1)) {
    $id = 10
    $seen = [Collections.Generic.HashSet[int]]::new()
    for ($i = 0; $i -lt 31; $i++) {
        $id = [RearrangedS060.WheelArrangement]::NextStandardId($id,$direction)
        Assert-Stage1 ([RearrangedS060.WheelArrangement]::IsStandardId($id)) "Radio direction $direction step $i excludes retired IDs"
        [void]$seen.Add($id)
    }
    Assert-Stage1 ($seen.Count -eq 31 -and $id -eq 10) "Radio direction $direction visits each conventional ID and wraps"
}
# Metadata-only inspection does not execute the mod or Unity native code.
$cecil = Join-Path $root 'lib\AssetStudioModCLI_net472_win32_64\Mono.Cecil.dll'
Add-Type -Path $cecil
$dll = Join-Path $root 'build\stage1\RearrangedS060.dll'
$module = [Mono.Cecil.ModuleDefinition]::ReadModule($dll)
try {
    $types = @($module.GetTypes())
    Assert-Stage1 (@($types | Where-Object FullName -Match 'RearrangedS060\.Sim\.|DuplexSimPatches|AddTwoCylinders|AssetBundleLoader').Count -eq 0) 'Compiled DLL excludes duplex simulation, patches and asset loader'
    $main = $types | Where-Object FullName -EQ 'RearrangedS060.Main'
    $load = $main.Methods | Where-Object Name -EQ 'Load'
    $calls = @($load.Body.Instructions | Where-Object { $_.OpCode.Code -in @('Call','Callvirt','Newobj') } | ForEach-Object { $_.Operand.FullName })
    Assert-Stage1 (@($calls | Where-Object { $_ -match 'HarmonyLib|ArrangementSaveManager|Settings::|AssetBundle|WheelRearranger' }).Count -eq 0) 'Compiled entry point registers no patches and invokes no arrangement, settings-load or asset mutation path'
    Assert-Stage1 (@($module.AssemblyReferences | Where-Object Name -Match '^CCL').Count -eq 0) 'No CCL runtime dependency introduced'
    # Inventory prospective Harmony targets against real B99 metadata for the next milestone.
    $game = [Mono.Cecil.ModuleDefinition]::ReadModule((Join-Path $GamePath 'DerailValley_Data\Managed\Assembly-CSharp.dll'))
    try {
        $gameTypes = @($game.GetTypes())
        $targets = @('TrainCar:Start','CommsRadioController:Awake','SaveGameManager:Save','CarsSaveManager:Load','Bogie:get_Axles','TrainStress:FixedUpdate','ExplosionModelHandler:RevertToUnexplodedModel')
        foreach ($target in $targets) {
            $parts = $target.Split(':')
            $type = $gameTypes | Where-Object Name -EQ $parts[0]
            Assert-Stage1 (@($type.Methods | Where-Object Name -EQ $parts[1]).Count -gt 0) "B99 contains prospective patch target $target (signature/runtime matching still pending)"
        }
    } finally { $game.Dispose() }
} finally { $module.Dispose() }
$report = [ordered]@{ passed = $checks.Count; failed = 0; checks = @($checks.ToArray()); runtimeValidated = $false }
$report | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $root 'build\stage1\validation.json') -Encoding utf8
Write-Output "$($checks.Count) stage 1 checks passed. In-game loading and runtime switching have not been tested."
