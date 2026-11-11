param([ValidateSet('geometry','preview','cocks','effects')][string]$Only='geometry',[string]$Runtime='')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
. (Join-Path $repo 'Build.Config.ps1')
$config=Get-S060BuildConfig
$project=Join-Path $repo 'build\shay-r3-authoring\UnityProject'
if(-not $Runtime){$Runtime=Join-Path $repo 'build\stage7\RearrangedS060.dll'}
$runtimeHash=(Get-FileHash -LiteralPath $Runtime).Hash
$output=Join-Path $repo ('build\shay-r3-20261111\native-'+$Only+'-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $output -Force | Out-Null
# Reuse the existing native fixture in the separate authoring project. Remove
# only the unrelated CCL Meyer preview, so no second CCL SDK is needed here.
$audit=Get-Content -LiteralPath (Join-Path $repo 'Meyer\Authoring\ConventionalNativeAudit.cs') -Raw
$start=$audit.IndexOf('    private static void ValidateMeyerBody(')
$end=$audit.IndexOf('    private static void RenderWide(', $start)
$audit=$audit.Remove($start,$end-$start)
$audit=$audit -replace '(?m)^.*if\(!string.IsNullOrEmpty\(Environment.GetEnvironmentVariable\("S060_MEYER_AUDIT_BUNDLE"\)\)\).*\r?\n',''
$audit=$audit.Replace('SteamNativePreview.NativeCylinder','NativeCylinder')
$insert='    private static bool NativeCylinder(Vector3 p,int side) => p.x*side>=.77f&&p.x*side<=4&&p.y>=0&&p.y<=1.57f&&p.z>=2.55f&&p.z<=3.7f;'
$audit=$audit.Replace('    private static int checks;', $insert+[Environment]::NewLine+'    private static int checks;')
$audit=$audit.Replace('fiveDrivenAxles = true','fiveDrivenAxles = false').Replace('tenOilSites = true','tenOilSites = false')
$audit | Set-Content -LiteralPath (Join-Path $project 'Assets\Editor\ConventionalNativeAudit.cs') -Encoding UTF8
foreach($name in @('SteamMeshContact.cs','CockNativeAudit.cs')){Copy-Item -LiteralPath (Join-Path $repo ('Meyer\Authoring\'+$name)) -Destination (Join-Path $project 'Assets\Editor') -Force}
$manifest=Get-Content (Join-Path $project 'Packages\manifest.json') -Raw | ConvertFrom-Json
foreach($name in @('audio','animation','particlesystem')){$manifest.dependencies | Add-Member -NotePropertyName ('com.unity.modules.'+$name) -NotePropertyValue '1.0.0' -Force}
$manifest | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $project 'Packages\manifest.json') -Encoding UTF8
$env:S060_ONLY_SHAY='1'
$env:S060_SHAY_PREVIEW_ONLY=if($Only -eq 'preview'){'1'}else{''}
$env:S060_AUDIT_MANAGED=Join-Path $config.GamePath 'DerailValley_Data\Managed'
$env:S060_AUDIT_MODS=Join-Path $config.GamePath 'Mods'
$env:S060_EXHAUST_AUDIT_RUNTIME=$Runtime
$env:S060_CONVENTIONAL_NATIVE=Join-Path $repo 'build\qa\conventional-native.json'
$env:S060_NATIVE_PREVIEW=Join-Path $repo 'build\qa\native-preview.json'
$bundle=Get-ChildItem (Join-Path $repo 'build\shay') -Directory -Filter 'bundle-*' | Where-Object {Test-Path (Join-Path $_.FullName 'bundle-receipt.json')} | Sort-Object Name -Descending | Select-Object -First 1
$env:S060_SHAY_AUDIT_BUNDLE=Join-Path $bundle.FullName 'shay_running_gear'
$env:S060_CONVENTIONAL_OUTPUT=$output
$env:S060_COCK_AUDIT_OUTPUT=$output
$method=if($Only -in @('geometry','preview')){'ConventionalNativeAudit.RunBatch'}else{'CockNativeAudit.RunBatch'}
if($Only -eq 'effects'){throw 'Use the existing isolated PlayMode runner for effects lifecycle.'}
$process=Start-Process -FilePath $config.Unity -ArgumentList @('-projectPath',('"'+$project+'"'),'-executeMethod',$method,'-logFile',('"'+(Join-Path $output 'unity.log')+'"')) -WindowStyle Hidden -PassThru
Write-Output "Testing Shay $Only in isolated authoring project: $output"
if(-not $process.WaitForExit(180000)){$process.Kill();throw "Shay native fixture timed out: $output"}
$report=Join-Path $output $(if($Only -in @('geometry','preview')){'conventional-validation.json'}else{'native-cock-validation.json'})
if($process.ExitCode -ne 0 -or -not (Test-Path $report)){throw "Shay native fixture failed: $output"}
$data=Get-Content $report -Raw | ConvertFrom-Json
if(-not $data.passed){throw "Shay assertions failed: $output"}
if((Get-FileHash $Runtime).Hash -ne $runtimeHash){throw 'Runtime changed during isolated Shay validation.'}
[ordered]@{passed=$true;runtimeSha256=$runtimeHash;report=Get-FileHash $report|Select-Object Path,Hash;gameplay=$false;installed=$false}|ConvertTo-Json -Depth 4|Set-Content (Join-Path $output 'receipt.json')
Write-Output "Shay $Only passed $($data.checks) checks. $output"
