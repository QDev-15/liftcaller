<#
    Opens the 3 ports the Factory Lift Call System uses so devices on the same LAN (tablets,
    phones) can reach IIS on this machine:
      - 5000  Api
      - 6001  WebApp (Caller/Receiver)
      - 7000  Admin

    Safe to re-run any time — existing rules are left alone instead of duplicated.
#>

$ports = @(
    @{ Name = "TochuForkLift Api";    Port = 5000 },
    @{ Name = "TochuForkLift WebApp"; Port = 6001 },
    @{ Name = "TochuForkLift Admin";  Port = 7000 }
)

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "Requesting administrator permission..."
    Start-Process powershell.exe -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    exit
}

foreach ($p in $ports) {
    $existing = Get-NetFirewallRule -DisplayName $p.Name -ErrorAction SilentlyContinue
    if ($existing) {
        Write-Host "Rule '$($p.Name)' already exists — enabling it." -ForegroundColor Yellow
        $existing | Set-NetFirewallRule -Enabled True -Action Allow
    } else {
        New-NetFirewallRule -DisplayName $p.Name -Direction Inbound -Protocol TCP -LocalPort $p.Port -Action Allow -Profile Any | Out-Null
        Write-Host "Created rule '$($p.Name)' — allow inbound TCP $($p.Port)." -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "Done. Ports 5000, 6001, 7000 now accept inbound connections from the LAN." -ForegroundColor Cyan
Write-Host ""
Write-Host "Press any key to close..."
[void][System.Console]::ReadKey($true)
