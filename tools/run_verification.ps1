$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$assetValidator = Join-Path $PSScriptRoot 'validate_assets.py'
$manifest = Join-Path $root 'assets\asset_manifest.json'

python $assetValidator --self-test
python $assetValidator $manifest --allow-placeholders

if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $sdkList = dotnet --list-sdks
    if ($sdkList) {
        dotnet test (Join-Path $root 'tests\GlassesBar.Domain.Tests.csproj')
    } else {
        Write-Warning '.NET SDK is missing; domain tests were not run.'
    }
} else {
    Write-Warning 'dotnet is missing; domain tests were not run.'
}

$godotCommand = Get-Command godot -ErrorAction SilentlyContinue
if (-not $godotCommand) { $godotCommand = Get-Command godot4 -ErrorAction SilentlyContinue }
$godotPath = if ($godotCommand) { $godotCommand.Source } else { $null }
$portableGodot = 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
if (-not $godotPath -and (Test-Path -LiteralPath $portableGodot)) { $godotPath = $portableGodot }
if ($godotPath) {
    & $godotPath --headless --path $root --editor --quit
    & $godotPath --headless --path $root --quit-after 300 res://tests/godot/SmokeTests.tscn
    & $godotPath --headless --path $root --quit-after 300 res://tests/godot/InputIntegrationTests.tscn
    & $godotPath --headless --path $root --quit-after 300 res://tests/godot/FlowIntegrationTests.tscn
} else {
    Write-Warning 'Godot is missing; import and smoke tests were not run.'
}
