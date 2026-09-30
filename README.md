# H6 – NimbusWatch
Svendeprøve

NimbusWatch monitors Windows servers. A small agent on each server reports hardware usage and the status of selected Windows services to a central API, and a web dashboard shows the data per company.

## Architecture

```
Windows server                          Docker host (nimbus-watch.com)
┌──────────────────────┐   HTTPS    ┌──────────────────────────────────────────┐
│ NimbusWatch service  │──REST────▶│ cloudflared ─▶ nginx ─┬─ /api/  ─▶ API   │
│ (ServerService)      │◀─SignalR──│                        ├─ /hubs/ ─▶ API   │
└──────────────────────┘            │                        └─ *.php  ─▶ PHP   │
                                    │                  API ─▶ MariaDB           │
Browser ──── dashboard ───────────▶│                                          │
                                    └──────────────────────────────────────────┘
```

| Project | Description |
|---|---|
| `NimbusWatch/NimbusWatchApi` | ASP.NET Core Web API (.NET 10). JWT auth (cookie for the website, bearer token for services), MariaDB via MySqlConnector, and a SignalR hub at `/hubs/configuration` that pushes configuration changes to connected services. Creates the database schema on startup. |
| `NimbusWatch/ServerService` | Windows Worker Service (published as `NimbusWatch.exe`). Reports CPU, RAM, disk, network and monitored service statuses to the API, and listens on the SignalR hub for config updates. |
| `NimbusWatch/ServiceInstaller` | Self-contained installer that embeds the published service (`Resources/Services.zip`) and installs/upgrades the `NimbusWatch` Windows service. |
| `NimbusWatch/ClassLibrary` | Shared DTOs, enums, `ServiceConfiguration` and services (encryption, client secrets, auth header handler). |
| `NimbusWatchWebsite` | PHP 8.3 + vanilla JS dashboard. All data is fetched client-side from the REST API (`assets/js/api.js`). |

## How it works

1. An **Admin** creates a server in the dashboard (`create-server.php`) and picks the Windows services to monitor.
2. The API registers a service client and returns an encrypted `Service_Config.config` file (company ID, client ID/secret, API URL, report interval, services).
3. The config file and `ServiceInstaller.exe` are copied to the target server and the installer is run.
4. The service exchanges its client credentials for a short-lived token (`POST /api/ServiceAuth/token/{companyId}`) and then:
   - posts hardware data to `/api/CompanyHardwareData/Add/{companyId}` and service statuses to `/api/ServiceStatus/Add/{companyId}` at the configured interval (default 60 s, exponential backoff up to 5 min on failures)
   - stays connected to `/hubs/configuration`, so changes made in `server-config.php` (report interval, monitored services) are applied and saved locally without a restart.

### Roles
| Role | Access |
|---|---|
| `Admin` | Manage users, create servers, edit server configuration |
| `User` | View dashboard and server details |
| `Service` | Used by the Windows service to report data |

---

## Hosting (Docker)

The backend runs with Docker Compose (`NimbusWatch/docker-compose.yml`):

| Service | Image / build | Purpose |
|---|---|---|
| `mariadb` | `mariadb:12.3` | Database (volume `mariadb_data`) |
| `nimbuswatchapi` | `NimbusWatchApi/Dockerfile` | API on port 8080 |
| `app` | `NimbusWatchWebsite/Dockerfile` (`php:8.3-fpm-alpine`) | PHP-FPM for the website |
| `nginx` | `nginx:1.27-alpine` | Serves the site and proxies `/api/` and `/hubs/` to the API (`Docker/nginx/default.conf`) |
| `cloudflare-tunnel` | `cloudflare/cloudflared` | Exposes nginx publicly – no ports are opened on the host |

Create a `.env` file next to `docker-compose.yml`:

```env
MariaDBROOTPassword=...
ConnectionString=Server=mariadb;Database=...;User=...;Password=...
JWT__SigningKey=...
TUNNEL_TOKEN=...
```

Then start it:

```bash
docker compose up -d --build
```

The website uses company-scoped URLs (`/{companyName}/index.php`). nginx handles the rewrite; `.htaccess` does the same for Apache.

---

## Building the installer

1. Publish the service (self-contained single file, win-x64):
   ```bash
   dotnet publish NimbusWatch/ServerService -c Release
   ```
2. Zip the publish output so the archive contains a `Services` folder with `NimbusWatch.exe` in it, and replace `NimbusWatch/ServiceInstaller/Resources/Services.zip`.
3. Publish the installer:
   ```bash
   dotnet publish NimbusWatch/ServiceInstaller -c Release
   ```

## Installing the service (NimbusWatch)

### Requirements
- Windows (x64)
- Administrator rights
- The `Service_Config.config` file downloaded from the dashboard when the server was created

### Installation
1. Put `ServiceInstaller.exe` and `Service_Config.config` in the **same folder**.
2. Right-click `ServiceInstaller.exe` and choose **Run as administrator**.
3. The installer will:
   - Stop and remove any existing `NimbusWatch` service (upgrade)
   - Extract the service to `C:\Program Files\NimbusWatch\Services`
   - Copy the config to `C:\ProgramData\NimbusWatch`
   - Create the service with automatic startup and restart on failure
   - Start the service

### Verification
- The server shows up with live data in the dashboard.
- Logs are written to the **Windows Event Log** (`Application`, source `NimbusWatch`). Repeated connection failures are logged once, and again when the connection is restored.

### Uninstall
There is no uninstaller yet. From an elevated prompt:

```cmd
sc stop NimbusWatch
sc delete NimbusWatch
rmdir /s /q "C:\Program Files\NimbusWatch"
rmdir /s /q "C:\ProgramData\NimbusWatch"
```
