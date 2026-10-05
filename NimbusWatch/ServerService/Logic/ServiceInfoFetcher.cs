using System.Text.Json;

namespace ServerService.Logic
{
  public interface IServiceInfoFetcher
  {
    Task<int> GetServiceIdAsync(CancellationToken cancellationToken = default);
  }

  internal sealed class ServiceInfoFetcher : IServiceInfoFetcher
  {
    private readonly HttpClient httpClient;
    private readonly IConfigReader configReader;
    private readonly SemaphoreSlim fetchLock = new(1, 1);
    private int serviceId;

    public ServiceInfoFetcher(IHttpClientFactory factory, IConfigReader configReader)
    {
      httpClient = factory.CreateClient("ApiClient");
      this.configReader = configReader;
    }

    public async Task<int> GetServiceIdAsync(CancellationToken cancellationToken = default)
    {
      if (serviceId != 0)
        return serviceId;

      await fetchLock.WaitAsync(cancellationToken);
      try
      {
        if (serviceId != 0)
          return serviceId;

        var config = configReader.Configuration;
        using var response = await httpClient.GetAsync(
          $"api/ServerService/GetServiceInfo/{config.CompanyId}?clientId={config.ClientId}",
          cancellationToken);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var dto = JsonSerializer.Deserialize(content, AppJsonSerializerContext.Default.ServiceInfoDto);

        if (dto is null || dto.Id == 0)
          throw new InvalidOperationException("API returned no valid service id.");

        serviceId = dto.Id;
        return serviceId;
      }
      finally
      {
        fetchLock.Release();
      }
    }
  }
}
