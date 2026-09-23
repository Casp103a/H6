using ClassLibrary.Configuration;

namespace ClassLibrary.Dtos.Configuration
{
  public class ConfigurationDto
  {
    public int ReportFrequency { get; set; }
    public List<ServerServicesInfo>? MonitoredServices { get; set; }

  }
}
