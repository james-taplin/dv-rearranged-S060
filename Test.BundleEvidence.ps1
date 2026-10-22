param([Parameter(Mandatory)][string]$Evidence)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($Evidence)
$receipt = Get-Content (Join-Path $root 'bundle-evidence.json') -Raw | ConvertFrom-Json
$requiredReports = @('bundle-validation.json','motion-validation.json','steam-pipe-validation.json','oilcup-geometry-validation.json','normal-rod-validation.json','native-pin-fit-validation.json')
$bundleEntries = @($receipt.files | Where-Object relative -Like '*\ccl_bundle')
if ($bundleEntries.Count -ne 1) { throw 'Exactly one bundle must be validated.' }
$pack = Split-Path $bundleEntries[0].relative -Parent
if ($pack -eq 'RS060_Meyer_0440') { $requiredReports += 'meyer-lights-validation.json' }
$required = @($requiredReports) + @((Join-Path $pack 'ccl_bundle'), (Join-Path $pack 'Info.json'))
foreach ($name in $required) { if (@($receipt.files | Where-Object relative -eq $name).Count -ne 1) { throw "Missing/duplicate evidence: $name" } }
if (@($receipt.files).Count -ne $required.Count) { throw 'Unexpected evidence entries.' }
foreach ($file in $receipt.files) {
    $path = [IO.Path]::GetFullPath((Join-Path $root $file.relative))
    if (-not $path.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Evidence path escapes its directory.' }
    if ((Get-FileHash -LiteralPath $path).Hash -ne $file.hash) { throw "Bundle evidence changed: $($file.relative)" }
}
foreach ($report in $requiredReports) { if (-not (Get-Content (Join-Path $root $report) -Raw | ConvertFrom-Json).passed) { throw "Failed evidence: $report" } }
$validatedHash = (Get-Content (Join-Path $root 'bundle-validation.json') -Raw | ConvertFrom-Json).bundleSha256
if (-not $validatedHash -or $validatedHash -ne (Get-FileHash (Join-Path $root $bundleEntries[0].relative)).Hash) { throw 'Bundle differs from the binary reopened by the Unity audit.' }
if (@($receipt.files | Where-Object relative -Like '*\ccl_bundle').Count -ne 1 -or
    @($receipt.files | Where-Object relative -eq 'bundle-validation.json').Count -ne 1) { throw 'Incomplete bundle evidence receipt.' }
