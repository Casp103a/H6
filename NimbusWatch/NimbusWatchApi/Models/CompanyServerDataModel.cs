using System.ComponentModel.DataAnnotations;

namespace NimbusWatchApi.Models
{
  public class CompanyServerDataModel
  {
    [Key]
    [Required]
    public int Id { get; set; }

    [Required]
    public string CompanyId { get; set; }

    [Required]
    public string ServerName { get; set; }

    [Required]
    public int CPUUsages { get; set; }

    [Required]
    public double DiskTotalSpace { get; set; }

    [Required]
    public double DiskRemainingSpace { get; set; }

    [Required]
    public double RAMUsages { get; set; }

    [Required]
    public double RAMTotal { get; set; }
  }
}
