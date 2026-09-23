using System.ComponentModel.DataAnnotations;

namespace NimbusWatchApi.Models
{
  public class ServerTypesModel
  {
    [Key]
    [Required]
    public int Id { get; set; }

    [Required]
    public string ServerTypeName { get; set; }
  }
}
