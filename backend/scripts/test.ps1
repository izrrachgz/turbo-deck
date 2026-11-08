param(
    [string]$Configuration = "Debug",
    [string]$Project = "turbo-deck-backend.sln",
    [string]$Filter = "",
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$target = Join-Path $root $Project
$arguments = @("test", $target, "--configuration", $Configuration)

if ($NoBuild) {
    $arguments += "--no-build"
}

if (-not [string]::IsNullOrWhiteSpace($Filter)) {
    $arguments += @("--filter", $Filter)
}

dotnet @arguments

if ($LASTEXITCODE -ne 0) {
    throw "Tests failed for '$Project'."
}
