param(
    [string]$ClientDir = "C:\Games\Dev\WIldstar\WildStar\Patch",
    [string]$ServerHost = "127.0.0.1",
    [ValidateSet("en", "de")]
    [string]$Language = "en"
)

$ErrorActionPreference = "Stop"

if (!(Test-Path $ClientDir)) {
    throw "ClientDir not found: $ClientDir"
}

$exe64 = Join-Path $ClientDir "WildStar64.exe"
$exe32 = Join-Path $ClientDir "WildStar32.exe"
$exe = if (Test-Path $exe64) { $exe64 } elseif (Test-Path $exe32) { $exe32 } else { $null }

if ($null -eq $exe) {
    throw "WildStar executable not found in $ClientDir"
}

$args = "/auth $ServerHost /authNc $ServerHost /lang $Language /patcher $ServerHost /SettingsKey WildStar /realmDataCenterId 9"
Start-Process -FilePath $exe -WorkingDirectory $ClientDir -ArgumentList $args

Write-Host "Launched $exe"
Write-Host "Args: $args"
