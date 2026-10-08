using MySqlConnector;
using NimbusWatchApi.Models;

namespace NimbusWatchApi.Database.TableManagers
{
  public interface IServerTypesTableManager
  {
    Task<bool> CreateServerType(string serverTypeName);
    Task<bool> DeleteServerType(int serverTypeId);
    Task<bool> UpdateServerType(int serverTypeId, string serverTypeName);
    Task<bool> DoesServerTypeExist(string serverTypeName);
    Task<List<ServerTypesModel>> GetAllServerTypes();
    Task<ServerTypesModel?> GetServerTypeById(int serverTypeId);
  }

  public class ServerTypesTableManager : TableManager, IServerTypesTableManager
  {
    public ServerTypesTableManager(string connectionString) : base(connectionString)
    {
    }

    public async Task<bool> CreateServerType(string serverTypeName)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
                INSERT INTO servertypes (ServerTypeName)
                VALUES (@ServerTypeName);
            """;
      cmd.Parameters.AddWithValue("@ServerTypeName", serverTypeName);

      var result = await cmd.ExecuteNonQueryAsync();
      return result > 0;
    }

    public async Task<bool> DeleteServerType(int serverTypeId)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
                DELETE FROM servertypes
                WHERE Id = @ServerTypeId;
            """;
      cmd.Parameters.AddWithValue("@ServerTypeId", serverTypeId);

      var result = await cmd.ExecuteNonQueryAsync();
      return result > 0;
    }

    public async Task<bool> UpdateServerType(int serverTypeId, string serverTypeName)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
                UPDATE servertypes
                SET ServerTypeName = @ServerTypeName
                WHERE Id = @ServerTypeId;
            """;
      cmd.Parameters.AddWithValue("@ServerTypeId", serverTypeId);
      cmd.Parameters.AddWithValue("@ServerTypeName", serverTypeName);

      var result = await cmd.ExecuteNonQueryAsync();
      return result > 0;
    }

    public async Task<bool> DoesServerTypeExist(string serverTypeName)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
                SELECT COUNT(*)
                FROM servertypes
                WHERE ServerTypeName = @ServerTypeName;
            """;
      cmd.Parameters.AddWithValue("@ServerTypeName", serverTypeName);

      var result = (long)await cmd.ExecuteScalarAsync();
      return result > 0;
    }

    public async Task<List<ServerTypesModel>> GetAllServerTypes()
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
                SELECT *
                FROM servertypes;
            """;

      var serverTypes = new List<ServerTypesModel>();
      await using var reader = await cmd.ExecuteReaderAsync();
      while (await reader.ReadAsync())
      {
        serverTypes.Add(MapModel(reader));
      }

      return serverTypes;
    }

    public async Task<ServerTypesModel?> GetServerTypeById(int serverTypeId)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
                SELECT *
                FROM servertypes
                WHERE Id = @ServerTypeId;
            """;
      cmd.Parameters.AddWithValue("@ServerTypeId", serverTypeId);

      await using var reader = await cmd.ExecuteReaderAsync();
      if (await reader.ReadAsync())
      {
        return MapModel(reader);
      }

      return null;
    }

    private ServerTypesModel MapModel(MySqlDataReader reader)
    {
      return new ServerTypesModel
      {
        Id = reader.GetInt32("Id"),
        ServerTypeName = reader.GetString("ServerTypeName"),
      };
    }
  }
}
