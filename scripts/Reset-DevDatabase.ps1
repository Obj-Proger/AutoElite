<#
.SYNOPSIS
    Deletes the development database and creates it empty, so that the next start of the API creates its schema and the
    demo data again, with the past and the future of the demo moved to the new day.

.DESCRIPTION
    Stop the API, the website and the application first. By default the PostgreSQL of compose.dev.yaml is stopped, its
    data deleted and it is started again. With -ExistingDatabase the database that the connection string in the user
    secrets of the API names is dropped on its server, closing the connections still open to it, and created again,
    owned by the user of the connection string; this needs psql, from the PostgreSQL command line tools or, without them,
    from the postgres image in Docker. The password goes to psql in the PGPASSWORD variable of this process only, never
    on a command line or into a file. The script asks to type the name of the database before it deletes anything.

.PARAMETER ExistingDatabase
    Recreates the database on the PostgreSQL server that the user secrets name instead of the one in the local Docker.

.PARAMETER Force
    Deletes without asking to type the name of the database.

.EXAMPLE
    .\scripts\Reset-DevDatabase.ps1

.EXAMPLE
    .\scripts\Reset-DevDatabase.ps1 -ExistingDatabase
#>
[CmdletBinding()]
param(
    [switch] $ExistingDatabase,
    [switch] $Force
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot

# Asks to type the name of the database; anything else changes nothing.
function Confirm-Deletion([string] $database, [string] $server) {
    if ($Force) { return $true }

    $answer = Read-Host "This deletes the database '$database' on $server with all its data. Type its name to go on"
    if ($answer -cne $database) {
        Write-Host 'Nothing was changed.'
        return $false
    }

    return $true
}

if (-not $ExistingDatabase) {
    $compose = Join-Path $repository 'compose.dev.yaml'
    if (-not (Confirm-Deletion 'autoelite' 'the PostgreSQL in Docker')) { return }

    docker compose -f $compose down -v
    if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL in Docker could not be stopped: is Docker running on this machine?' }

    docker compose -f $compose up -d --wait
    if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL in Docker did not start again.' }

    Write-Host 'The database is empty. Start the API with scripts/Start-DevEnvironment.ps1; it creates the demo data.'
    return
}

$connectionString = $null
foreach ($line in (dotnet user-secrets list --project (Join-Path $repository 'src/AutoElite.API'))) {
    if ($line -match '^ConnectionStrings:AutoElite = (?<value>.*)$') { $connectionString = $Matches.value }
}

if (-not $connectionString) {
    throw 'The user secrets have no ConnectionStrings:AutoElite: run scripts/Initialize-DevEnvironment.ps1 -ExistingDatabase.'
}

# The builder reads the keys and the quoted values of the connection string. PowerShell shows its keys as properties,
# so that assigning the ConnectionString property would add a key of that name; the setter method parses it.
$parts = New-Object System.Data.Common.DbConnectionStringBuilder
$parts.set_ConnectionString($connectionString)
function Get-Part([string[]] $names) {
    foreach ($name in $names) {
        $value = $null
        if ($parts.TryGetValue($name, [ref] $value) -and $value) { return [string] $value }
    }

    return $null
}

$server = Get-Part 'Host', 'Server'
$port = Get-Part 'Port'
if (-not $port) { $port = '5432' }
$database = Get-Part 'Database'
$user = Get-Part 'Username', 'User ID', 'User'
$password = Get-Part 'Password'

# The names go into the SQL as they are, without quotes, which Windows PowerShell would not pass to psql intact; so only
# plain names in lower case are accepted, which PostgreSQL reads the same with or without quotes.
foreach ($name in $database, $user) {
    if ($name -cnotmatch '^[a-z_][a-z0-9_]*$') {
        throw "The connection string names a database or a user that is not a plain name in lower case: '$name'."
    }
}

if (-not $server) { throw 'The connection string names no host.' }
if (-not (Confirm-Deletion $database "${server}:$port")) { return }

$statements = @(
    "DROP DATABASE IF EXISTS $database WITH (FORCE);",
    "CREATE DATABASE $database OWNER $user ENCODING 'UTF8' TEMPLATE template0;"
)
$arguments = @('-h', $server, '-p', $port, '-U', $user, '-d', 'postgres', '-w', '-v', 'ON_ERROR_STOP=1')
foreach ($statement in $statements) { $arguments += @('-c', $statement) }

# psql on this machine, or that of the postgres image in Docker, which reaches this machine itself as host.docker.internal.
$psql = (Get-Command psql -ErrorAction SilentlyContinue).Source
if (-not $psql) {
    $psql = Get-ChildItem "$env:ProgramFiles\PostgreSQL\*\bin\psql.exe" -ErrorAction SilentlyContinue |
        Sort-Object { $version = 0; [void] [int]::TryParse($_.Directory.Parent.Name, [ref] $version); $version } -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}

$env:PGPASSWORD = $password
try {
    if ($psql) {
        & $psql @arguments
    }
    elseif (Get-Command docker -ErrorAction SilentlyContinue) {
        if ($server -in 'localhost', '127.0.0.1', '::1') { $arguments[1] = 'host.docker.internal' }
        docker run --rm -e PGPASSWORD postgres:18 psql @arguments
    }
    else {
        throw 'psql was not found: install the PostgreSQL command line tools, or Docker, or run the SQL in another client.'
    }

    if ($LASTEXITCODE -ne 0) { throw "The database '$database' could not be recreated; psql says why above." }
}
finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
}

Write-Host "The database '$database' is empty. Start the API with scripts/Start-DevEnvironment.ps1 -ExistingDatabase;"
Write-Host 'it creates the schema and the demo data.'
