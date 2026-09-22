namespace NimbusWatchApi.Database
{
  public static class CompanyTableNames
  {
    // sc_<32 hex> = 35 chars, hd_<32 hex> = 35 chars
    public static string ServiceClients(string companyId) => $"sc_{Normalize(companyId)}";

    public static string HardwareData(string companyId) => $"hd_{Normalize(companyId)}";

    public static string ServiceStatuses(string companyId) => $"ss_{Normalize(companyId)}";

    private static string Normalize(string companyId)
    {
      if (!Guid.TryParse(companyId, out var id))
        throw new ArgumentException($"Invalid companyId: '{companyId}'", nameof(companyId));

      return id.ToString("N");
    }
  }
}
