param(
    [string]$Installer = 'C:\Users\james\Desktop\Tools\UnityModManagerInstaller',
    [string]$Version = '',
    [string]$LegacyArchive = ''
)
$ErrorActionPreference = 'Stop'
# Run in Windows PowerShell/.NET Framework for UMM's WinForms assembly. No form
# is shown or constructed: only its native ZIP reader/extractor is exercised.
if ($PSVersionTable.PSEdition -ne 'Desktop') { throw 'Use Windows PowerShell for the native installer fixture.' }
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if (-not $Version) { $Version = (Get-Content (Join-Path $repo 'Info.json') -Raw | ConvertFrom-Json).Version }
$package = Join-Path $repo "build\nexus-$Version-single"
$receiptPath = Join-Path $package 'nexus-package-receipt.json'
$receipt = Get-Content $receiptPath -Raw | ConvertFrom-Json
foreach ($archive in @($receipt.zip)) {
    if ((Get-FileHash -LiteralPath $archive.Path).Hash -ne $archive.Hash) { throw 'Nexus archive changed.' }
}
$output = Join-Path $package ('umm-proof-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
$native = Join-Path $output 'native-installer'
New-Item -ItemType Directory -Path $native -Force | Out-Null
foreach ($name in @('UnityModManager.exe','Console.exe','UnityModManager.dll','Ionic.Zip.dll','Newtonsoft.Json.dll','dnlib.dll','0Harmony.dll')) {
    Copy-Item -LiteralPath (Join-Path $Installer $name) -Destination $native
    Unblock-File -LiteralPath (Join-Path $native $name)
}
Add-Type -AssemblyName System.Windows.Forms
$app = [Reflection.Assembly]::LoadFrom((Join-Path $native 'UnityModManager.exe'))
$console = [Reflection.Assembly]::LoadFrom((Join-Path $native 'Console.exe'))
$zipAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $native 'Ionic.Zip.dll'))
$flags = [Reflection.BindingFlags]'Instance,Static,Public,NonPublic'
$formType = $app.GetType('UnityModManagerNet.Installer.UnityModManagerForm', $true)
$form = [Runtime.Serialization.FormatterServices]::GetUninitializedObject($formType)
$game = [Activator]::CreateInstance($console.GetType('UnityModManagerNet.ConsoleInstaller.GameInfo', $true))
$game.Name='S060 isolated package fixture'; $game.Folder='S060Fixture'; $game.ModsDirectory='Mods'; $game.ModInfo='Info.json'
$combo = New-Object System.Windows.Forms.ComboBox
$null = $combo.Items.Add($game); $combo.SelectedIndex=0
$formType.GetField('gameList', $flags).SetValue($form, $combo)
$tabs = New-Object System.Windows.Forms.TabControl
$formType.GetField('tabControl', $flags).SetValue($form, $tabs)
$list = New-Object System.Windows.Forms.ListView
$formType.GetField('listMods', $flags).SetValue($form, $list)
$modsField = $formType.GetField('mods', $flags)
$mods = [Activator]::CreateInstance($modsField.FieldType)
$modsField.SetValue($form, $mods)
$logType = $console.GetType('UnityModManagerNet.ConsoleInstaller.Log', $true)
$logType.GetField('<Instance>k__BackingField', $flags).SetValue($null, [Activator]::CreateInstance($logType))
$install = $formType.GetMethods($flags) | Where-Object { $_.Name -eq 'InstallMod' -and $_.GetParameters().Count -eq 2 }
$read = $formType.GetMethods($flags) | Where-Object { $_.Name -eq 'ReadModInfoFromZip' -and $_.GetParameters()[0].ParameterType.FullName -eq 'Ionic.Zip.ZipFile' }
$reload = $formType.GetMethod('ReloadMods', $flags)
$uninstall = $formType.GetMethod('UninstallMod', $flags)
$zipRead = $zipAssembly.GetType('Ionic.Zip.ZipFile', $true).GetMethod('Read', [Type[]]@([string]))
$checks = 0
function Check([bool]$Value, [string]$Message) {
    if (-not $Value) { throw $Message }; $script:checks++
}
function Set-FakeGame([string]$Path) {
    $absolute = [IO.Path]::GetFullPath($Path)
    if (-not $absolute.StartsWith($output + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Native fixture must stay inside its scratch output.' }
    New-Item -ItemType Directory -Path (Join-Path $absolute 'Mods') -Force | Out-Null
    $formType.GetField('gamePath', $flags).SetValue($null, $absolute)
    $mods.Clear()
}
function Install-Native([string]$Archive, [bool]$Remember, [string]$FakeGame) {
    $zip = $zipRead.Invoke($null, @($Archive))
    try {
        $readArgs = New-Object object[] 1; $readArgs[0] = $zip
        $info = $read.Invoke($form, $readArgs)
        Check ($null -ne $info -and $info.Id) 'Actual UMM must read the mod identity.'
        $installArgs = New-Object object[] 2; $installArgs[0] = $zip; $installArgs[1] = $false
        $install.Invoke($form, $installArgs)
        if ($Remember) {
            $info.Path = Join-Path $FakeGame ('Mods\' + $info.Id)
            $info.Status = [Enum]::Parse($info.Status.GetType(), 'Installed')
            $mods.Add($info)
        }
    } finally { $zip.Dispose() }
}
function Verify-Payload([string]$FakeGame) {
    foreach ($file in $receipt.files) {
        $root = Join-Path $package 'package'
        $relative = $file.Path.Substring($root.Length + 1)
        $target = Join-Path $FakeGame ('Mods\' + $relative)
        Check ((Get-FileHash -LiteralPath $target).Hash -eq $file.Hash) "Native UMM extracted incorrect bytes: $relative"
    }
}
function Uninstall-Native([string]$Id) {
    $found = @($mods | Where-Object Id -eq $Id)
    if ($found.Count -ne 1) { throw 'Expected exactly one installed fixture mod.' }
    $target = [IO.Path]::GetFullPath($found[0].Path)
    if (-not $target.StartsWith($output + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Uninstall target escaped the fixture.' }
    $uninstallArgs = New-Object object[] 1; $uninstallArgs[0] = $found[0].PSObject.BaseObject
    $uninstall.Invoke($form, $uninstallArgs)
    Check (-not (Test-Path -LiteralPath $target)) "Native uninstall did not remove $Id"
}
try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $outer = [IO.Compression.ZipFile]::OpenRead($receipt.zip.Path)
    try {
        Check (@($outer.Entries | Where-Object { $_.FullName -match '(?i)\.(zip|7z|rar)$' }).Count -eq 0) 'Release must contain no nested archives.'
        Check (@($outer.Entries | Where-Object { -not $_.FullName.StartsWith('RearrangedS060/') }).Count -eq 0) 'Every entry must be inside the single mod folder.'
        Check (@($outer.Entries | Where-Object { $_.Name -eq 'Info.json' }).Count -eq 1) 'Exactly one UMM mod identity is allowed.'
    } finally { $outer.Dispose() }
    $fake = Join-Path $output 'positive-game'; Set-FakeGame $fake
    Install-Native $receipt.zip.Path $true $fake
    Verify-Payload $fake
    Check ($mods.Count -eq 1 -and $mods[0].Id -eq 'RearrangedS060') 'Fresh native install must recognize one combined mod.'
    Check (@(Get-ChildItem (Join-Path $fake 'Mods\RearrangedS060') -Recurse -File -Filter ccl_bundle).Count -eq 2) 'Both native CCL bundles must be nested below the active mod.'
    $settings = Join-Path $fake 'Mods\RearrangedS060\Settings.xml'
    $unrelated = Join-Path $fake 'Mods\OtherMod\sentinel.txt'
    New-Item -ItemType Directory -Path (Split-Path $unrelated) -Force | Out-Null
    Set-Content -LiteralPath $settings -Value 'User settings fixture'
    Set-Content -LiteralPath $unrelated -Value 'Unrelated mod fixture'
    $settingsHash=(Get-FileHash $settings).Hash; $unrelatedHash=(Get-FileHash $unrelated).Hash
    Install-Native $receipt.zip.Path $false $fake
    Verify-Payload $fake
    Check ((Get-FileHash $settings).Hash -eq $settingsHash) 'Native update must retain user settings.'
    Check ((Get-FileHash $unrelated).Hash -eq $unrelatedHash) 'Native update must retain unrelated mods.'

    Uninstall-Native 'RearrangedS060'
    Check ((Get-FileHash $unrelated).Hash -eq $unrelatedHash) 'Uninstall must retain unrelated mods.'

    # Actual UMM uninstall/update migration from the previous three-part layout.
    $migration = Join-Path $output 'migration-game'; Set-FakeGame $migration
    $oldZip = if ($LegacyArchive) { [IO.Path]::GetFullPath($LegacyArchive) } else { Join-Path $repo "build\articulated-$Version\RearrangedS060-$Version-articulated-test.zip" }
    Install-Native $oldZip $false $migration
    $reload.Invoke($form, @())
    Check ($mods.Count -eq 3) 'Legacy fixture must contain all three original mod identities.'
    $migrationSettings = Join-Path $migration 'Mods\RearrangedS060\Settings.xml'
    Set-Content -LiteralPath $migrationSettings -Value 'Existing S060 user settings'
    $migrationSettingsHash=(Get-FileHash $migrationSettings).Hash
    Uninstall-Native 'RS060_Meyer_0440'
    Uninstall-Native 'RS060_Franklin_Dockside_0440'
    Check ((Get-FileHash $migrationSettings).Hash -eq $migrationSettingsHash) 'Removing old asset packs must retain main settings.'
    Install-Native $receipt.zip.Path $false $migration
    $reload.Invoke($form, @())
    Verify-Payload $migration
    Check ($mods.Count -eq 1 -and $mods[0].Id -eq 'RearrangedS060') 'Migrated installation must have one mod identity.'
    Check ((Get-FileHash $migrationSettings).Hash -eq $migrationSettingsHash) 'Combined update must retain existing S060 settings.'
    Check (@(Get-ChildItem (Join-Path $migration 'Mods') -Recurse -File -Filter ccl_bundle).Count -eq 2) 'Migration must leave only the two bundled assets, without duplicate old packs.'
    $combinedRoot = Join-Path $migration 'Mods\RearrangedS060'
    $combinedInfo=Get-Content (Join-Path $combinedRoot 'Info.json') -Raw | ConvertFrom-Json
    Check ($combinedInfo.Requirements -contains 'DVCustomCarLoader' -and $combinedInfo.Requirements -contains 'LocoMeshSplitter') 'Combined metadata must require both native frameworks.'

    # Inspect the exact installed CCL implementation rather than inventing a
    # custom loader: it recursively scans bundles and saves its own settings.
    Add-Type -Path (Join-Path $repo 'lib\AssetStudioModCLI_net472_win32_64\Mono.Cecil.dll')
    $cclPath='B:\SteamLibrary\steamapps\common\Derail Valley\Mods\DVCustomCarLoader\CCL.Importer.dll'
    $ccl=[Mono.Cecil.ModuleDefinition]::ReadModule($cclPath)
    try {
        $load=(($ccl.Types | Where-Object FullName -eq 'CCL.Importer.CarManager').Methods | Where-Object Name -eq 'LoadCarDefinitions').Body.Instructions
        $enumerate=@($load | Where-Object { $_.OpCode.Code -eq 'Call' -and $_.Operand.FullName -match 'Directory::EnumerateFiles' })
        Check ($enumerate.Count -eq 1 -and $enumerate[0].Previous.OpCode.Code -eq 'Ldc_I4_1') 'Installed CCL must scan SearchOption.AllDirectories.'
        Check (@($load | Where-Object { $_.OpCode.Code -eq 'Call' -and $_.Operand.FullName -match 'AssetBundle::LoadFromFile' }).Count -eq 1) 'Installed CCL must use native bundle loading.'
        $save=(($ccl.Types | Where-Object FullName -eq 'CCL.Importer.LoadedModSettings').Methods | Where-Object Name -eq 'Save').Body.Instructions
        Check (@($save | Where-Object { $_.OpCode.Code -eq 'Ldsfld' -and $_.Operand.FullName -match 'CCLPlugin::Instance$' }).Count -eq 1) 'CCL must save its settings to its own mod entry, not overwrite S060 settings.'
    } finally {$ccl.Dispose()}
    [ordered]@{
        passed=$true; checks=$checks; nativeInstaller=$true; freshInstall=$true; repeatedUpdate=$true; uninstall=$true
        legacyMigration=$true; settingsAndOtherModsPreserved=$true; noNestedArchives=$true; singleIdentity=$true
        nativeCclRecursiveDiscovery=$true; nativeCclSettingsOwnership=$true; cclImporterHash=(Get-FileHash $cclPath).Hash
        installedFixture=$combinedRoot
        installerAssemblyVersion=$app.GetName().Version.ToString()
        managerVersion=([Reflection.Assembly]::LoadFrom((Join-Path $native 'UnityModManager.dll'))).GetName().Version.ToString()
        installerHash=(Get-FileHash (Join-Path $native 'UnityModManager.exe')).Hash
        packageReceiptSha256=(Get-FileHash $receiptPath).Hash; outerArchiveSha256=$receipt.zip.Hash
        gameFilesTouched=$false; nativeMethods=@('ReadModInfoFromZip','InstallMod(ZipFile,bool)')
    } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $output 'native-umm-validation.json') -Encoding UTF8
    Write-Output "`nPASS: $checks actual UMM package assertions. Evidence: $output"
} finally { $combo.Dispose(); $tabs.Dispose(); $list.Dispose() }
