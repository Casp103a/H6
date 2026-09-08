using System.ComponentModel.DataAnnotations;

namespace NimbusWatchApi.Models
{
  public class RefreshTokens
  {
    [Key]
    [Required]
    public Guid ID { get; set; }

    [Required]
    public string RefreshToken { get; set; }

    [Required]
    public DateTime RefreshTokenExpiry { get; set; }

    [Required]
    public bool RefreshTokenIsUsed { get; set; }

    [Required]
    public int UserId { get; set; }
  }
}
