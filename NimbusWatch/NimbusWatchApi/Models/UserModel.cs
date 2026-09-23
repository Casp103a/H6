using System.ComponentModel.DataAnnotations;

namespace NimbusWatchApi.Models
{
  public class UserModel
  {
    [Key]
    [Required]
    public int Id { get; set; }

    [Required]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }

    [Required]
    public int RoleId { get; set; }

    [Required]
    public bool IsActive { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public string CompanyId { get; set; }

    [Required]
    public Guid PublicId { get; set; }
  }
}
