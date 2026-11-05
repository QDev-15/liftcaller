<#
  One-time IIS setup for the Lift Call System (Api, Admin, WebApp).

  Run this ONCE, in an elevated PowerShell ("Run as Administrator"). Re-running it later is safe —
  it skips/updates anything that already exists instead of failing.

  Before running this:
    1. Build each of the 3 projects in Release configuration at least once (so the
       bin\Release\net10.0\publish folders below already exist) — e.g. from the repo root:
         dotnet build src\IMIP.TochuForkLift.Api\IMIP.TochuForkLift.Api.csproj -c Release
         dotnet build src\IMIP.TochuForkLift.Admin\IMIP.TochuForkLift.Admin.csproj -c Release
         dotnet build src\IMIP.TochuForkLift.WebApp\IMIP.TochuForkLift.WebApp.csproj -c Release
       (Each .csproj now auto-publishes into bin\Release\net10.0\publish whenever you build in
       Release — see the PublishToIisOnBuild target added to each .csproj.)
    2. Install the "IIS URL Rewrite" module (required for the WebApp site's SPA fallback routing;
       it is not a Windows feature and is not installed by this script):
         https://www.iis.net/downloads/microsoft/url-rewrite
    3. Set the real SQL Server connection string in
       src\IMIP.TochuForkLift.Api\appsettings.json (ConnectionStrings:ImipLiftCall) — it still has
       YOUR_SERVER/YOUR_USER/YOUR_PASSWORD placeholders.

  After this script finishes, every later "Build Solution" done in Release configuration
  re-publishes straight into these same folders, so IIS picks up the change automatically.
  Debug-configuration builds (used for F5/debugging) never touch IIS.
#>

#Requires -RunAsAdministrator
$ErrorActionPreference = 'Stop'

# ---- Adjust these if your paths/ports differ ----
$root = Split-Path -Parent $PSScriptRoot   # repo root (…\tochu_forklift)
$sites = @(
    @{ Name = 'TochuForkLift-Api';    Pool = 'TochuForkLift-Api';    Path = "$root\src\IMIP.TochuForkLift.Api\bin\Release\net10.0\publish";    Protocol = 'https'; Port = 443;  HostHeader = 'localhost' }
    @{ Name = 'TochuForkLift-Admin';  Pool = 'TochuForkLift-Admin';  Path = "$root\src\IMIP.TochuForkLift.Admin\bin\Release\net10.0\publish";  Protocol = 'http';  Port = 8081; HostHeader = '' }
    @{ Name = 'TochuForkLift-WebApp'; Pool = 'TochuForkLift-WebApp'; Path = "$root\src\IMIP.TochuForkLift.WebApp\bin\Release\net10.0\publish"; Protocol = 'http';  Port = 8082; HostHeader = '' }
)
# --------------------------------------------------

Write-Host "Enabling required Windows features (WebSockets for SignalR, Application Initialization for preload)..." -ForegroundColor Cyan
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebSockets -NoRestart | Out-Null
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ApplicationInit -NoRestart | Out-Null

if (-not (Test-Path "$env:windir\System32\inetsrv\rewrite.dll")) {
    Write-Warning "IIS URL Rewrite module is not installed. The WebApp site will return 404s on any deep link until you install it from https://www.iis.net/downloads/microsoft/url-rewrite and re-run this script."
}

Import-Module WebAdministration

foreach ($site in $sites) {
    Write-Host "`n--- $($site.Name) ---" -ForegroundColor Cyan

    if (-not (Test-Path $site.Path)) {
        Write-Warning "$($site.Path) does not exist yet. Build that project in Release configuration first, then re-run this script. Skipping $($site.Name) for now."
        continue
    }

    # App pool: ASP.NET Core doesn't run under the classic CLR IIS hosts, so "No Managed Code".
    if (-not (Test-Path "IIS:\AppPools\$($site.Pool)")) {
        New-WebAppPool -Name $site.Pool | Out-Null
    }
    Set-ItemProperty "IIS:\AppPools\$($site.Pool)" -Name managedRuntimeVersion -Value ''
    Set-ItemProperty "IIS:\AppPools\$($site.Pool)" -Name startMode -Value 'AlwaysRunning'
    Set-ItemProperty "IIS:\AppPools\$($site.Pool)" -Name processModel.idleTimeout -Value ([TimeSpan]::Zero)
    Set-ItemProperty "IIS:\AppPools\$($site.Pool)" -Name recycling.periodicRestart.time -Value ([TimeSpan]::Zero)

    # The publish folder lives under the project tree, not C:\inetpub, so grant the app pool's
    # virtual identity read+execute access explicitly.
    $acct = "IIS AppPool\$($site.Pool)"
    icacls $site.Path /grant "${acct}:(OI)(CI)RX" /T | Out-Null

    # Site + binding
    if (-not (Test-Path "IIS:\Sites\$($site.Name)")) {
        New-WebSite -Name $site.Name -PhysicalPath $site.Path -ApplicationPool $site.Pool `
            -Port $site.Port -HostHeader $site.HostHeader -Ssl:($site.Protocol -eq 'https') | Out-Null
    }
    else {
        Set-ItemProperty "IIS:\Sites\$($site.Name)" -Name physicalPath -Value $site.Path
    }
    Set-ItemProperty "IIS:\Sites\$($site.Name)" -Name applicationDefaults.preloadEnabled -Value $true

    if ($site.Protocol -eq 'https') {
        $cert = Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -eq 'CN=localhost' } | Select-Object -First 1
        if (-not $cert) {
            Write-Host "Creating a self-signed certificate for 'localhost' (internal LAN use only)..." -ForegroundColor Yellow
            $cert = New-SelfSignedCertificate -DnsName 'localhost' -CertStoreLocation Cert:\LocalMachine\My
        }
        netsh http delete sslcert ipport=0.0.0.0:$($site.Port) 2>$null | Out-Null
        $appId = '{' + [guid]::NewGuid().ToString() + '}'
        netsh http add sslcert ipport=0.0.0.0:$($site.Port) certhash=$($cert.Thumbprint) appid=$appId | Out-Null
    }

    # Keep ASPNETCORE_ENVIRONMENT=Production at the IIS level (survives republishing, since it's
    # not stored in the app's own web.config file).
    & "$env:windir\System32\inetsrv\appcmd.exe" set config "$($site.Name)" -section:system.webServer/aspNetCore `
        "/+environmentVariables.[name='ASPNETCORE_ENVIRONMENT',value='Production']" /commit:apphost 2>&1 | Out-Null

    Write-Host "Configured $($site.Name) -> $($site.Path)  ($($site.Protocol)://*:$($site.Port))" -ForegroundColor Green
}

Write-Host "`nDone." -ForegroundColor Cyan
Write-Host "Api:    https://localhost/              (loopback only, used by Admin's ApiBaseUrl)"
Write-Host "Admin:  http://<this-pc-ip>:8081/       (office PCs)"
Write-Host "WebApp: http://<this-pc-ip>:8082/       (kiosk tablets)"
Write-Host "`nIf a site 404s, check %SystemDrive%\inetpub\logs\LogFiles and each publish folder's logs\stdout* for the real error." -ForegroundColor Yellow
