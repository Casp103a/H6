using System.ComponentModel.DataAnnotations;

namespace NimbusWatchApi.Models
{
  public class RoleModel
  {
    [Key]
    [Required]
    public int Id { get; set; }

    [Required]
    public string RoleName { get; set; }
  }
}
