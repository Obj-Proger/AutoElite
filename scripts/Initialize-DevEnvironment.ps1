<#
.SYNOPSIS
    Prepares this machine to run AutoElite in development: PostgreSQL and the settings of the API.

.DESCRIPTION
    By default writes the password of the development database to .env, which git ignores, and starts PostgreSQL from
    compose.dev.yaml in the local Docker; with -ExistingDatabase it uses a PostgreSQL server that already runs instead.
    Then it keeps the settings and secrets of the API in its user secrets: the connection string, the keys of the tokens
    and of the chats, the folders of the files and of the emails, the address of the website and the demo data with the
    password of its users, which it asks for. A secret that is already set is kept, so that running the script again
    neither signs anybody out nor makes the chats unreadable. No secret is written to a file of the repository.

.PARAMETER ExistingDatabase
    Uses a PostgreSQL server that already runs, such as one on another machine or in a remote Docker, instead of
    starting one in the local Docker. The script asks for its connection string without showing it, so that its
    password stays out of the history of the console. The API creates the database when it first starts, if the user
    may create databases.

.EXAMPLE
    .\scripts\Initialize-DevEnvironment.ps1

.EXAMPLE
    .\scripts\Initialize-DevEnvironment.ps1 -ExistingDatabase
#>
[CmdletBinding()]
param(
    [switch] $ExistingDatabase
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$api = Join-Path $repository 'src/AutoElite.API'
$environmentFile = Join-Path $repository '.env'

function New-RandomBytes([int] $count) {
    $buffer = New-Object byte[] $count
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $generator.GetBytes($buffer) } finally { $generator.Dispose() }
    return , $buffer
}

function New-RandomKey([int] $bytes = 32) {
    return [Convert]::ToBase64String((New-RandomBytes $bytes))
}

function New-RandomPassword {
    return -join ((New-RandomBytes 24) | ForEach-Object { $_.ToString('x2') })
}

function Read-Secret([string] $prompt) {
    $secure = Read-Host $prompt -AsSecureString
    return (New-Object System.Net.NetworkCredential('', $secure)).Password
}

function Get-Secrets {
    $secrets = @{}
    foreach ($line in (dotnet user-secrets list --project $api)) {
        if ($line -match '^(?<key>[^=]+?) = (?<value>.*)$') {
            $secrets[$Matches.key] = $Matches.value
        }
    }
    return $secrets
}

function Set-Secret([string] $key, [string] $value) {
    dotnet user-secrets set $key $value --project $api | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not set the user secret $key." }
}

function Set-SecretIfMissing([hashtable] $secrets, [string] $key, [string] $value) {
    if ($secrets.ContainsKey($key)) {
        Write-Host "  $key is kept"
    }
    else {
        Set-Secret $key $value
        Write-Host "  $key is set"
    }
}

# Starts PostgreSQL in the local Docker, with the password of the development database in .env, and gives its
# connection string.
function Start-DockerDatabase {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw 'docker was not found. Install Docker, or use a PostgreSQL server that runs with -ExistingDatabase.'
    }

    if (Test-Path $environmentFile) {
        $line = Get-Content $environmentFile | Where-Object { $_ -match '^AUTOELITE_DB_PASSWORD=' } | Select-Object -First 1
        if (-not $line) { throw '.env has no AUTOELITE_DB_PASSWORD; add it or delete .env to make a new one.' }
        $password = $line.Substring('AUTOELITE_DB_PASSWORD='.Length)
    }
    else {
        $password = New-RandomPassword
        Set-Content -Path $environmentFile -Value "AUTOELITE_DB_PASSWORD=$password" -Encoding ascii
        Write-Host 'Wrote the password of the development database to .env'
    }

    Write-Host 'Starting PostgreSQL...'
    docker compose -f (Join-Path $repository 'compose.dev.yaml') up -d --wait
    if ($LASTEXITCODE -ne 0) {
        throw 'PostgreSQL did not start: is Docker running on this machine? Or use a server that runs with -ExistingDatabase.'
    }

    return "Host=localhost;Port=5433;Database=autoelite;Username=autoelite;Password=$password"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet was not found. Install the .NET 10 SDK first (see the README).'
}

if ($ExistingDatabase) {
    $connectionString = Read-Secret 'Connection string of the database (Host=...;Port=...;Database=autoelite;Username=...;Password=...)'
    if ($connectionString -notmatch 'Host=' -or $connectionString -notmatch 'Database=') {
        throw 'The connection string needs at least Host= and Database=.'
    }
}
else {
    $connectionString = Start-DockerDatabase
}

$data = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'AutoElite'
$files = Join-Path $data 'files'
$mail = Join-Path $data 'mail'
New-Item -ItemType Directory -Force -Path $files, $mail | Out-Null

Write-Host 'Settings of the API (user secrets):'
$secrets = Get-Secrets
Set-Secret 'ConnectionStrings:AutoElite' $connectionString
Write-Host '  ConnectionStrings:AutoElite is set'
Set-SecretIfMissing $secrets 'Jwt:SigningKey' (New-RandomKey 48)
Set-SecretIfMissing $secrets 'ChatEncryption:CurrentKeyId' 'key_1'
Set-SecretIfMissing $secrets 'ChatEncryption:Keys:key_1' (New-RandomKey)
Set-SecretIfMissing $secrets 'FileStorage:RootPath' $files
Set-SecretIfMissing $secrets 'Client:BaseUrl' 'http://localhost:5173'
Set-SecretIfMissing $secrets 'Email:Mode' 'PickupDirectory'
Set-SecretIfMissing $secrets 'Email:PickupDirectory' $mail
Set-SecretIfMissing $secrets 'Email:FromAddress' 'no-reply@autoelite.localhost'
Set-SecretIfMissing $secrets 'DemoData:Enabled' 'true'

if ($secrets.ContainsKey('DemoData:Password')) {
    Write-Host '  DemoData:Password is kept'
}
else {
    while ($true) {
        $password = Read-Secret 'Password of the demo users (8 to 128 characters with a letter and a digit)'
        if ($password.Length -ge 8 -and $password.Length -le 128 -and $password -match '\p{L}' -and $password -match '\d') { break }
        Write-Host 'The password needs 8 to 128 characters with at least one letter and one digit.'
    }
    Set-Secret 'DemoData:Password' $password
    Write-Host '  DemoData:Password is set'
}

Write-Host ''
Write-Host "Ready. Emails are written to $mail"
if ($ExistingDatabase) {
    Write-Host 'Start the API and the website with scripts/Start-DevEnvironment.ps1 -ExistingDatabase'
}
else {
    Write-Host 'Start the API and the website with scripts/Start-DevEnvironment.ps1'
}
