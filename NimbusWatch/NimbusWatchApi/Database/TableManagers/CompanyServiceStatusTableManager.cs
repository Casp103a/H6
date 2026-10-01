using MySqlConnector;
using NimbusWatchApi.Models;

namespace NimbusWatchApi.Database.TableManagers
{
  public interface ICompanyServiceStatusTableManager
  {
    Task<bool> AddStatus(string companyId, ServiceStatusesModel status);
    Task<List<ServiceStatusesModel>> GetStatuses(string companyId);
    Task<List<ServiceStatusesModel>> GetStatusesByServerId(string companyId, int serverId);
  }

  public class CompanyServiceStatusTableManager : TableManager, ICompanyServiceStatusTableManager
  {
    public CompanyServiceStatusTableManager(string connectionString) : base(connectionString)
    {

    }

    public async Task<bool> AddStatus(string companyId, ServiceStatusesModel status)
    {
      var tablename = CompanyTableNames.ServiceStatuses(companyId);

      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = $"""
        INSERT INTO `{tablename}` (`ServerId`, `Name`, `Status`, `CreatedAt`)
        VALUES (@ServerId, @Name, @Status, @CreatedAt);
        """;

      cmd.Parameters.AddWithValue("@ServerId", status.ServerId);
      cmd.Parameters.AddWithValue("@Name", status.Name);
      cmd.Parameters.AddWithValue("@Status", status.Status);
      cmd.Parameters.AddWithValue("@CreatedAt", status.CreatedAt);

      return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<ServiceStatusesModel>> GetStatuses(string companyId)
    {
      var tablename = CompanyTableNames.ServiceStatuses(companyId);

      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = $"""
            SELECT Id, ServerId, Name, Status, CreatedAt FROM `{tablename}`;
            """;

      var statuses = new List<ServiceStatusesModel>();
      await using var reader = await cmd.ExecuteReaderAsync();

      while (await reader.ReadAsync())
      {
        statuses.Add(MapModel(reader));
      }

      return statuses;
    }

    public async Task<List<ServiceStatusesModel>> GetStatusesByServerId(string companyId, int serverId)
    {
      var tablename = CompanyTableNames.ServiceStatuses(companyId);

      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = $"""
                SELECT Id, ServerId, Name, Status, CreatedAt FROM `{tablename}`
                WHERE ServerId = @ServerId;
                """;

      cmd.Parameters.AddWithValue("@ServerId", serverId);

      var statuses = new List<ServiceStatusesModel>();
      await using var reader = await cmd.ExecuteReaderAsync();

      while (await reader.ReadAsync())
      {
        statuses.Add(MapModel(reader));
      }

      return statuses;
    }

    private ServiceStatusesModel MapModel(MySqlDataReader reader)
    {
      return new ServiceStatusesModel
      {
        Id = reader.GetInt32("Id"),
        ServerId = reader.GetInt32("ServerId"),
        Name = reader.GetString("Name"),
        Status = reader.GetString("Status"),
        CreatedAt = reader.GetDateTime("CreatedAt")
      };
    }
  }
}
