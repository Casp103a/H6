using System.ComponentModel.DataAnnotations;

namespace ClassLibrary.Dtos
{
  public class UpdateUserDto
  {
    [Required]
    public int Id { get; set; }
    [Required]
    public string Email { get; set; }
    [Required]
    public int RoleId { get; set; }
    public string? NewPassword { get; set; }
  }
}
