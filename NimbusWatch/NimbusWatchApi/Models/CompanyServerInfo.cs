using System.ComponentModel.DataAnnotations;

namespace NimbusWatchApi.Models
{
  public class CompanyServerInfo
  {
    [Key]
    [Required]
    public int Id { get; set; }

    [Required]
    public string ServerName { get; set; }

    [Required]
    public string IPAddress { get; set; }
  }
}
