using System.ComponentModel.DataAnnotations;

namespace NimbusWatchApi.Models
{
  public class UserProfileModel
  {
    [Key]
    [Required]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public string FullName { get; set; }

    [Required]
    public string PhoneNumber { get; set; }
  }
}
