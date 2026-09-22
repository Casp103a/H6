namespace ClassLibrary.Dtos
{
  public class CreateServerServiceDto
  {
    public string Name { get; set; }
    public string IpAddress { get; set; }
    public string CompanyId { get; set; }

    public List<CreateServiceMonitorDto> ServicesToMonitor { get; set; }
  }

  public class CreateServiceMonitorDto
  {
    public string Name { get; set; }
  }
}
