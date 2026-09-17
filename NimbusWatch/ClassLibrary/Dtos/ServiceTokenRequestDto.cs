using System.ComponentModel.DataAnnotations;

namespace ClassLibrary.Dtos
{
  public class ServiceTokenRequestDto
  {
    [Required]
    public string GrantType { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ClientSecret { get; set; } = string.Empty;
  }
}
