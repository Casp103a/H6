using System.Text.Json;

namespace ServerService.Logic
{
  public interface IServiceInfoFetcher
  {
    int ServiceId { get; }
  }

  internal class ServiceInfoFetcher : IServiceInfoFetcher
  {
    private readonly HttpClient httpClient;
    private readonly IConfigReader configReader;

    public ServiceInfoFetcher(IHttpClientFactory factory, IConfigReader configReader)
    {
      httpClient = factory.CreateClient("ApiClient");
      this.configReader = configReader;
    }
    private int serviceId;

    public int ServiceId
    {
      get
      {
        if (serviceId == 0)
        {
          var response = httpClient.GetAsync($"api/ServerService/GetServiceInfo/{configReader.Configuration.CompanyId}?clientId={configReader.Configuration.ClientId}").GetAwaiter().GetResult();

          if (response.IsSuccessStatusCode)
          {
            var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var json = JsonSerializer.Deserialize(content, AppJsonSerializerContext.Default.ServiceInfoDto);
            if (json != null)
              serviceId = json.Id;
          }
        }

        return serviceId;
      }
    }
  }
}
