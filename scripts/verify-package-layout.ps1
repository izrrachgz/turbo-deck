param(
    [ValidateSet("win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")]
    [string]$Runtime
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if (-not $Runtime.StartsWith("win-")) {
    Write-Host "Package layout verification is currently defined for Windows installers; skipped for $Runtime."
    exit 0
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$installerDirectory = Join-Path $repositoryRoot "artifacts\installer"
$unpackedDirectory = Join-Path $installerDirectory "win-unpacked"
$resourcesDirectory = Join-Path $unpackedDirectory "resources"
$backendDirectory = Join-Path $resourcesDirectory "backend"
$version = (Get-Content -Raw (Join-Path $repositoryRoot "desktop\package.json") | ConvertFrom-Json).version
$architecture = $Runtime.Substring("win-".Length)

$requiredFiles = @(
    (Join-Path $unpackedDirectory "Turbo Deck.exe"),
    (Join-Path $resourcesDirectory "app.asar"),
    (Join-Path $resourcesDirectory "LICENSE.txt"),
    (Join-Path $resourcesDirectory "helpers\configure-firewall.ps1"),
    (Join-Path $backendDirectory "TurboDeck.Backend.exe"),
    (Join-Path $backendDirectory "appsettings.json"),
    (Join-Path $backendDirectory "appsettings.Production.json"),
    (Join-Path $backendDirectory "wwwroot\deck\index.html"),
    (Join-Path $backendDirectory "wwwroot\visualizer\index.html"),
    (Join-Path $installerDirectory "Turbo-Deck-Setup-$version-$architecture.exe"),
    (Join-Path $installerDirectory "Turbo-Deck-Setup-$version-$architecture.exe.blockmap"),
    (Join-Path $installerDirectory "latest.yml")
)
$requiredDirectories = @(
    (Join-Path $backendDirectory "locales")
)
$missing = @(
    $requiredFiles | Where-Object { -not (Test-Path -LiteralPath $_ -PathType Leaf) }
    $requiredDirectories | Where-Object { -not (Test-Path -LiteralPath $_ -PathType Container) }
)

if ($missing.Count -gt 0) {
    throw "Packaged runtime layout is incomplete:`n$($missing -join "`n")"
}

$manifest = Get-Content -Raw -LiteralPath (Join-Path $installerDirectory "latest.yml")
if ($manifest -notmatch "(?m)^version:\s+$([regex]::Escape($version))\s*$") {
    throw "latest.yml does not identify package version $version."
}

node (Join-Path $PSScriptRoot "verify-packaged-ui.js") (Join-Path $resourcesDirectory "app.asar")
if ($LASTEXITCODE -ne 0) {
    throw "Packaged UI verification failed."
}

Write-Host "Verified the packaged Turbo Deck $version Windows runtime layout."
