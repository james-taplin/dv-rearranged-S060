param(
    [string]$Installer = 'C:\Users\james\Desktop\Tools\UnityModManagerInstaller',
    [string]$Version = '0.18.0'
)
$ErrorActionPreference = 'Stop'
# Run in Windows PowerShell/.NET Framework for UMM's WinForms assembly. No form
# is shown or constructed: only its native ZIP reader/extractor is exercised.
if ($PSVersionTable.PSEdition -ne 'Desktop') { throw 'Use Windows PowerShell for the native installer fixture.' }
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$package = Join-Path $repo "build\nexus-$Version"
$receiptPath = Join-Path $package 'nexus-package-receipt.json'
$receipt = Get-Content $receiptPath -Raw | ConvertFrom-Json
foreach ($archive in @($receipt.innerArchives) + @($receipt.zip)) {
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
$modsField = $formType.GetField('mods', $flags)
$mods = [Activator]::CreateInstance($modsField.FieldType)
$modsField.SetValue($form, $mods)
$logType = $console.GetType('UnityModManagerNet.ConsoleInstaller.Log', $true)
$logType.GetField('<Instance>k__BackingField', $flags).SetValue($null, [Activator]::CreateInstance($logType))
$install = $formType.GetMethods($flags) | Where-Object { $_.Name -eq 'InstallMod' -and $_.GetParameters().Count -eq 2 }
$read = $formType.GetMethods($flags) | Where-Object { $_.Name -eq 'ReadModInfoFromZip' -and $_.GetParameters()[0].ParameterType.FullName -eq 'Ionic.Zip.ZipFile' }
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
        $root = Join-Path $package 'mods'
        $relative = $file.Path.Substring($root.Length + 1)
        $target = Join-Path $FakeGame ('Mods\' + $relative)
        Check ((Get-FileHash -LiteralPath $target).Hash -eq $file.Hash) "Native UMM extracted incorrect bytes: $relative"
    }
}
try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $outer = [IO.Compression.ZipFile]::OpenRead($receipt.zip.Path)
    try {
        $expected = @($receipt.innerArchives | ForEach-Object { [IO.Path]::GetFileName($_.Path) }) + @('INSTALL.txt')
        Check (@(Compare-Object $expected @($outer.Entries | ForEach-Object FullName)).Count -eq 0) 'Outer download must contain exactly three mod ZIPs and installation instructions.'
    } finally { $outer.Dispose() }
    $extracted = Join-Path $output 'extracted-download'
    [IO.Compression.ZipFile]::ExtractToDirectory($receipt.zip.Path, $extracted)
    $ummArchives = @()
    foreach ($archive in $receipt.innerArchives) {
        $innerPath = Join-Path $extracted ([IO.Path]::GetFileName($archive.Path))
        Check ((Get-FileHash -LiteralPath $innerPath).Hash -eq $archive.Hash) 'Downloaded inner archive must match its frozen package hash.'
        $ummArchives += $innerPath
    }
    $fake = Join-Path $output 'positive-game'; Set-FakeGame $fake
    foreach ($archive in $ummArchives) { Install-Native $archive $true $fake }
    Verify-Payload $fake
    Check ($mods.Count -eq 3) 'Fresh native install must recognize three distinct mod identities.'
    $settings = Join-Path $fake 'Mods\RearrangedS060\Settings.xml'
    $unrelated = Join-Path $fake 'Mods\OtherMod\sentinel.txt'
    New-Item -ItemType Directory -Path (Split-Path $unrelated) -Force | Out-Null
    Set-Content -LiteralPath $settings -Value 'User settings fixture'
    Set-Content -LiteralPath $unrelated -Value 'Unrelated mod fixture'
    $settingsHash=(Get-FileHash $settings).Hash; $unrelatedHash=(Get-FileHash $unrelated).Hash
    foreach ($archive in $ummArchives) { Install-Native $archive $false $fake }
    Verify-Payload $fake
    Check ((Get-FileHash $settings).Hash -eq $settingsHash) 'Native update must retain user settings.'
    Check ((Get-FileHash $unrelated).Hash -eq $unrelatedHash) 'Native update must retain unrelated mods.'

    # Negative control: the old three-folder ZIP can extract fresh, but UMM's
    # installed-mod renaming path collapses sibling directories on an update.
    $negative = Join-Path $output 'negative-game'; Set-FakeGame $negative
    $oldZip = Join-Path $repo "build\articulated-$Version\RearrangedS060-$Version-articulated-test.zip"
    Install-Native $oldZip $true $negative
    Install-Native $oldZip $false $negative
    $coreInfo = Get-Content (Join-Path $negative 'Mods\RearrangedS060\Info.json') -Raw | ConvertFrom-Json
    Check ($coreInfo.Id -ne 'RearrangedS060') 'Negative control must reproduce UMM sibling-folder collapse.'
    [ordered]@{
        passed=$true; checks=$checks; nativeInstaller=$true; freshInstall=$true; repeatedUpdate=$true
        settingsAndOtherModsPreserved=$true; multiFolderUpdateNegativeControl=$true
        installerAssemblyVersion=$app.GetName().Version.ToString()
        managerVersion=([Reflection.Assembly]::LoadFrom((Join-Path $native 'UnityModManager.dll'))).GetName().Version.ToString()
        installerHash=(Get-FileHash (Join-Path $native 'UnityModManager.exe')).Hash
        packageReceiptSha256=(Get-FileHash $receiptPath).Hash; outerArchiveSha256=$receipt.zip.Hash
        gameFilesTouched=$false; nativeMethods=@('ReadModInfoFromZip','InstallMod(ZipFile,bool)')
    } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $output 'native-umm-validation.json') -Encoding UTF8
    Write-Output "`nPASS: $checks actual UMM package assertions. Evidence: $output"
} finally { $combo.Dispose() }
