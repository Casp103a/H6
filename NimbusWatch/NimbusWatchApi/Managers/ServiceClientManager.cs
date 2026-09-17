using ClassLibrary.Dtos;
using ClassLibrary.Services;
using NimbusWatchApi.Database;
using NimbusWatchApi.Models;

namespace NimbusWatchApi.Managers
{
  public interface IServiceClientManager
  {
    Task<(Guid ClientId, string ClientSecret)?> AddAsync(CreateServerServiceDto client);
    Task<bool> UpdateAsync(string companyId, ServiceClientModel client);
    Task<bool> DeleteAsync(string companyId, int id);
    Task<ServiceClientModel?> GetByClientIdAsync(string companyId, Guid clientId);
    Task<ServiceClientModel?> GetByClientIdAsync(string companyId, string clientId);
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

      if (!await databaseManager.ServiceClients.AddServiceClient(client.CompanyId, serviceClientModel))
        return null;

      return (clientId, plainSecret);
    }

    public async Task<bool> UpdateAsync(string companyId, ServiceClientModel client)
    {
      try
      {
        return await databaseManager.ServiceClients.UpdateServiceClient(companyId, client);
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<bool> DeleteAsync(string companyId, int id)
    {
      try
      {
        return await databaseManager.ServiceClients.DeleteServiceClient(companyId, id);
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<ServiceClientModel?> GetByClientIdAsync(string companyId, Guid clientId)
    {
      try
      {
        return await databaseManager.ServiceClients.GetServiceClientByClientId(companyId, clientId);
      }
      catch (Exception)
      {
        return null;
      }
    }

    public async Task<ServiceClientModel?> GetByClientIdAsync(string companyId, string clientId)
    {
      try
      {
        if (!Guid.TryParse(clientId, out var parsedClientId))
          return null;

        return await databaseManager.ServiceClients.GetServiceClientByClientId(companyId, parsedClientId);
      }
      catch (Exception)
      {
        return null;
      }
    }
  }
}
