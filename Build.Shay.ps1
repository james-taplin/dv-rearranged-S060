param([string]$SourcesRoot = 'B:\LLW CONVERT\locos\s060-shay')
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Build.Config.ps1')
$config=Get-S060BuildConfig
$contract=(Get-FileHash (Join-Path $SourcesRoot 'shared\fit-contract.json')).Hash
if($contract -ne '3A6A104D1F12C49C3E399001E67F9F095C10EECD5AF2F39677AC44D70F37E8A7'){throw 'Runtime geometry is calibrated to the frozen R1 fit contract. Review a contract change before rebuilding.'}
$contacts=Join-Path $PSScriptRoot 'build\shay\combined-contact.json'
& $config.Python (Join-Path $PSScriptRoot 'Shay\tools\Check.Assembly.py') $SourcesRoot $contacts
if($LASTEXITCODE -ne 0){throw 'Combined Shay transmission/truck geometry failed.'}
$project=Join-Path $PSScriptRoot 'Meyer\UnityProject'
$sources=Join-Path $project 'Assets\ShaySources'
New-Item -ItemType Directory -Path $sources -Force | Out-Null
'{"name":"S060.Shay.ModelSource"}' | Set-Content (Join-Path $sources 'S060.Shay.ModelSource.asmdef') -Encoding UTF8
foreach($item in @(
    @{source='engine\unity\Assets\EngineStudy\Generated'; target='Engine'},
    @{source='trucks\unity\Assets\ShayTrucks'; target='Trucks'},
    @{source='transmission\unity\Assets\Transmission\Generated'; target='Transmission'}
)) {
    $target=Join-Path $sources $item.target
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $SourcesRoot $item.source) -Force | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $target -Recurse -Force }
}
Copy-Item -LiteralPath (Join-Path $SourcesRoot 'transmission\unity\Assets\Transmission\ShayTransmissionPreview.cs') -Destination $sources -Force
Copy-Item -LiteralPath (Join-Path $SourcesRoot 'transmission\unity\Assets\Transmission\ShayTransmissionPreview.cs.meta') -Destination $sources -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Shay\Authoring\ShayBundleBuild.cs') -Destination (Join-Path $project 'Assets\Editor') -Force
$output=Join-Path $PSScriptRoot ('build\shay\bundle-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $output -Force | Out-Null
$env:S060_SHAY_BUNDLE_OUTPUT=$output
$process=Start-Process -FilePath $config.Unity -ArgumentList @('-projectPath',('"'+$project+'"'),'-executeMethod','ShayBundleBuild.RunBatch','-logFile',('"'+(Join-Path $output 'unity.log')+'"')) -WindowStyle Hidden -PassThru
Write-Output "Building original Shay modules: $output"
if(-not $process.WaitForExit(240000)) { $process.Kill(); throw 'Shay bundle build timed out.' }
if($process.ExitCode -ne 0 -or -not (Test-Path (Join-Path $output 'bundle-validation.json'))) { throw "Shay bundle failed: $output" }
[ordered]@{ passed=$true; sourceContract=$contract; bundle=Get-FileHash (Join-Path $output 'shay_running_gear') | Select-Object Path,Hash } | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $output 'bundle-receipt.json') -Encoding UTF8
Write-Output "Verified original-only Shay asset bundle: $output"
