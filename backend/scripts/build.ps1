param(
    [string]$Configuration = "Debug",
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root "turbo-deck-backend.sln"

if (-not $NoRestore) {
    dotnet restore $solution

    if ($LASTEXITCODE -ne 0) {
        throw "Restore failed. If a CommandQ.* package is unavailable, build the sibling command-q repository in Release first."
    }
}

dotnet build $solution --configuration $Configuration --no-restore

if ($LASTEXITCODE -ne 0) {
    throw "The command-q-back solution build failed."
}
