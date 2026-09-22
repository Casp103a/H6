namespace ClassLibrary.Dtos
{
  public class ServiceReportDto
  {
    public int ServerId { get; set; }
    public List<ServiceStatusesDto> Statuses { get; set; }
  }
}
