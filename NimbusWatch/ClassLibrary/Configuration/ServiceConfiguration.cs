namespace ClassLibrary.Configuration
{
  public class ServiceConfiguration
  {
    public string ServerName { get; set; } = string.Empty;
    public int ReportFrequencyInMilliSec { get; set; }
    public Guid CompanyId { get; set; }

    public string ApiBaseUrl { get; set; } = "https://nimbus-watch.com";
    public string ServiceTokenPath { get; set; } = "/api/ServiceAuth/token";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public int TokenRefreshBufferSeconds { get; set; } = 30;
  }
}
