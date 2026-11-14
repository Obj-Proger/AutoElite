<#
.SYNOPSIS
    Starts AutoElite for development: PostgreSQL in Docker, then the API and the website, each in a window of its own.

.DESCRIPTION
    Run scripts/Initialize-DevEnvironment.ps1 once first. The API answers at http://localhost:5080 (its reference at
    /scalar) and the website at http://localhost:5173. Close a window to stop its part; PostgreSQL in Docker keeps
    running until `docker compose -f compose.dev.yaml down`, and keeps its data until the same command with -v.

.PARAMETER ExistingDatabase
    Uses the PostgreSQL server that the user secrets name, which already runs, and starts nothing in Docker.

.EXAMPLE
    .\scripts\Start-DevEnvironment.ps1

.EXAMPLE
    .\scripts\Start-DevEnvironment.ps1 -ExistingDatabase
#>
[CmdletBinding()]
param(
    [switch] $ExistingDatabase
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot

if (-not $ExistingDatabase) {
    if (-not (Test-Path (Join-Path $repository '.env'))) {
        throw 'Run scripts/Initialize-DevEnvironment.ps1 first, or use a PostgreSQL server that runs with -ExistingDatabase.'
    }

    docker compose -f (Join-Path $repository 'compose.dev.yaml') up -d --wait
    if ($LASTEXITCODE -ne 0) {
        throw 'PostgreSQL did not start: is Docker running on this machine? Or use a server that runs with -ExistingDatabase.'
    }
}

$shell = if (Get-Command pwsh -ErrorAction SilentlyContinue) { 'pwsh' } else { 'powershell' }
foreach ($project in 'src/AutoElite.API', 'src/AutoElite.Web') {
    Start-Process $shell -WorkingDirectory $repository -ArgumentList '-NoExit', '-Command', "dotnet run --project $project"
}

Write-Host 'The API starts at http://localhost:5080/scalar and the website at http://localhost:5173.'
Write-Host 'The first start creates the database and the demo data; the users are in the README (Demo data).'
