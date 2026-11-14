param([string]$SourcesRoot = (Join-Path $PSScriptRoot 'Shay\ModelSource'))
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Build.Config.ps1')
$config=Get-S060BuildConfig
$contract=(Get-FileHash (Join-Path $SourcesRoot 'shared\fit-contract.json')).Hash
if($contract -ne '617AE50BEBEC7759064FDDAED09CBE26D258072A1836A48BBEC2B52EC4407F77'){throw 'Runtime geometry is calibrated to the frozen R3 fit contract. Review a contract change before rebuilding.'}
$contacts=Join-Path $PSScriptRoot 'build\shay\combined-contact.json'
& $config.Python -B (Join-Path $PSScriptRoot 'Shay\Audio\generate.py') --check --report (Join-Path $PSScriptRoot 'build\shay\audio-validation.json')
if($LASTEXITCODE -ne 0){throw 'Original Shay exhaust audio validation failed.'}
& $config.Python -B (Join-Path $PSScriptRoot 'Shay\ModelSource\generate.py')
if($LASTEXITCODE -ne 0){throw 'Original R3 model generation failed.'}
& $config.Python -B (Join-Path $PSScriptRoot 'Shay\tools\Validate.R3.py') --source $SourcesRoot --output $contacts
if($LASTEXITCODE -ne 0){throw 'Combined Shay transmission/truck geometry failed.'}
$project=Join-Path $PSScriptRoot 'build\shay-r3-authoring\UnityProject'
foreach($folder in @('Assets\Editor','Packages','ProjectSettings')) { New-Item -ItemType Directory -Path (Join-Path $project $folder) -Force | Out-Null }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Meyer\UnityProject\ProjectSettings\ProjectVersion.txt') -Destination (Join-Path $project 'ProjectSettings') -Force
'{"dependencies":{"com.unity.modules.assetbundle":"1.0.0","com.unity.modules.audio":"1.0.0","com.unity.modules.jsonserialize":"1.0.0","com.unity.modules.imageconversion":"1.0.0","com.unity.modules.physics":"1.0.0"}}' | Set-Content (Join-Path $project 'Packages\manifest.json') -Encoding UTF8
foreach($name in @('ShayBundleBuild.cs','ShayR3ModelBuild.cs','ShayExhaustBuild.cs')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('Shay\Authoring\'+$name)) -Destination (Join-Path $project 'Assets\Editor') -Force
}
$env:S060_SHAY_SOURCE=(Resolve-Path -LiteralPath $SourcesRoot).Path
$env:S060_SHAY_AUDIO_SOURCE=Join-Path $PSScriptRoot 'Shay\Audio'
$output=Join-Path $PSScriptRoot ('build\shay\bundle-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $output -Force | Out-Null
$env:S060_SHAY_BUNDLE_OUTPUT=$output
$process=Start-Process -FilePath $config.Unity -ArgumentList @('-projectPath',('"'+$project+'"'),'-executeMethod','ShayBundleBuild.RunBatch','-logFile',('"'+(Join-Path $output 'unity.log')+'"')) -WindowStyle Hidden -PassThru
Write-Output "Building original Shay modules: $output"
if(-not $process.WaitForExit(240000)) { $process.Kill(); throw 'Shay bundle build timed out.' }
if($process.ExitCode -ne 0 -or -not (Test-Path (Join-Path $output 'bundle-validation.json'))) { throw "Shay bundle failed: $output" }
[ordered]@{ passed=$true; sourceContract=$contract; bundle=Get-FileHash (Join-Path $output 'shay_running_gear') | Select-Object Path,Hash } | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $output 'bundle-receipt.json') -Encoding UTF8
Write-Output "Verified original-only Shay asset bundle: $output"
