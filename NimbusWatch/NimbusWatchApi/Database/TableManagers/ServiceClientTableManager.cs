using MySqlConnector;
using NimbusWatchApi.Models;

namespace NimbusWatchApi.Database.TableManagers
{
  public interface IServiceClientTableManager
  {
    Task<bool> AddServiceClient(string companyId, ServiceClientModel client);
    Task<bool> UpdateServiceClient(string companyId, ServiceClientModel client);
    Task<bool> DeleteServiceClient(string companyId, int id);
    Task<List<ServiceClientModel>> GetAllServiceClients(string companyId);
    Task<ServiceClientModel?> GetServiceClientByClientId(string companyId, Guid clientId);
    Task<ServiceClientModel?> GetServiceClientByClientId(string companyId, string clientId);
  }

  public class ServiceClientTableManager : TableManager, IServiceClientTableManager
  {
    public ServiceClientTableManager(string connectionString) : base(connectionString)
    {
    }

    public async Task<bool> AddServiceClient(string companyId, ServiceClientModel client)
    {
      var table = CompanyTableNames.ServiceClients(companyId);

      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = $"""
        INSERT INTO `{table}` (ClientId, ClientSecretHash, ServerName, IpAddress, RoleId, IsActive, CreatedAt, UpdatedAt)
        VALUES (@ClientId, @ClientSecretHash, @ServerName, @IpAddress, @RoleId, @IsActive, @CreatedAt, @UpdatedAt);
        """;

      cmd.Parameters.AddWithValue("@ClientId", client.ClientId.ToString());
      cmd.Parameters.AddWithValue("@ClientSecretHash", client.ClientSecretHash);
      cmd.Parameters.AddWithValue("@ServerName", client.ServerName);
      cmd.Parameters.AddWithValue("@IpAddress", client.IpAddress);
      cmd.Parameters.AddWithValue("@RoleId", client.RoleId);
      cmd.Parameters.AddWithValue("@IsActive", client.IsActive);
      cmd.Parameters.AddWithValue("@CreatedAt", client.CreatedAt);
      cmd.Parameters.AddWithValue("@UpdatedAt", client.UpdatedAt);

      return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> UpdateServiceClient(string companyId, ServiceClientModel client)
    {
      var table = CompanyTableNames.ServiceClients(companyId);

      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = $"""
        UPDATE `{table}`
        SET ClientSecretHash = @ClientSecretHash,
            ServerName = @ServerName,
            IpAddress = @IpAddress,
            RoleId = @RoleId,
            IsActive = @IsActive,
            UpdatedAt = @UpdatedAt
        WHERE Id = @Id;
        """;

      cmd.Parameters.AddWithValue("@Id", client.Id);
      cmd.Parameters.AddWithValue("@ClientSecretHash", client.ClientSecretHash);
      cmd.Parameters.AddWithValue("@ServerName", client.ServerName);
      cmd.Parameters.AddWithValue("@IpAddress", client.IpAddress);
      cmd.Parameters.AddWithValue("@RoleId", client.RoleId);
      cmd.Parameters.AddWithValue("@IsActive", client.IsActive);
      cmd.Parameters.AddWithValue("@UpdatedAt", client.UpdatedAt);

      return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteServiceClient(string companyId, int id)
    {
      var table = CompanyTableNames.ServiceClients(companyId);

      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = $"DELETE FROM `{table}` WHERE Id = @Id;";
      cmd.Parameters.AddWithValue("@Id", id);

      return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<ServiceClientModel>> GetAllServiceClients(string companyId)
    {
      var table = CompanyTableNames.ServiceClients(companyId);
      var clients = new List<ServiceClientModel>();

      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = $"""
        SELECT Id, ClientId, ClientSecretHash, ServerName, IpAddress, RoleId, IsActive, CreatedAt, UpdatedAt
        FROM `{table}`;
        """;

      await using var reader = await cmd.ExecuteReaderAsync();
      while (await reader.ReadAsync())
        clients.Add(MapClient(reader));

      return clients;
    }

    public Task<ServiceClientModel?> GetServiceClientByClientId(string companyId, string clientId)
    {
      if (!Guid.TryParse(clientId, out var parsedClientId))
        return Task.FromResult<ServiceClientModel?>(null);

      return GetServiceClientByClientId(companyId, parsedClientId);
    }

    public async Task<ServiceClientModel?> GetServiceClientByClientId(string companyId, Guid clientId)
    {
      var table = CompanyTableNames.ServiceClients(companyId);

      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = $"""
        SELECT Id, ClientId, ClientSecretHash, ServerName, IpAddress, RoleId, IsActive, CreatedAt, UpdatedAt
        FROM `{table}`
        WHERE ClientId = @ClientId
        LIMIT 1;
        """;

      cmd.Parameters.AddWithValue("@ClientId", clientId.ToString());

      await using var reader = await cmd.ExecuteReaderAsync();
      return await reader.ReadAsync() ? MapClient(reader) : null;
    }

    private static ServiceClientModel MapClient(MySqlDataReader reader)
    {
      return new ServiceClientModel
      {
        Id = reader.GetInt32("Id"),
        ClientId = reader.GetGuid("ClientId"),
        ClientSecretHash = reader.GetString("ClientSecretHash"),
        ServerName = reader.GetString("ServerName"),
        IpAddress = reader.GetString("IpAddress"),
        RoleId = reader.GetInt32("RoleId"),
        IsActive = reader.GetBoolean("IsActive"),
        CreatedAt = reader.GetDateTime("CreatedAt"),
        UpdatedAt = reader.GetDateTime("UpdatedAt")
      };
    }
  }
}
