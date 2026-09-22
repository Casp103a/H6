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
        var category = new PerformanceCounterCategory("Network Interface");
        var instanceNames = category.GetInstanceNames();

        var instanceName = instanceNames
          .FirstOrDefault(n => n.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase)
                            || n.Contains("WiFi", StringComparison.OrdinalIgnoreCase)
                            || n.Contains("MediaTek", StringComparison.OrdinalIgnoreCase));

        if (instanceName is null)
        {
          logger.LogWarning("No network interface found matching criteria");
        }

        networkSentCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", instanceName);
        networkRecvCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", instanceName);

        cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", readOnly: true);
        cpuCounter.NextValue(); // prime; first sample is always 0
      }
      catch (Exception)
      {
        cpuCounter = null; // category missing or access denied
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
      // Implementation to read disk usage
      var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);

      var totalDisk = drives.Sum(d => d.TotalSize) / (1024 * 1024 * 1024);
      var remainingDisk = drives.Sum(d => d.AvailableFreeSpace) / (1024 * 1024 * 1024);

      return (remainingDisk, totalDisk);
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
