using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ServerService.Logic
{
  public interface IHardwareReader
  {
    double ReadCpuTemperature();
    double ReadCpuUsage();
    (double useRam, double totalRam) ReadMemoryUsage();
    (long RemainingDisk, long totalDisk) ReadDiskUsage();
  }

  public partial class HardwareReader : IHardwareReader
  {
    private readonly PerformanceCounter? cpuCounter;

    public HardwareReader()
    {
      try
      {
        cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", readOnly: true);
        cpuCounter.NextValue(); // prime; first sample is always 0
      }
      catch (Exception)
      {
        cpuCounter = null; // category missing or access denied
      }
    }

    public double ReadCpuTemperature()
    {
      var cpuTemperature = new PerformanceCounter("Thermal Zone Information", "Temperature", @"\_TZ.THRM");
      var temperatureValue = cpuTemperature.NextValue();

      if (temperatureValue == 0)
      {
        Thread.Sleep(1000); // Wait a second to get a valid reading
        temperatureValue = cpuTemperature.NextValue();
      }

      return temperatureValue;
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
  }
}
