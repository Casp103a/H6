namespace ClassLibrary.Dtos
{
  public class UserInfoDto
  {
    public int Id { get; set; }
    public string Email { get; set; }
    public int RoleId { get; set; }
    public Guid PublicId { get; set; }
  }
}
