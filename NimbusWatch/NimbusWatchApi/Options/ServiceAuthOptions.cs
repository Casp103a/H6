namespace NimbusWatchApi.Options
{
  public class ServiceAuthOptions
  {
    public int AccessTokenLifetimeMinutes { get; set; } = 5;
    public List<ServiceClientOptions> Clients { get; set; } = [];
  }

  public class ServiceClientOptions
  {
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Role { get; set; } = "Service";
    public List<string> Scopes { get; set; } = [];
  }
}
