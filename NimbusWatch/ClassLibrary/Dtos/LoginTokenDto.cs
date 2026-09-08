using System.ComponentModel.DataAnnotations;

namespace ClassLibrary.Dtos
{
  public class LoginTokenDto
  {
    [Required]
    public string Token { get; set; }

    public string? RefreshToken { get; set; }
  }
}
