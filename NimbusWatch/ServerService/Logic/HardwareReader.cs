using System.Diagnostics;
using System.Management;

namespace ServerService.Logic
{
  public interface IHardwareReader
  {
    double ReadCpuTemperature();
    double ReadCpuUsage();
    (double useRam, double totalRam) ReadMemoryUsage();
    (long RemainingDisk, long totalDisk) ReadDiskUsage();
  }

  internal class HardwareReader : IHardwareReader
  {
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
      var cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
      var cpuUsageValue = cpuUsage.NextValue();

      if (cpuUsageValue == 0)
      {
        Thread.Sleep(1000); // Wait a second to get a valid reading
        cpuUsageValue = cpuUsage.NextValue();
      }

      return cpuUsageValue;
    }

    public (double useRam, double totalRam) ReadMemoryUsage()
    {
      // Implementation to read memory usage

      using (var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem"))
      {
        foreach (var obj in searcher.Get())
        {
          // Values are in KB
          ulong totalMB = (ulong)obj["TotalVisibleMemorySize"] / 1024;
          ulong freeMB = (ulong)obj["FreePhysicalMemory"] / 1024;
          ulong usedMB = totalMB - freeMB;
          //float percent = (float)usedMB / totalMB * 100;

          return (usedMB, totalMB);
        }
      }
      return (0, 0);
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
