<#
.SYNOPSIS
    Starts AutoElite for development: PostgreSQL in Docker, then the API and the website, each in a window of its own.

.DESCRIPTION
    Run scripts/Initialize-DevEnvironment.ps1 once first. The API answers at http://localhost:5080 (its reference at
    /scalar) and the website at http://localhost:5173. Close a window to stop its part; PostgreSQL keeps running until
    `docker compose -f compose.dev.yaml down`, and keeps its data until the same command with -v.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot

if (-not (Test-Path (Join-Path $repository '.env'))) {
    throw 'Run scripts/Initialize-DevEnvironment.ps1 first.'
}

docker compose -f (Join-Path $repository 'compose.dev.yaml') up -d --wait
if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL did not start; is Docker running?' }

$shell = if (Get-Command pwsh -ErrorAction SilentlyContinue) { 'pwsh' } else { 'powershell' }
foreach ($project in 'src/AutoElite.API', 'src/AutoElite.Web') {
    Start-Process $shell -WorkingDirectory $repository -ArgumentList '-NoExit', '-Command', "dotnet run --project $project"
}

Write-Host 'The API starts at http://localhost:5080/scalar and the website at http://localhost:5173.'
Write-Host 'The first start creates the database and the demo data; the users are in the README (Demo data).'
