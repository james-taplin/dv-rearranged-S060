$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$version=(Get-Content (Join-Path $repo 'Info.json') -Raw | ConvertFrom-Json).Version
$real=Join-Path $repo "build\nexus-$version-single"
$output=Join-Path $repo ('build\nexus-install-fixture-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
$source=Join-Path $output 'source';$game=Join-Path $output 'fake-game'
$candidate=Join-Path $source "build\nexus-$version-single"
New-Item -ItemType Directory -Path $source,$candidate -Force | Out-Null
foreach($file in @('Install.Nexus.ps1','Build.Config.ps1','Info.json')){Copy-Item (Join-Path $repo $file) $source}
Copy-Item (Join-Path $real 'package') $candidate -Recurse
$receipt=Get-Content (Join-Path $real 'nexus-package-receipt.json') -Raw | ConvertFrom-Json
foreach($file in $receipt.files){$file.Path=$file.Path.Replace($real,$candidate)}
$receiptPath=Join-Path $candidate 'nexus-package-receipt.json'
$receipt | ConvertTo-Json -Depth 10 | Set-Content $receiptPath -Encoding UTF8
# Payload paths are re-rooted for this fixture; native UMM testing is separate.
$proof=Join-Path $candidate 'umm-proof-fixture';New-Item -ItemType Directory -Path $proof | Out-Null
@{passed=$true;packageReceiptSha256=(Get-FileHash $receiptPath).Hash} | ConvertTo-Json | Set-Content (Join-Path $proof 'native-umm-validation.json')
Copy-Item (Join-Path $real 'combined-bundle-validation.json') $candidate
$mods=Join-Path $game 'Mods';$main=Join-Path $mods 'RearrangedS060'
New-Item -ItemType Directory -Path $main,(Join-Path $mods 'OtherMod') -Force | Out-Null
Set-Content (Join-Path $main 'Settings.xml') 'Settings sentinel'
Set-Content (Join-Path $main 'Info.json') '{"Id":"RearrangedS060","Version":"0.18.0"}'
Set-Content (Join-Path $main 'RearrangedS060.Multiplayer.dll') 'Old optional bridge'
Set-Content (Join-Path $main 'MULTIPLAYER_TESTING.txt') 'Old optional bridge instructions'
Set-Content (Join-Path $mods 'OtherMod\Info.json') 'Unrelated sentinel'
foreach($id in @('RS060_Meyer_0440','RS060_Franklin_Dockside_0440')){
    $path=Join-Path $mods $id;New-Item -ItemType Directory -Path $path | Out-Null
    @{Id=$id;Version='0.7.0'} | ConvertTo-Json | Set-Content (Join-Path $path 'Info.json')
    Set-Content (Join-Path $path 'ccl_bundle') 'Old fixture bundle'
}
$settings=(Get-FileHash (Join-Path $main 'Settings.xml')).Hash
$other=(Get-FileHash (Join-Path $mods 'OtherMod\Info.json')).Hash
$gate=@{running=$true}
function Get-Process{[CmdletBinding()]param([string]$Name)if($Name -ne 'DerailValley'){throw 'Unexpected query.'};if($gate.running){[pscustomobject]@{ProcessName='DerailValley'}}}
$checks=0
function Check($value,$message){if(-not $value){throw $message};$script:checks++}
$installer=Join-Path $source 'Install.Nexus.ps1'
$rejected=$false
try{& $installer -GamePath $game}catch{if($_.Exception.Message -notmatch 'Close Derail Valley'){throw};$rejected=$true}
Check $rejected 'Running game must block all installation.'
Check ((Get-Content (Join-Path $main 'Info.json') -Raw | ConvertFrom-Json).Version -eq '0.18.0') 'Blocked installation must preserve old bytes.'
$gate.running=$false
$payload=Join-Path $candidate 'package\RearrangedS060\RearrangedS060.dll'
$bytes=[IO.File]::ReadAllBytes($payload);Set-Content $payload 'Tampered fixture'
$rejected=$false
try{& $installer -GamePath $game}catch{if($_.Exception.Message -notmatch 'Package changed'){throw};$rejected=$true}
Check $rejected 'Changed payload must be rejected before mutation.'
[IO.File]::WriteAllBytes($payload,$bytes)
& $installer -GamePath $game
foreach($file in $receipt.files){$relative=$file.Path.Substring((Join-Path $candidate 'package').Length+1);Check ((Get-FileHash (Join-Path $mods $relative)).Hash -eq $file.Hash) 'Every installed byte must match its receipt.'}
Check ((Get-FileHash (Join-Path $main 'Settings.xml')).Hash -eq $settings) 'Settings must survive.'
Check ((Get-FileHash (Join-Path $mods 'OtherMod\Info.json')).Hash -eq $other) 'Unrelated mod must survive.'
Check ((Test-Path -LiteralPath (Join-Path $main 'RearrangedS060.Multiplayer.dll')) -eq (Test-Path -LiteralPath (Join-Path $candidate 'package\RearrangedS060\RearrangedS060.Multiplayer.dll'))) 'Optional bridge presence must match the package.'
Check ((Test-Path -LiteralPath (Join-Path $main 'MULTIPLAYER_TESTING.txt')) -eq (Test-Path -LiteralPath (Join-Path $candidate 'package\RearrangedS060\MULTIPLAYER_TESTING.txt'))) 'Optional bridge instructions must match the package.'
Check (-not (Test-Path (Join-Path $mods 'RS060_Meyer_0440')) -and -not (Test-Path (Join-Path $mods 'RS060_Franklin_Dockside_0440'))) 'Old donor packs must be retired to avoid duplicate CCL IDs.'
& $installer -GamePath $game
Check ((Get-FileHash (Join-Path $main 'Settings.xml')).Hash -eq $settings) 'Repeated installation must preserve settings.'
@{passed=$true;checks=$checks;realGameTouched=$false;nativeUmmTestSeparate=$true} | ConvertTo-Json | Set-Content (Join-Path $output 'validation.json')
Write-Output "$checks single-folder installer checks passed; no real game files changed. $output"
