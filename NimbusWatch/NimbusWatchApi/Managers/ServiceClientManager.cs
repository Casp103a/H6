using ClassLibrary.Dtos;
using ClassLibrary.Services;
using NimbusWatchApi.Database;
using NimbusWatchApi.Models;

namespace NimbusWatchApi.Managers
{
  public interface IServiceClientManager
  {
    Task<(Guid ClientId, string ClientSecret)?> AddAsync(CreateServerServiceDto client);
    Task<bool> UpdateAsync(ServiceClientModel client);
    Task<bool> DeleteAsync(int id);
    Task<ServiceClientModel?> GetByClientIdAsync(Guid clientId);
    Task<ServiceClientModel?> GetByClientIdAsync(string clientId);
  }

  public class ServiceClientManager : IServiceClientManager
  {
    private readonly IDatabaseManager databaseManager;
    private readonly IClientSecretService clientSecretService;

    public ServiceClientManager(IDatabaseManager databaseManager, IClientSecretService clientSecretService)
    {
      this.databaseManager = databaseManager;
      this.clientSecretService = clientSecretService;
    }

    public async Task<(Guid ClientId, string ClientSecret)?> AddAsync(CreateServerServiceDto client)
    {
      var plainSecret = clientSecretService.GenerateSecureClientSecret();
      var clientId = Guid.NewGuid();

      var serviceClientModel = new ServiceClientModel
      {
        ClientId = clientId,
        ServerName = client.Name,
        IpAddress = client.IpAddress,
        IsActive = true,
        ClientSecretHash = clientSecretService.HashSecret(plainSecret),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      };

      if (!await databaseManager.ServiceClients.AddServiceClient(serviceClientModel))
        return null;

      return (clientId, plainSecret);
    }

    public async Task<bool> UpdateAsync(ServiceClientModel client)
    {
      try
      {
        return await databaseManager.ServiceClients.UpdateServiceClient(client);
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<bool> DeleteAsync(int id)
    {
      try
      {
        return await databaseManager.ServiceClients.DeleteServiceClient(id);
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<ServiceClientModel?> GetByClientIdAsync(Guid clientId)
    {
      try
      {
        return await databaseManager.ServiceClients.GetServiceClientByClientId(clientId);
      }
      catch (Exception)
      {
        return null;
      }
    }

    public async Task<ServiceClientModel?> GetByClientIdAsync(string clientId)
    {
      try
      {
        if (!Guid.TryParse(clientId, out var parsedClientId))
          return null;

        return await databaseManager.ServiceClients.GetServiceClientByClientId(parsedClientId);
      }
      catch (Exception)
      {
        return null;
      }
    }
  }
}
