using MySqlConnector;
using NimbusWatchApi.Models;

namespace NimbusWatchApi.Database.TableManagers
{
  public interface IServiceClientTableManager
  {
    Task<bool> AddServiceClient(ServiceClientModel client);
    Task<bool> UpdateServiceClient(ServiceClientModel client);
    Task<bool> DeleteServiceClient(int id);
    Task<List<ServiceClientModel>> GetAllServiceClients();
    Task<ServiceClientModel?> GetServiceClientByClientId(string clientId);
  }

  public class ServiceClientTableManager : TableManager, IServiceClientTableManager
  {
    public ServiceClientTableManager(string connectionString) : base(connectionString)
    {
    }

    public async Task<bool> AddServiceClient(ServiceClientModel client)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        INSERT INTO serviceclients (ClientId, ClientSecretHash, RoleId, IsActive, CreatedAt, UpdatedAt)
        VALUES (@ClientId, @ClientSecretHash, @RoleId, @IsActive, @CreatedAt, @UpdatedAt);
        """;

      cmd.Parameters.AddWithValue("@ClientId", client.ClientId);
      cmd.Parameters.AddWithValue("@ClientSecretHash", client.ClientSecretHash);
      cmd.Parameters.AddWithValue("@RoleId", client.RoleId);
      cmd.Parameters.AddWithValue("@IsActive", client.IsActive);
      cmd.Parameters.AddWithValue("@CreatedAt", client.CreatedAt);
      cmd.Parameters.AddWithValue("@UpdatedAt", client.UpdatedAt);

      return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> UpdateServiceClient(ServiceClientModel client)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        UPDATE serviceclients
        SET ClientSecretHash = @ClientSecretHash,
            RoleId = @RoleId,
            IsActive = @IsActive,
            UpdatedAt = @UpdatedAt
        WHERE Id = @Id;
        """;

      cmd.Parameters.AddWithValue("@Id", client.Id);
      cmd.Parameters.AddWithValue("@ClientSecretHash", client.ClientSecretHash);
      cmd.Parameters.AddWithValue("@RoleId", client.RoleId);
      cmd.Parameters.AddWithValue("@IsActive", client.IsActive);
      cmd.Parameters.AddWithValue("@UpdatedAt", client.UpdatedAt);

      return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteServiceClient(int id)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        DELETE FROM serviceclients
        WHERE Id = @Id;
        """;

      cmd.Parameters.AddWithValue("@Id", id);

      return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<ServiceClientModel>> GetAllServiceClients()
    {
      var clients = new List<ServiceClientModel>();

      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        SELECT Id, ClientId, ClientSecretHash, RoleId, IsActive, CreatedAt, UpdatedAt
        FROM serviceclients;
        """;

      await using var reader = await cmd.ExecuteReaderAsync();

      while (await reader.ReadAsync())
      {
        clients.Add(MapClient(reader));
      }

      return clients;
    }

    public async Task<ServiceClientModel?> GetServiceClientByClientId(string clientId)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        SELECT Id, ClientId, ClientSecretHash, RoleId, IsActive, CreatedAt, UpdatedAt
        FROM serviceclients
        WHERE ClientId = @ClientId
        LIMIT 1;
        """;

      cmd.Parameters.AddWithValue("@ClientId", clientId);

      await using var reader = await cmd.ExecuteReaderAsync();

      if (await reader.ReadAsync())
      {
        return MapClient(reader);
      }

      return null;
    }

    private static ServiceClientModel MapClient(MySqlDataReader reader)
    {
      return new ServiceClientModel
      {
        Id = reader.GetInt32("Id"),
        ClientId = reader.GetString("ClientId"),
        ClientSecretHash = reader.GetString("ClientSecretHash"),
        RoleId = reader.GetInt32("RoleId"),
        IsActive = reader.GetBoolean("IsActive"),
        CreatedAt = reader.GetDateTime("CreatedAt"),
        UpdatedAt = reader.GetDateTime("UpdatedAt")
      };
    }
  }
}
