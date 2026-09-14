namespace ClassLibrary.Dtos
{
  public class ServiceTokenResponseDto
  {
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
  }
}
