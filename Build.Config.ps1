function Get-S060BuildConfig {
    param([string]$GamePath = '')
    $local = Join-Path $PSScriptRoot 'Build.Local.json'
    $settings = if (Test-Path -LiteralPath $local) { Get-Content $local -Raw | ConvertFrom-Json } else { [pscustomobject]@{} }
    function Setting([string]$name, [string]$fallback) {
        if ($settings.PSObject.Properties.Name -contains $name -and $settings.$name) { return [string]$settings.$name }
        return $fallback
    }
    [pscustomobject]@{
        Version = (Get-Content (Join-Path $PSScriptRoot 'Info.json') -Raw | ConvertFrom-Json).Version
        GamePath = if ($GamePath) { $GamePath } else { Setting 'GamePath' 'B:\SteamLibrary\steamapps\common\Derail Valley' }
        Dotnet = Setting 'Dotnet' 'C:\Program Files\dotnet\dotnet.exe'
        Unity = Setting 'Unity' 'B:\Games\Unity 2019.4.40f1\Editor\Unity.exe'
        Python = Setting 'Python' 'C:\Users\james\Desktop\Derail Valley Mods\Claudes Place\.venv312\Scripts\python.exe'
        ReferenceProject = Setting 'ReferenceProject' 'B:\LLW CONVERT\locos\l74\unity'
        QAPath = Join-Path $PSScriptRoot 'build\qa'
    }
}
