param(
    [ValidateSet("win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")]
    [string]$Runtime = "win-x64",
    [switch]$RequireSigning,
    [switch]$ExternalSigningCandidate,
    [switch]$Publish
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$env:NODE_OPTIONS = "--use-system-ca"
Remove-Item Env:TURBO_DECK_REQUIRE_SIGNING -ErrorAction SilentlyContinue
Remove-Item Env:TURBO_DECK_EXTERNAL_SIGNING -ErrorAction SilentlyContinue
Remove-Item Env:TURBO_DECK_OFFICIAL_RELEASE -ErrorAction SilentlyContinue

if ($RequireSigning) {
    $env:TURBO_DECK_REQUIRE_SIGNING = "1"
    $env:TURBO_DECK_OFFICIAL_RELEASE = "1"
}

if ($ExternalSigningCandidate) {
    if ($env:GITHUB_ACTIONS -ne "true") {
        throw "External signing candidates may only be created by a reviewed GitHub Actions release workflow."
    }

    $env:TURBO_DECK_EXTERNAL_SIGNING = "1"
    $env:TURBO_DECK_OFFICIAL_RELEASE = "1"
}

if ($RequireSigning -and $ExternalSigningCandidate) {
    throw "Use either -RequireSigning for a local certificate or -ExternalSigningCandidate, not both."
}

if ($Publish -and (-not $RequireSigning -or $ExternalSigningCandidate)) {
    throw "Direct publishing requires local signing. External candidates must be signed, verified, and published by a release workflow."
}

if ($Publish -and [string]::IsNullOrWhiteSpace($env:GH_TOKEN)) {
    throw "GH_TOKEN is required to publish a GitHub release."
}

if ($RequireSigning -and $Runtime.StartsWith("win-") -and
    [string]::IsNullOrWhiteSpace($env:WIN_CSC_LINK) -and [string]::IsNullOrWhiteSpace($env:CSC_LINK)) {
    throw "WIN_CSC_LINK or CSC_LINK is required for a signed Windows release."
}

Push-Location (Join-Path $repositoryRoot "desktop")
try {
    npm ci
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    npm run build:all
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}

& (Join-Path $PSScriptRoot "publish-backend.ps1") -Runtime $Runtime -Configuration Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Push-Location (Join-Path $repositoryRoot "desktop")
try {
    $packageArguments = @(".\electron\package.js", $Runtime)
    if ($Publish) { $packageArguments += "--publish" }
    node @packageArguments
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}

$installerDirectory = Join-Path $repositoryRoot "artifacts\installer"
$artifacts = Get-ChildItem -LiteralPath $installerDirectory -File |
    Where-Object { $_.Extension -in ".exe", ".AppImage", ".dmg", ".zip" -and $_.Name -notlike "*.blockmap" }
foreach ($artifact in $artifacts) {
    $hash = (Get-FileHash -LiteralPath $artifact.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $checksumPath = "$($artifact.FullName).sha256"
    [IO.File]::WriteAllText($checksumPath, "$hash  $($artifact.Name)`n", [Text.UTF8Encoding]::new($false))
}

Write-Host "Turbo Deck $Runtime package and SHA-256 checksum generated under $installerDirectory"
