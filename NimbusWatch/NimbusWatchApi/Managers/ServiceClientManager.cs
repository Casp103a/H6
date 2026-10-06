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
    Task<ServiceInfoDto?> GetByClientIdAsync(string companyId, Guid clientId);
    Task<List<ServerInfoDto>?> GetAllByCompanyIdAsync(string companyId);
    Task<ServerInfoDto?> GetByClientIdAsync(string companyId, string clientId);
    Task<ServiceClientModel?> GetServiceClientByClientId(string companyId, string clientId);
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
        ServerTypeId = client.ServerTypeId,
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

    public async Task<ServiceInfoDto?> GetByClientIdAsync(string companyId, Guid clientId)
    {
      try
      {
        var client = await databaseManager.ServiceClients.GetServiceClientByClientId(companyId, clientId);
        if (client == null)
          return null;

        return new ServiceInfoDto
        {
          Id = client.Id,
        };
      }
      catch (Exception)
      {
        return null;
      }
    }

    public async Task<ServerInfoDto?> GetByClientIdAsync(string companyId, string clientId)
    {
      try
      {
        if (!Guid.TryParse(clientId, out var parsedClientId))
          return null;

        var model = await databaseManager.ServiceClients.GetServiceClientByClientId(companyId, parsedClientId);
        if (model == null)
          return null;

        return new ServerInfoDto
        {
          ClientId = model.ClientId.ToString(),
          IpAddress = model.IpAddress,
          ServerName = model.ServerName,
          ServerTypeId = model.ServerTypeId,
        };
      }
      catch (Exception)
      {
        return null;
      }
    }

    public async Task<List<ServerInfoDto>?> GetAllByCompanyIdAsync(string companyId)
    {
      try
      {
        var clients = await databaseManager.ServiceClients.GetAllServiceClientsByCompanyId(companyId);
        if (clients == null)
          return null;

        return clients.Select(c => new ServerInfoDto
        {
            ClientId = c.ClientId.ToString(),
            IpAddress = c.IpAddress,
            ServerName = c.ServerName,
            ServerTypeId = c.ServerTypeId,
        }).ToList();
      }
      catch (Exception)
      {
        return null;
      }
    }

    public async Task<ServiceClientModel?> GetServiceClientByClientId(string companyId, string clientId)
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
