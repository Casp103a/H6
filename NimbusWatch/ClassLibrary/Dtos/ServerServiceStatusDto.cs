namespace ClassLibrary.Dtos
{
  public class ServerServiceStatusDto
  {
    public int Id { get; set; }
    public string Status { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ServerId { get; set; }
  }
}
