using ClassLibrary.Emuns;
using System.ComponentModel.DataAnnotations;

namespace NimbusWatchApi.Models
{
  public class ServiceClientModel
  {
    [Key]
    [Required]
    public int Id { get; set; }

    [Required]
    public Guid ClientId { get; set; }

    [Required]
    public string ClientSecretHash { get; set; } = string.Empty;

    [Required]
    public int RoleId { get; set; } = (int)RoleEnums.Service;

    [Required]
    public string ServerName { get; set; }

    [Required]
    public string IpAddress { get; set; }

    [Required]
    public int ServerTypeId { get; set; }

    [Required]
    public bool IsActive { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }
  }
}
