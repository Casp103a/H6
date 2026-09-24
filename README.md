# H6 – NimbusWatch
Svendeprøve

## Description
NimbusWatch is a monitoring solution for Windows servers. It consists of:

| Project | Description |
|---|---|
| `NimbusWatchApi` | ASP.NET Core Web API (.NET 10) with JWT authentication, a MySQL database and a SignalR hub (`/hubs/configuration`) that pushes configuration changes to connected services. |
| `ServerService` | Windows Worker Service (`NimbusWatch`) that collects hardware data and the status of monitored Windows services, then reports it to the API. |
| `ServiceInstaller` | Self-contained installer that installs, updates and starts the `NimbusWatch` Windows service. |
| `ClassLibrary` | Shared DTOs, configuration models and services, such as encryption. |

---

## Installing the service (NimbusWatch)

### Requirements
- Windows (x64)
- Administrator rights
- A `service_config.config` file for your company/server (encrypted configuration containing, among other things, the API URL and client credentials)

### Installation
1. Put `ServiceInstaller.exe` and `service_config.config` in the **same folder**.
2. Right-click `ServiceInstaller.exe` and choose **Run as administrator**.
3. The installer will:
- Stop and remove any existing `NimbusWatch` service (upgrade)
- Extract the service to `C:\Program Files\NimbusWatch\Services`
- Copy `service_config.config` to `C:\ProgramData\NimbusWatch`
- Create the service with automatic startup and restart on failure
- Start the service

### Verification
Logs are written to the **Windows Event Log** (`Application`, source `NimbusWatch`).

### Uninstall
Run the uninstaller as administrator. It will stop and remove the service and delete the service folder.

