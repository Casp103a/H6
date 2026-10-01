using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Security.Principal;

namespace ServiceInstaller
{
  internal sealed class Program
  {
    private const string ServiceName = "NimbusWatch";
    private const string InstallFolder = @"C:\Program Files\NimbusWatch";
    private const string ProgramDataFolder = @"C:\ProgramData\NimbusWatch";
    private const string ServiceExeName = "NimbusWatch.exe";
    private const string ConfigFileName = "service_config.config";
    private const string EmbeddedZipResourceName = "ServiceInstaller.Resources.Services.zip";

    static int Main(string[] args)
    {
      if (!IsAdministrator())
      {
        Console.Error.WriteLine("Run this installer as Administrator.");
        return 1;
      }

      var installerFolder = AppContext.BaseDirectory;
      var configSource = Path.Combine(installerFolder, ConfigFileName);

      if (!File.Exists(configSource))
      {
        Console.Error.WriteLine($"Missing config next to installer: {configSource}");
        return 1;
      }

      Directory.CreateDirectory(InstallFolder);
      Directory.CreateDirectory(ProgramDataFolder);

      if (ServiceExists(ServiceName))
      {
        // Only stop the service if it is actually running (or paused)
        var state = GetServiceState(ServiceName);
        if (state is "RUNNING" or "PAUSED")
        {
          RunSc("stop", ServiceName);
          WaitForServiceState(ServiceName, "STOPPED", TimeSpan.FromSeconds(30));
        }

        RunSc("delete", ServiceName);
        Thread.Sleep(2000);
      }

      ExtractEmbeddedZipToFolder(InstallFolder);

      var configTarget = Path.Combine(ProgramDataFolder, ConfigFileName);
      File.Copy(configSource, configTarget, overwrite: true);

      //adds the services folder from zip format
      var zipformat = Path.Combine(InstallFolder, "Services");
      var exePath = Path.Combine(zipformat, ServiceExeName);
      if (!File.Exists(exePath))
      {
        Console.Error.WriteLine($"Service exe not found: {exePath}");
        return 1;
      }

      RunSc("create", $"{ServiceName} binPath= \"{exePath}\" start= auto");
      RunSc("description", $"{ServiceName} \"NimbusWatch background service\"");
      RunSc("failure", $"{ServiceName} reset= 86400 actions= restart/60000/restart/60000/restart/60000");
      RunSc("start", ServiceName);

      Console.WriteLine("NimbusWatch installed successfully.");
      return 0;
    }

    static bool IsAdministrator()
    {
      using var identity = WindowsIdentity.GetCurrent();
      var principal = new WindowsPrincipal(identity);
      return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    static bool ServiceExists(string serviceName)
    {
      var psi = new ProcessStartInfo("sc.exe", $"query \"{serviceName}\"")
      {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      using var process = Process.Start(psi)!;
      process.WaitForExit();
      return process.ExitCode == 0;
    }

    // Returns the service state (e.g. "RUNNING", "STOPPED", "STOP_PENDING") or null if unknown
    static string? GetServiceState(string serviceName)
    {
      var psi = new ProcessStartInfo("sc.exe", $"query \"{serviceName}\"")
      {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      using var process = Process.Start(psi)!;
      var stdout = process.StandardOutput.ReadToEnd();
      process.WaitForExit();

      if (process.ExitCode != 0)
      {
        return null;
      }

      // Example line: "        STATE              : 4  RUNNING"
      foreach (var line in stdout.Split('\n'))
      {
        var trimmed = line.Trim();
        if (!trimmed.StartsWith("STATE", StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }

        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 4 ? parts[3].ToUpperInvariant() : null;
      }

      return null;
    }

    static void WaitForServiceState(string serviceName, string desiredState, TimeSpan timeout)
    {
      var stopwatch = Stopwatch.StartNew();
      while (stopwatch.Elapsed < timeout)
      {
        if (GetServiceState(serviceName) == desiredState)
        {
          return;
        }

        Thread.Sleep(500);
      }

      throw new TimeoutException(
        $"Service '{serviceName}' did not reach state '{desiredState}' within {timeout.TotalSeconds} seconds.");
    }

    static void RunSc(string command, string arguments)
    {
      var psi = new ProcessStartInfo("sc.exe", $"{command} {arguments}")
      {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      using var process = Process.Start(psi)!;
      var stdout = process.StandardOutput.ReadToEnd();
      var stderr = process.StandardError.ReadToEnd();
      process.WaitForExit();

      if (process.ExitCode != 0)
      {
        throw new InvalidOperationException(
          $"sc.exe {command} failed.{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");
      }
    }

    static void ExtractEmbeddedZipToFolder(string targetFolder)
    {
      CleanDirectory(targetFolder);

      using var zipStream = Assembly.GetExecutingAssembly()
        .GetManifestResourceStream(EmbeddedZipResourceName);

      if (zipStream is null)
      {
        throw new InvalidOperationException(
          $"Embedded ZIP resource not found: {EmbeddedZipResourceName}");
      }

      using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

      foreach (var entry in archive.Entries)
      {
        var destinationPath = Path.GetFullPath(Path.Combine(targetFolder, entry.FullName));

        if (!destinationPath.StartsWith(Path.GetFullPath(targetFolder), StringComparison.OrdinalIgnoreCase))
        {
          throw new InvalidOperationException($"Invalid ZIP entry path: {entry.FullName}");
        }

        if (string.IsNullOrEmpty(entry.Name))
        {
          Directory.CreateDirectory(destinationPath);
          continue;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        entry.ExtractToFile(destinationPath, overwrite: true);
      }
    }

    static void CleanDirectory(string targetFolder)
    {
      foreach (var file in Directory.GetFiles(targetFolder, "*", SearchOption.AllDirectories))
      {
        File.SetAttributes(file, FileAttributes.Normal);
      }

      foreach (var dir in Directory.GetDirectories(targetFolder))
      {
        Directory.Delete(dir, recursive: true);
      }

      foreach (var file in Directory.GetFiles(targetFolder))
      {
        File.Delete(file);
      }
    }
  }
}
