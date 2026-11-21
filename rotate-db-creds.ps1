[CmdletBinding()]
param(
    [string]$DbName = "sample_app",
    [string]$DbHost = "localhost",
    [int]$DbPort = 5432,
    [string]$ApiAppSettings = (Join-Path $PSScriptRoot "src/Sample.PubSub/Sample.Api/appsettings.json"),
    [string]$WorkerAppSettings = (Join-Path $PSScriptRoot "src/Sample.PubSub/Sample.WorkerService/appsettings.json"),
    [string]$ComposeFile = (Join-Path $PSScriptRoot "docker-compose.yml")
)

$ErrorActionPreference = "Stop"

function New-RandomString {
    param(
        [int]$Length,
        [string]$Charset
    )
    $bytes = New-Object byte[] $Length
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    $chars = for ($i = 0; $i -lt $Length; $i++) {
        $Charset[$bytes[$i] % $Charset.Length]
    }
    -join $chars
}

$dbUser = "app_" + (New-RandomString -Length 8 -Charset "abcdefghijklmnopqrstuvwxyz0123456789")
$dbPass = New-RandomString -Length 24 -Charset "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
$connStr = "Host=$DbHost;Port=$DbPort;Database=$DbName;Username=$dbUser;Password=$dbPass"

function Update-AppSettings {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        Write-Host "[skip] $Path not found" -ForegroundColor Yellow
        return
    }

    $json = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    if (-not $json.PSObject.Properties["ConnectionStrings"]) {
        $json | Add-Member -NotePropertyName "ConnectionStrings" -NotePropertyValue ([PSCustomObject]@{})
    }
    if (-not $json.ConnectionStrings.PSObject.Properties["DefaultConnection"]) {
        $json.ConnectionStrings | Add-Member -NotePropertyName "DefaultConnection" -NotePropertyValue $connStr
    } else {
        $json.ConnectionStrings.DefaultConnection = $connStr
    }

    $json | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $Path -Encoding UTF8
    Write-Host "[ok]   Updated $Path" -ForegroundColor Green
}

function Update-ComposeFile {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        Write-Host "[skip] $Path not found" -ForegroundColor Yellow
        return
    }

    $content = Get-Content -LiteralPath $Path -Raw

    # Supports both KEY: value and - KEY=value YAML styles
    $content = [regex]::Replace($content, '(POSTGRES_USER\s*[:=]\s*)([^\s#]+)', "`${1}$dbUser")
    $content = [regex]::Replace($content, '(POSTGRES_PASSWORD\s*[:=]\s*)([^\s#]+)', "`${1}$dbPass")
    $content = [regex]::Replace($content, '(POSTGRES_DB\s*[:=]\s*)([^\s#]+)', "`${1}$DbName")

    Set-Content -LiteralPath $Path -Value $content -Encoding UTF8 -NoNewline
    Write-Host "[ok]   Updated $Path" -ForegroundColor Green
}

Update-AppSettings -Path $ApiAppSettings
Update-AppSettings -Path $WorkerAppSettings
Update-ComposeFile -Path $ComposeFile

Write-Host ""
Write-Host "Generated credentials:"
Write-Host "  POSTGRES_USER=$dbUser"
Write-Host "  POSTGRES_PASSWORD=$dbPass"
Write-Host "  POSTGRES_DB=$DbName"
