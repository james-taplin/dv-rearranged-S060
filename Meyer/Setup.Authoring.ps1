param([string]$ReferenceProject = 'B:\LLW CONVERT\locos\l74\unity')
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'UnityProject'
if (Test-Path -LiteralPath $project) { throw 'Authoring project already exists; preserve it and update deliberately.' }
$sdk = Join-Path $ReferenceProject 'Assets\CarCreator'
if (-not (Test-Path -LiteralPath (Join-Path $sdk 'Bin\CCL.Creator.dll'))) { throw 'Existing CarCreator SDK not found.' }
New-Item -ItemType Directory -Path (Join-Path $project 'Assets\Editor'),(Join-Path $project 'Packages'),(Join-Path $project 'ProjectSettings') -Force | Out-Null
Copy-Item -LiteralPath $sdk -Destination (Join-Path $project 'Assets\CarCreator') -Recurse
Copy-Item -LiteralPath (Join-Path $ReferenceProject 'Packages\manifest.json') -Destination (Join-Path $project 'Packages\manifest.json')
Copy-Item -LiteralPath (Join-Path $ReferenceProject 'ProjectSettings\ProjectVersion.txt') -Destination (Join-Path $project 'ProjectSettings\ProjectVersion.txt')
Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'Authoring') -Filter '*.cs' -File | Copy-Item -Destination (Join-Path $project 'Assets\Editor')
Write-Output "Prepared $project from the existing CCL SDK. Reference project untouched; no game files installed."
