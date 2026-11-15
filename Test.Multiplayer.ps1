param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'build\multiplayer-0.3.6'))
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
foreach ($suite in @('Bridge','Core','Ports')) {
    $result = if ($suite -eq 'Ports') { $OutputDirectory } else { Join-Path $OutputDirectory "$suite.json" }
    & 'C:\Program Files\dotnet\dotnet.exe' run --project (Join-Path $PSScriptRoot "tests\Multiplayer\$suite.csproj") -c Release -- $result 2>&1 | Tee-Object (Join-Path $OutputDirectory "$suite-test.log")
    if ($LASTEXITCODE -ne 0) { throw "$suite multiplayer fixture failed." }
}
Write-Output 'Managed multiplayer checks passed. Real peer testing belongs to James.'

