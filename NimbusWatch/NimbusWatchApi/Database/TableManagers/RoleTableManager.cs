using MySqlConnector;
using NimbusWatchApi.Models;

namespace NimbusWatchApi.Database.TableManagers
{
  public interface IRoleTableManager
  {
    Task<bool> CreateRole(string roleName);
    Task<bool> DeleteRole(int roleId);
    Task<bool> DoesRoleExist(string roleName);
    Task<List<RoleModel>> GetAllRoles();

  }

  public class RoleTableManager : TableManager, IRoleTableManager
  {
    public RoleTableManager(string connectionString) : base(connectionString)
    {
    }

    public async Task<bool> CreateRole(string roleName)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        INSERT INTO roles (RoleName)
        VALUES (@RoleName);
        """;
      cmd.Parameters.AddWithValue("@RoleName", roleName);
      return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteRole(int roleId)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        DELETE FROM roles
        WHERE RoleName = @RoleName;
        """;

      cmd.Parameters.AddWithValue("@RoleId", roleId);
      return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DoesRoleExist(string roleName)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        SELECT COUNT(*)
        FROM roles
        WHERE RoleName = @RoleName;
        """;

      cmd.Parameters.AddWithValue("@RoleName", roleName);
      return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
    }

    public async Task<List<RoleModel>> GetAllRoles()
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        SELECT *
        FROM roles;
        """;

      var roles = new List<RoleModel>();

      await using var reader = await cmd.ExecuteReaderAsync();

      while (await reader.ReadAsync())
      {
        roles.Add(MapModel(reader));
      }

      return roles;
    }

    private RoleModel MapModel(MySqlDataReader reader)
    {
      return new RoleModel
      {
        Id = reader.GetInt32("Id"),
        RoleName = reader.GetString("RoleName"),
      };
    }
  }
}
