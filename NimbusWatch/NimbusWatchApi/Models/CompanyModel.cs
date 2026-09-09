using System.ComponentModel.DataAnnotations;

namespace NimbusWatchApi.Models
{
  public class CompanyModel
  {
    [Key]
    [Required]
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; }
  }
}
