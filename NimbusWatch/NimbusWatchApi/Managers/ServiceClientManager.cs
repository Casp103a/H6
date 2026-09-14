using NimbusWatchApi.Database;
using NimbusWatchApi.Models;

namespace NimbusWatchApi.Managers
{
  public interface IServiceClientManager
  {
    Task<bool> AddAsync(ServiceClientModel client);
    Task<bool> UpdateAsync(ServiceClientModel client);
    Task<bool> DeleteAsync(int id);
    Task<ServiceClientModel?> GetByClientIdAsync(string clientId);
  }

  public class ServiceClientManager : IServiceClientManager
  {
    private readonly IDatabaseManager databaseManager;

    public ServiceClientManager(IDatabaseManager databaseManager)
    {
      this.databaseManager = databaseManager;
    }

    public async Task<bool> AddAsync(ServiceClientModel client)
    {
      try
      {
        return await databaseManager.ServiceClients.AddServiceClient(client);
      }
      catch (Exception)
      {
        return false;
      }
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

    public async Task<ServiceClientModel?> GetByClientIdAsync(string clientId)
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
  }
}
