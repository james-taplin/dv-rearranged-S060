param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
Write-Output 'Test.Exhaust now forwards to the complete native lifecycle suite in Test.Runtime.ps1.'
& (Join-Path $PSScriptRoot 'Test.Runtime.ps1') -SkipBuild:$SkipBuild
