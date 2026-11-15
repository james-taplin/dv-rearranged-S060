param([Parameter(Mandatory=$true)][string]$RuntimePath,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Build.Config.ps1')
$config=Get-S060BuildConfig
$project=Join-Path $PSScriptRoot 'Meyer\UnityProject'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$OutputDirectory=(Resolve-Path -LiteralPath $OutputDirectory).Path
$env:S060_AUDIT_MANAGED=Join-Path $config.GamePath 'DerailValley_Data\Managed'
$env:S060_AUDIT_MODS=Join-Path $config.GamePath 'Mods'
$env:S060_EXHAUST_AUDIT_RUNTIME=(Resolve-Path -LiteralPath $RuntimePath).Path
$assetRoot=Join-Path $config.GamePath 'Mods\RearrangedS060\assets'
$env:S060_MEYER_AUDIT_BUNDLE=Join-Path $assetRoot 'Meyer\ccl_bundle'
$env:S060_SHAY_AUDIT_BUNDLE=Join-Path $assetRoot 'Shay\shay_running_gear'
$env:S060_NATIVE_PREVIEW=Join-Path $config.QAPath 'native-preview.json'
$env:S060_CONVENTIONAL_NATIVE=Join-Path $config.QAPath 'conventional-native.json'
$env:S060_DETAIL_NATIVE=Join-Path $config.QAPath 'meyer-running-gear-native.json'
$env:S060_COUPLING_FIXTURE=Join-Path $config.QAPath 'meyer-rear-coupling-native.json'
$env:S060_ONLY_SHAY='1';$env:S060_SHAY_PREVIEW_ONLY='1';$env:S060_PIPE_SWEEP_PREVIEW='1'
$env:S060_CONVENTIONAL_OUTPUT=Join-Path $OutputDirectory 'shay'
$env:S060_SMOOTH_STEAM_OUTPUT=Join-Path $OutputDirectory 'meyer'
foreach($source in Get-ChildItem (Join-Path $PSScriptRoot 'Meyer\Authoring') -Filter '*.cs' -File){Copy-Item -LiteralPath $source.FullName -Destination (Join-Path $project 'Assets\Editor') -Force}
foreach($name in @('S060RodMesh.cs','S060MeyerLayout.cs','S060MeyerFrontMotion.cs','S060MeyerGearMounts.cs','S060MeyerFrontEquipment.cs','S060MeyerInwardPipes.cs','S060MeyerClearance.cs','S060SteamPipes.cs','S060SteamSleeve.cs','S060BodyPipes.cs','S060MeyerExhaustPipe.cs','S060PipeGeometry.cs','S060MeyerBodywork.cs','S060MeyerLights.cs','S060MeyerCollision.cs')){
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('RearrangedS060\'+$name)) -Destination (Join-Path $project 'Assets\Editor') -Force
}
foreach($entry in @(@{name='shay';method='ConventionalNativeAudit.RunBatch';report='conventional-validation.json'},@{name='meyer';method='MeyerSmoothSteamAudit.RunBatch';report='pipe-sweep-preview.json'})){
    $folder=Join-Path $OutputDirectory $entry.name
    New-Item -ItemType Directory $folder -Force | Out-Null
    $process=Start-Process $config.Unity -ArgumentList @('-batchmode','-projectPath',('"'+$project+'"'),'-executeMethod',$entry.method,'-logFile',('"'+(Join-Path $folder 'unity.log')+'"')) -WindowStyle Hidden -PassThru
    if(-not $process.WaitForExit(180000)){$process.Kill();throw "Preview timed out: $folder"}
    if($process.ExitCode -ne 0 -or -not(Test-Path (Join-Path $folder $entry.report))){throw "Preview failed: $folder"}
    Write-Output "Rendered $($entry.name): $folder"
}
