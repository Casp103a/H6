using System.ComponentModel.DataAnnotations;

namespace ClassLibrary.Dtos
{
  public class RegisterUserDto
  {
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [MinLength(8)]
    public string Password { get; set; }

    [Required]
    public int RoleId { get; set; }

    [Required]
    public string CompanyId { get; set; }
  }
}
