$ErrorActionPreference = "SilentlyContinue"

$names = @("NexusForever.StsServer", "NexusForever.AuthServer", "NexusForever.WorldServer")
$killed = @()

foreach ($name in $names) {
    $procs = Get-Process -Name $name
    foreach ($p in $procs) {
        Stop-Process -Id $p.Id -Force
        $killed += "${name}:$($p.Id)"
    }
}

if ($killed.Count -eq 0) {
    Write-Host "No local NexusForever server processes were running."
} else {
    Write-Host "Stopped:"
    $killed | ForEach-Object { Write-Host "  $_" }
}
