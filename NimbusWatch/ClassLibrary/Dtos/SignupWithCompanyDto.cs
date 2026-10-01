using System.ComponentModel.DataAnnotations;

namespace ClassLibrary.Dtos
{
  public class SignupWithCompanyDto
  {
    [Required]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }

    [Required]
    public string CompanyId { get; set; }
  }
}
