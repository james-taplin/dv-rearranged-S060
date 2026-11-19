# Builds the VR single-pass Standard shader bundle shipped as assets/Shared/s060_stereo_standard.
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Build.Config.ps1')
$config = Get-S060BuildConfig
$project = Join-Path $PSScriptRoot 'build\stereo-shaders\UnityProject'
foreach ($folder in @('Assets\Editor','Packages','ProjectSettings')) { New-Item -ItemType Directory -Path (Join-Path $project $folder) -Force | Out-Null }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Meyer\UnityProject\ProjectSettings\ProjectVersion.txt') -Destination (Join-Path $project 'ProjectSettings') -Force
'{"dependencies":{"com.unity.modules.assetbundle":"1.0.0","com.unity.modules.vr":"1.0.0","com.unity.modules.xr":"1.0.0"}}' | Set-Content (Join-Path $project 'Packages\manifest.json') -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Shared\Authoring\StereoShaderBuild.cs') -Destination (Join-Path $project 'Assets\Editor') -Force
$output = Join-Path $PSScriptRoot ('build\stereo-shaders\bundle-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $output -Force | Out-Null
$env:S060_STEREO_OUTPUT = $output
$process = Start-Process -FilePath $config.Unity -ArgumentList @('-batchmode','-nographics','-projectPath',('"'+$project+'"'),'-executeMethod','StereoShaderBuild.RunBatch','-logFile',('"'+(Join-Path $output 'unity.log')+'"')) -WindowStyle Hidden -PassThru
if (-not $process.WaitForExit(600000)) { $process.Kill(); throw 'Stereo shader build timed out.' }
if ($process.ExitCode -ne 0) { throw "Stereo shader build failed: $output" }
$bundle = Join-Path $output 's060_stereo_standard'
& $config.Python -B (Join-Path $PSScriptRoot 'Shared\tools\Check.StereoShader.py') $bundle (Join-Path $output 'stereo-validation.json')
if ($LASTEXITCODE -ne 0) { throw 'Built Standard shader lacks single-pass stereo variants.' }
[ordered]@{ passed=$true; unity=$config.Unity; bundle=Get-FileHash $bundle | Select-Object Path,Hash } | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $output 'bundle-receipt.json') -Encoding UTF8
Write-Output $bundle
