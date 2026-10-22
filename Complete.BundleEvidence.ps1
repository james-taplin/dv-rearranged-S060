param([Parameter(Mandatory)][string]$Evidence)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($Evidence)
$reports = @(Get-ChildItem -LiteralPath $root -File -Filter '*validation.json')
foreach ($report in $reports) { if (-not (Get-Content $report.FullName -Raw | ConvertFrom-Json).passed) { throw "Failed authoring report: $($report.Name)" } }
$bundle = @(Get-ChildItem -LiteralPath $root -Recurse -File -Filter ccl_bundle)
if ($bundle.Count -ne 1) { throw 'Expected one exported bundle.' }
if ((Get-Content (Join-Path $root 'bundle-validation.json') -Raw | ConvertFrom-Json).bundleSha256 -ne (Get-FileHash $bundle[0].FullName).Hash) { throw 'Export differs from the reopened, validated bundle.' }
$files = @($reports) + $bundle + @(Get-Item (Join-Path $bundle[0].DirectoryName 'Info.json'))
[ordered]@{ utc=[DateTime]::UtcNow.ToString('o'); files=@($files | ForEach-Object {
    [ordered]@{ relative=$_.FullName.Substring($root.Length).TrimStart('\'); hash=(Get-FileHash $_.FullName).Hash }
}) } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $root 'bundle-evidence.json') -Encoding UTF8
& (Join-Path $PSScriptRoot 'Test.BundleEvidence.ps1') -Evidence $root
