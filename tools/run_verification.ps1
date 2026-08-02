$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$assetValidator = Join-Path $PSScriptRoot 'validate_assets.py'
$manifest = Join-Path $root 'assets\asset_manifest.json'

function Assert-LastExitCode {
    param([Parameter(Mandatory = $true)][string]$Step)
    if ($LASTEXITCODE -ne 0) {
        throw "$Step failed with exit code $LASTEXITCODE."
    }
}

python $assetValidator --self-test
Assert-LastExitCode 'Asset validator self-test'
python $assetValidator $manifest --allow-placeholders
Assert-LastExitCode 'Asset manifest validation'

if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $sdkList = dotnet --list-sdks
    Assert-LastExitCode '.NET SDK discovery'
    if ($sdkList) {
        # Godot's editor import may reuse a loaded desktop editor's C# assembly. Build the
        # game/test assembly explicitly so headless integration scenes never run stale code.
        dotnet build (Join-Path $root 'GlassesBar.csproj') --configuration Debug --nologo
        Assert-LastExitCode 'Debug build'
        dotnet build (Join-Path $root 'GlassesBar.csproj') --configuration Release --nologo
        Assert-LastExitCode 'Release build'
        dotnet test (Join-Path $root 'tests\GlassesBar.Domain.Tests.csproj')
        Assert-LastExitCode 'Domain tests'
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
    Assert-LastExitCode 'Godot editor import'
    & $godotPath --headless --path $root --quit-after 300 res://tests/godot/SmokeTests.tscn
    Assert-LastExitCode 'Godot smoke tests'
    & $godotPath --headless --path $root --quit-after 300 res://tests/godot/Stage1AssetIntegrationTests.tscn
    Assert-LastExitCode 'Godot stage 1 asset integration tests'
    & $godotPath --headless --path $root --quit-after 300 res://tests/godot/Stage2AssetIntegrationTests.tscn
    Assert-LastExitCode 'Godot stage 2 asset integration tests'
    & $godotPath --headless --path $root --quit-after 300 res://tests/godot/InputIntegrationTests.tscn
    Assert-LastExitCode 'Godot input integration tests'
    & $godotPath --headless --path $root --quit-after 300 res://tests/godot/FlowIntegrationTests.tscn
    Assert-LastExitCode 'Godot flow integration tests'
} else {
    Write-Warning 'Godot is missing; import and smoke tests were not run.'
}
