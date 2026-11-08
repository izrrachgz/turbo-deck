param(
    [string]$Environment = "Development",
    [int]$Port = 0,
    [string]$DataDirectory = "",
    [string]$ControlToken = "",
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repositoryRoot "Api\Api.csproj"

if ([string]::IsNullOrWhiteSpace($DataDirectory)) {
    $DataDirectory = Join-Path $repositoryRoot "Api\App_Data"
}

if ([string]::IsNullOrWhiteSpace($ControlToken)) {
    $tokenBytes = [byte[]]::new(32)
    [Security.Cryptography.RandomNumberGenerator]::Fill($tokenBytes)
    $ControlToken = [Convert]::ToBase64String($tokenBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

$env:ASPNETCORE_ENVIRONMENT = $Environment
$env:TURBO_DECK_CONTROL_TOKEN = $ControlToken
$env:TURBO_DECK_DATA_DIR = [IO.Path]::GetFullPath($DataDirectory)

Write-Host "Control token: $ControlToken"
Write-Host "Data directory: $env:TURBO_DECK_DATA_DIR"

$arguments = @("run", "--project", $project, "--no-launch-profile")
if ($NoBuild) {
    $arguments += "--no-build"
}
$arguments += @("--", "--Port=$Port")

do {
    dotnet @arguments
    $backendExitCode = $LASTEXITCODE
    if ($backendExitCode -eq 23) {
        Write-Host "Backend requested restart. Relaunching..."
    }
} while ($backendExitCode -eq 23)

exit $backendExitCode
