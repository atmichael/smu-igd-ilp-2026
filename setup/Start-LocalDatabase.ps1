<#
.SYNOPSIS
Runs MariaDB 11.4 LTS (MySQL-compatible) for local development without Docker or admin rights,
with the same database and user as docker-compose.yml.

.EXAMPLE
.\setup\Start-LocalDatabase.ps1
.\setup\Start-LocalDatabase.ps1 -Stop
#>
param(
    [string]$InstallRoot = (Join-Path $env:LOCALAPPDATA 'ilp-mariadb'),
    [string]$Version = '11.4.13',
    [switch]$Stop
)

$ErrorActionPreference = 'Stop'
$serverHome = Join-Path $InstallRoot "mariadb-$Version-winx64"
$bin = Join-Path $serverHome 'bin'
$data = Join-Path $InstallRoot 'data'

function Test-DatabasePort {
    $client = [Net.Sockets.TcpClient]::new()
    try { $client.Connect('127.0.0.1', 3306); $true } catch { $false } finally { $client.Dispose() }
}

if ($Stop) {
    & (Join-Path $bin 'mariadb-admin.exe') --user=root --host=127.0.0.1 shutdown
    return
}

if (-not (Test-Path (Join-Path $bin 'mariadbd.exe'))) {
    New-Item -ItemType Directory -Force -Path $InstallRoot | Out-Null
    $zip = Join-Path $InstallRoot "mariadb-$Version-winx64.zip"
    if (-not (Test-Path $zip)) {
        Write-Host "Downloading MariaDB $Version (about 95 MB) to $InstallRoot ..."
        # curl resumes interrupted downloads; the .part name keeps a broken download from being extracted.
        & curl.exe --fail --location --retry 10 --retry-all-errors --continue-at - --output "$zip.part" `
            "https://archive.mariadb.org/mariadb-$Version/winx64-packages/mariadb-$Version-winx64.zip"
        if ($LASTEXITCODE -ne 0) { throw 'Download failed; run the script again to resume.' }
        Move-Item "$zip.part" $zip
    }

    Write-Host 'Extracting ...'
    Expand-Archive -Path $zip -DestinationPath $InstallRoot -Force
}

if (-not (Test-Path $data)) {
    Write-Host 'Initializing the data folder ...'
    # root has no password; the server only listens on this computer.
    & (Join-Path $bin 'mariadb-install-db.exe') --datadir="$data"
    if ($LASTEXITCODE -ne 0) { throw "Database initialization failed; see the .err file in $data." }
}

if (-not (Test-DatabasePort)) {
    Write-Host 'Starting MariaDB on 127.0.0.1:3306 ...'
    # 64M matches MySQL 8's default packet limit, above the API's ~30 MB upload limit.
    Start-Process -FilePath (Join-Path $bin 'mariadbd.exe') -WindowStyle Hidden -ArgumentList @(
        "--datadir=`"$data`"", '--port=3306', '--bind-address=127.0.0.1', '--max-allowed-packet=64M')

    $deadline = (Get-Date).AddSeconds(60)
    while (-not (Test-DatabasePort)) {
        if ((Get-Date) -gt $deadline) { throw "MariaDB did not start; see the .err file in $data." }
        Start-Sleep -Milliseconds 500
    }
}

$sql = "CREATE DATABASE IF NOT EXISTS ilpdvapp; " +
    "CREATE USER IF NOT EXISTS 'ilpuser'@'%' IDENTIFIED BY 'Pass123#'; " +
    "GRANT ALL PRIVILEGES ON ilpdvapp.* TO 'ilpuser'@'%';"
& (Join-Path $bin 'mariadb.exe') --user=root --host=127.0.0.1 --execute=$sql
if ($LASTEXITCODE -ne 0) { throw 'Could not create the ilpdvapp database and ilpuser.' }

Write-Host ''
Write-Host 'Database is running. Connection string (matches appsettings.Development.json):'
Write-Host '  Server=127.0.0.1;Port=3306;Database=ilpdvapp;User ID=ilpuser;Password=Pass123#'
Write-Host "Client: & '$(Join-Path $bin 'mariadb.exe')' --host=127.0.0.1 --user=ilpuser --password ilpdvapp"
Write-Host 'Stop it with: .\setup\Start-LocalDatabase.ps1 -Stop'
