using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ServerService.Logic
{
  public interface IHardwareReader
  {
    double ReadCpuUsage();
    (double useRam, double totalRam) ReadMemoryUsage();
    (long RemainingDisk, long totalDisk) ReadDiskUsage();
    (long bytesSend, long bytesReceived) ReadNetworkUsage();
  }

  public partial class HardwareReader : IHardwareReader
  {
    private readonly PerformanceCounter? cpuCounter;
    private readonly PerformanceCounter? networkSentCounter;
    private readonly PerformanceCounter? networkRecvCounter;
    private readonly ILogger<HardwareReader> logger;

    public HardwareReader(ILogger<HardwareReader> logger)
    {
      this.logger = logger;

      try
      {
        cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", readOnly: true);
        cpuCounter.NextValue(); // prime
      }
      catch (Exception ex)
      {
        logger.LogWarning(ex, "CPU performance counter unavailable.");
      }

      try
      {
        var instanceName = new PerformanceCounterCategory("Network Interface")
          .GetInstanceNames()
          .FirstOrDefault(n => n.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase)
                            || n.Contains("WiFi", StringComparison.OrdinalIgnoreCase)
                            || n.Contains("MediaTek", StringComparison.OrdinalIgnoreCase));

        if (instanceName is null)
        {
          logger.LogWarning("No network interface found matching criteria.");
          return;
        }

        networkSentCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", instanceName);
        networkRecvCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", instanceName);
      }
      catch (Exception ex)
      {
        logger.LogWarning(ex, "Network performance counters unavailable.");
      }
    }

    public double ReadCpuUsage()
    {
      if (cpuCounter is null)
      {
        return 0;
      }

      try
      {
        return cpuCounter.NextValue();
      }
      catch (Exception)
      {
        return 0;
      }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
      public uint dwLength;
      public uint dwMemoryLoad;
      public ulong ullTotalPhys;
      public ulong ullAvailPhys;
      public ulong ullTotalPageFile;
      public ulong ullAvailPageFile;
      public ulong ullTotalVirtual;
      public ulong ullAvailVirtual;
      public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    public (double useRam, double totalRam) ReadMemoryUsage()
    {
      var status = new MEMORYSTATUSEX();
      status.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();

      if (!GlobalMemoryStatusEx(ref status))
      {
        return (0, 0);
      }

      double totalMB = status.ullTotalPhys / (1024d * 1024d);
      double freeMB = status.ullAvailPhys / (1024d * 1024d);

      return (totalMB - freeMB, totalMB);
    }

    public (long RemainingDisk, long totalDisk) ReadDiskUsage()
    {
      const long GB = 1024L * 1024 * 1024;
      long total = 0, free = 0;

      foreach (var drive in DriveInfo.GetDrives())
      {
        try
        {
          if (!drive.IsReady || drive.DriveType != DriveType.Fixed)
            continue;

          total += drive.TotalSize;
          free += drive.AvailableFreeSpace;
        }
        catch (Exception ex)
        {
          logger.LogDebug(ex, "Could not read drive {Drive}", drive.Name);
        }
      }

      return (free / GB, total / GB);
    }

    public (long bytesSend, long bytesReceived) ReadNetworkUsage()
    {
      if (networkSentCounter is null || networkRecvCounter is null)
      {
        return (0, 0);
      }

      try
      {
        var sent = networkSentCounter.NextValue();
        var recv = networkRecvCounter.NextValue();

        return ((long)sent, (long)recv);
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error reading network usage");
        return (0, 0);
      }
    }
  }
}
