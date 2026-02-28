$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot\.."
$logDir = Join-Path $repoRoot "tmp\runlogs"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

$remoteDbHost = "192.168.1.70"
$remoteDbPort = 33306

$stsDir = Join-Path $repoRoot "Source\NexusForever.StsServer\bin\Debug\net10.0"
$authDir = Join-Path $repoRoot "Source\NexusForever.AuthServer\bin\Debug\net10.0"
$worldDir = Join-Path $repoRoot "Source\NexusForever.WorldServer\bin\Debug\net10.0"

$stsExe = Join-Path $stsDir "NexusForever.StsServer.exe"
$authExe = Join-Path $authDir "NexusForever.AuthServer.exe"
$worldExe = Join-Path $worldDir "NexusForever.WorldServer.exe"

if (!(Test-Path $stsExe)) { throw "Missing $stsExe" }
if (!(Test-Path $authExe)) { throw "Missing $authExe" }
if (!(Test-Path $worldExe)) { throw "Missing $worldExe" }
if (!(Test-Path (Join-Path $stsDir "StsServer.json"))) {
    $stsConfig = @"
{
  "Network": {
    "Host": "0.0.0.0",
    "Port": 6600
  },
  "Database": {
    "Auth": {
      "ConnectionString": "server=$remoteDbHost;port=$remoteDbPort;user=nexusforever;password=nexusforever;database=nexus_forever_auth",
      "Provider": "MySql"
    }
  }
}
"@
    Set-Content -Path (Join-Path $stsDir "StsServer.json") -Value $stsConfig -Encoding ASCII
}
if (!(Test-Path (Join-Path $authDir "AuthServer.json"))) { throw "Missing AuthServer.json in $authDir" }
if (!(Test-Path (Join-Path $worldDir "WorldServer.json"))) { throw "Missing WorldServer.json in $worldDir" }

$stsOut = Join-Path $logDir "stsserver.out.log"
$stsErr = Join-Path $logDir "stsserver.err.log"
$authOut = Join-Path $logDir "authserver.out.log"
$authErr = Join-Path $logDir "authserver.err.log"
$worldOut = Join-Path $logDir "worldserver.out.log"
$worldErr = Join-Path $logDir "worldserver.err.log"

$sts = Start-Process -FilePath $stsExe -WorkingDirectory $stsDir -RedirectStandardOutput $stsOut -RedirectStandardError $stsErr -PassThru
Start-Sleep -Seconds 2
$auth = Start-Process -FilePath $authExe -WorkingDirectory $authDir -RedirectStandardOutput $authOut -RedirectStandardError $authErr -PassThru
Start-Sleep -Seconds 2
$world = Start-Process -FilePath $worldExe -WorkingDirectory $worldDir -RedirectStandardOutput $worldOut -RedirectStandardError $worldErr -PassThru

Write-Host "Started StsServer PID=$($sts.Id)"
Write-Host "Started AuthServer PID=$($auth.Id)"
Write-Host "Started WorldServer PID=$($world.Id)"
Write-Host "Logs:"
Write-Host "  $stsOut"
Write-Host "  $stsErr"
Write-Host "  $authOut"
Write-Host "  $authErr"
Write-Host "  $worldOut"
Write-Host "  $worldErr"
