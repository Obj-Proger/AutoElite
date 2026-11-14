<#
.SYNOPSIS
    Prepares this machine to run AutoElite in development: PostgreSQL in Docker and the settings of the API.

.DESCRIPTION
    Writes the password of the development database to .env, which git ignores, starts PostgreSQL from
    compose.dev.yaml and keeps the settings and secrets of the API in its user secrets: the connection string, the keys
    of the tokens and of the chats, the folders of the files and of the emails, the address of the website and the demo
    data with the password of its users, which it asks for. A secret that is already set is kept, so that running the
    script again neither signs anybody out nor makes the chats unreadable. No secret is written to a file of the
    repository.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$api = Join-Path $repository 'src/AutoElite.API'
$environmentFile = Join-Path $repository '.env'

function New-RandomKey([int] $bytes = 32) {
    $buffer = New-Object byte[] $bytes
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $generator.GetBytes($buffer) } finally { $generator.Dispose() }
    return [Convert]::ToBase64String($buffer)
}

function New-RandomPassword {
    $buffer = New-Object byte[] 24
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $generator.GetBytes($buffer) } finally { $generator.Dispose() }
    return -join ($buffer | ForEach-Object { $_.ToString('x2') })
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

foreach ($tool in 'dotnet', 'docker') {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        throw "$tool was not found. Install it first (see the README)."
    }
}

# The password of the development database, in .env for Docker Compose.
if (Test-Path $environmentFile) {
    $line = Get-Content $environmentFile | Where-Object { $_ -match '^AUTOELITE_DB_PASSWORD=' } | Select-Object -First 1
    if (-not $line) { throw ".env has no AUTOELITE_DB_PASSWORD; add it or delete .env to make a new one." }
    $databasePassword = $line.Substring('AUTOELITE_DB_PASSWORD='.Length)
}
else {
    $databasePassword = New-RandomPassword
    Set-Content -Path $environmentFile -Value "AUTOELITE_DB_PASSWORD=$databasePassword" -Encoding ascii
    Write-Host 'Wrote the password of the development database to .env'
}

Write-Host 'Starting PostgreSQL...'
docker compose -f (Join-Path $repository 'compose.dev.yaml') up -d --wait
if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL did not start; is Docker running?' }

$data = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'AutoElite'
$files = Join-Path $data 'files'
$mail = Join-Path $data 'mail'
New-Item -ItemType Directory -Force -Path $files, $mail | Out-Null

Write-Host 'Settings of the API (user secrets):'
$secrets = Get-Secrets
Set-Secret 'ConnectionStrings:AutoElite' "Host=localhost;Port=5433;Database=autoelite;Username=autoelite;Password=$databasePassword"
Write-Host '  ConnectionStrings:AutoElite is set to the database in Docker'
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
        $secure = Read-Host 'Password of the demo users (8 to 128 characters with a letter and a digit)' -AsSecureString
        $password = (New-Object System.Net.NetworkCredential('', $secure)).Password
        if ($password.Length -ge 8 -and $password.Length -le 128 -and $password -match '\p{L}' -and $password -match '\d') { break }
        Write-Host 'The password needs 8 to 128 characters with at least one letter and one digit.'
    }
    Set-Secret 'DemoData:Password' $password
    Write-Host '  DemoData:Password is set'
}

Write-Host ''
Write-Host "Ready. Emails are written to $mail"
Write-Host 'Start the API and the website with scripts/Start-DevEnvironment.ps1'
