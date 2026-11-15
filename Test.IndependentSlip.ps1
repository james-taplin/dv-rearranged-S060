param([string]$RuntimePath = '', [string]$BaselinePath = '', [switch]$CompileOnly, [string]$ReusePlayerFrom = '')
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Build.Config.ps1')
$config = Get-S060BuildConfig
if (-not $RuntimePath) { $RuntimePath = Join-Path $PSScriptRoot 'build\stage7\RearrangedS060.dll' }
if (-not $BaselinePath) { $BaselinePath = Join-Path $PSScriptRoot 'build\nexus-0.3.0-single\package\RearrangedS060\RearrangedS060.dll' }
$RuntimePath = (Resolve-Path -LiteralPath $RuntimePath).Path
$BaselinePath = (Resolve-Path -LiteralPath $BaselinePath).Path
$hash = (Get-FileHash -LiteralPath $RuntimePath).Hash
Add-Type -Path (Join-Path $PSScriptRoot 'lib\AssetStudioModCLI_net472_win32_64\Mono.Cecil.dll')
$baseline = [Mono.Cecil.ModuleDefinition]::ReadModule($BaselinePath)
try {
    if (@($baseline.GetTypes() | Where-Object Name -eq 'S060IndependentDrive').Count) { throw 'Expected preserved shared-traction 0.3.0 baseline.' }
} finally { $baseline.Dispose() }
$run = Join-Path $PSScriptRoot ("build\independent-slip-$($config.Version)\native-" + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
$project = Join-Path $run 'fixture'
New-Item -ItemType Directory -Path (Join-Path $project 'Assets\Editor'),(Join-Path $project 'Assets\Bootstrap'),(Join-Path $project 'Packages'),(Join-Path $project 'ProjectSettings') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Meyer\Authoring\IndependentSlipNativeAudit.cs') -Destination (Join-Path $project 'Assets\Editor')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'tests\IndependentDrive\IndependentSlipPlayerBootstrap.cs') -Destination (Join-Path $project 'Assets\Bootstrap')
'{"name":"S060.IndependentSlipBootstrap","references":[],"autoReferenced":true}' | Set-Content (Join-Path $project 'Assets\Bootstrap\Bootstrap.asmdef') -Encoding utf8
'{"dependencies":{"com.unity.modules.physics":"1.0.0","com.unity.modules.jsonserialize":"1.0.0","com.unity.modules.animation":"1.0.0","com.unity.modules.particlesystem":"1.0.0","com.unity.modules.audio":"1.0.0"}}' | Set-Content (Join-Path $project 'Packages\manifest.json') -Encoding utf8
[IO.File]::WriteAllText((Join-Path $project 'ProjectSettings\ProjectVersion.txt'), "m_EditorVersion: 2019.4.40f1`r`nm_EditorVersionWithRevision: 2019.4.40f1 (ffc62b691db5)`r`n", [Text.Encoding]::ASCII)
$managed = Join-Path $config.GamePath 'DerailValley_Data\Managed'
[IO.File]::WriteAllText((Join-Path $run 'managed-path.txt'), $managed)
& $config.Dotnet build (Join-Path $PSScriptRoot 'tests\IndependentDrive\NativeProbe.csproj') -c Release "-p:ManagedPath=$managed" -o (Join-Path $run 'probe-bin') --ignore-failed-sources --nologo
if ($LASTEXITCODE -ne 0) { throw 'Independent slip native fixture compilation failed.' }
$receiptPath = Join-Path $run 'receipt.json'
$receipt = [ordered]@{ status='compiled_only'; runtimeSha256=$hash; runtimePath=$RuntimePath; baselineSha256=(Get-FileHash $BaselinePath).Hash; baselineMissingIndependentDrive=$true;
    realGameLaunched=$false; installed=$false; gamePlayerLoop=$false; run=$run; sourceHashes=@(Get-ChildItem (Join-Path $PSScriptRoot 'tests\IndependentDrive') -File | Get-FileHash | Select-Object Path,Hash) }
$receipt | ConvertTo-Json -Depth 6 | Set-Content $receiptPath -Encoding utf8
if ($CompileOnly) { Write-Output $receiptPath; return }
$env:S060_SLIP_RUNTIME = $RuntimePath
$env:S060_SLIP_CALIBRATION = Join-Path $config.QAPath 'stock-s060-engine-calibration.json'
if ($ReusePlayerFrom) {
    $previous = (Resolve-Path -LiteralPath $ReusePlayerFrom).Path
    $oldReceipt = Get-Content (Join-Path $previous 'receipt.json') -Raw | ConvertFrom-Json
    $bootstrapSource = Join-Path $PSScriptRoot 'tests\IndependentDrive\IndependentSlipPlayerBootstrap.cs'
    # Historical receipts retain their original location after consolidation.
    # Require the unique bootstrap filename and exact source hash, not its old path.
    $oldBootstrap = @($oldReceipt.sourceHashes | Where-Object { [IO.Path]::GetFileName($_.Path) -eq [IO.Path]::GetFileName($bootstrapSource) })
    if ($oldBootstrap.Count -ne 1 -or $oldBootstrap[0].Hash -ne (Get-FileHash $bootstrapSource).Hash -or
        -not (Test-Path (Join-Path $previous 'player\IndependentSlipFixture.exe'))) { throw 'Private player bootstrap provenance mismatch.' }
    Copy-Item -LiteralPath (Join-Path $previous 'player') -Destination $run -Recurse
    $receipt['playerReusedFrom'] = $previous
} else {
    $log = Join-Path $run 'unity.log'
    $process = Start-Process -FilePath $config.Unity -ArgumentList @('-projectPath',('"'+$project+'"'),'-executeMethod','IndependentSlipNativeAudit.BuildPlayer','-logFile',('"'+$log+'"')) -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(180000)) { throw "Private fixture build timeout; inspect $log" }
    if ($process.ExitCode -ne 0) { throw "Private fixture build failed; inspect $log" }
}
$playerLog = Join-Path $run 'player.log'
$player = Start-Process -FilePath (Join-Path $run 'player\IndependentSlipFixture.exe') -ArgumentList @('-batchmode','-nographics','-logFile',('"'+$playerLog+'"')) -WindowStyle Hidden -PassThru
if (-not $player.WaitForExit(90000)) { throw "Private fixture player timeout; inspect $playerLog" }
$resultPath = Join-Path $run 'result.json'
if (-not (Test-Path -LiteralPath $resultPath)) { throw "Native fixture stopped before report; inspect $playerLog" }
$result = Get-Content $resultPath -Raw | ConvertFrom-Json
$receipt.status = if ($player.ExitCode -eq 0 -and $result.passed -and $result.runtimeSha256 -eq $hash) { 'passed' } else { 'failed' }
$receipt['result'] = $resultPath; $receipt['checks'] = $result.checks
$receipt | ConvertTo-Json -Depth 6 | Set-Content $receiptPath -Encoding utf8
if ($receipt.status -ne 'passed' -or (Get-FileHash -LiteralPath $RuntimePath).Hash -ne $hash) { throw "Native fixture failed: $resultPath" }
[ordered]@{
    passed=$true; checks=$result.checks; runtimeSha256=$hash; nativeEcs=$true; productionCoordinator=$true;
    dockside=$true; meyer=$true; forceOnce=$true; cleanup=$true; effectsRestored=$true;
    lateLoadedAfterTypeManager=$true; gameLaunched=$false; installed=$false; gameplayValidated=$false;
    result=$resultPath; resultSha256=(Get-FileHash $resultPath).Hash; receipt=$receiptPath;
    fixtureSourceHashes=$receipt.sourceHashes; baselineMissingIndependentDrive=$true;
    nativeGameAssemblySha256=(Get-FileHash (Join-Path $managed 'Assembly-CSharp.dll')).Hash;
    limits='Private Unity release player. Actual candidate DLL, native ECS systems, engines, ports, coordinator and effects binding. Car host remains inactive and helpers active outside it. Final Bogie.ApplyForce intercepted for recording/fault injection; no rail dynamics, actual weather, audible sound, full gameplay scene or game saves exercised. Actual old/hot save compatibility is validated by the separate engine suite.'
} | ConvertTo-Json -Depth 7 | Set-Content (Join-Path $run 'independent-slip-validation.json') -Encoding utf8
Write-Output "Independent slip: $($result.checks) native offline checks passed; exact runtime $hash. Evidence $receiptPath"
