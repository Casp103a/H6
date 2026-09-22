namespace NimbusWatchApi.Models
{
  public class ServiceStatusesModel
  {
    public int Id { get; set; }
    public int ServerId { get; set; }
    public string Name { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
  }
}
