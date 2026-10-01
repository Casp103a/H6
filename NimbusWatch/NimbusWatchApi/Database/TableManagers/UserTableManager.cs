using MySqlConnector;
using NimbusWatchApi.Models;

namespace NimbusWatchApi.Database.TableManagers
{
  public interface IUserTableManager
  {
    Task<bool> AddUser(UserModel user);
    Task<bool> UpdateUser(UserModel user);
    Task<bool> DeleteUser(int id);
    Task<List<UserModel>> GetAllUsers(string companyId);
    Task<UserModel?> GetUser(int id);
    Task<UserModel?> GetUser(string email);
    Task<bool> CheckIfCompanyHasAUser(string companyId);
  }

  public class UserTableManager : TableManager, IUserTableManager
  {
    public UserTableManager(string connectionString) : base(connectionString)
    {
    }

    public async Task<bool> AddUser(UserModel user)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        INSERT INTO users (Email, Password, RoleId, IsActive, CreatedAt, CompanyId, PublicId)
        VALUES (@Email, @Password, @RoleId, @IsActive, @CreatedAt, @CompanyId, @PublicId);
        """;

      cmd.Parameters.AddWithValue("@Email", user.Email);
      cmd.Parameters.AddWithValue("@Password", user.Password);
      cmd.Parameters.AddWithValue("@RoleId", user.RoleId);
      cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
      cmd.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);
      cmd.Parameters.AddWithValue("@CompanyId", user.CompanyId);
      cmd.Parameters.AddWithValue("@PublicId", user.PublicId);

      return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> UpdateUser(UserModel user)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        UPDATE users
        SET Email = @Email,
            Password = @Password,
            RoleId = @RoleId,
            IsActive = @IsActive,
            CreatedAt = @CreatedAt,
            CompanyId = @CompanyId
        WHERE Id = @Id;
        """;

      cmd.Parameters.AddWithValue("@Id", user.Id);
      cmd.Parameters.AddWithValue("@Email", user.Email);
      cmd.Parameters.AddWithValue("@Password", user.Password);
      cmd.Parameters.AddWithValue("@RoleId", user.RoleId);
      cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
      cmd.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);
      cmd.Parameters.AddWithValue("@CompanyId", user.CompanyId);

      return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteUser(int id)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        DELETE FROM users
        WHERE Id = @Id;
        """;

      cmd.Parameters.AddWithValue("@Id", id);

      return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<UserModel>> GetAllUsers(string companyId)
    {
      var users = new List<UserModel>();

      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        SELECT Id, Email, Password, RoleId, IsActive, CreatedAt, CompanyId, PublicId
        FROM users
        WHERE CompanyId = @CompanyId;
        """;

      cmd.Parameters.AddWithValue("@CompanyId", companyId);

      await using var reader = await cmd.ExecuteReaderAsync();

      while (await reader.ReadAsync())
      {
        users.Add(MapUser(reader));
      }

      return users;
    }

    public async Task<UserModel?> GetUser(int id)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        SELECT Id, Email, Password, RoleId, IsActive, CreatedAt, CompanyId, PublicId
        FROM users
        WHERE Id = @Id;
        """;

      cmd.Parameters.AddWithValue("@Id", id);

      await using var reader = await cmd.ExecuteReaderAsync();

      if (await reader.ReadAsync())
      {
        return MapUser(reader);
      }

      return null;
    }

    public async Task<UserModel?> GetUser(string email)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        SELECT Id, Email, Password, RoleId, IsActive, CreatedAt, CompanyId, PublicId
        FROM users
        WHERE Email = @Email;
        """;

      cmd.Parameters.AddWithValue("@Email", email);

      await using var reader = await cmd.ExecuteReaderAsync();

      if (await reader.ReadAsync())
      {
        return MapUser(reader);
      }

      return null;
    }

    public async Task<bool> CheckIfCompanyHasAUser(string companyId)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        SELECT COUNT(*)
        FROM users
        WHERE CompanyId = @CompanyId;
        """;

      cmd.Parameters.AddWithValue("@CompanyId", companyId);

      var result = await cmd.ExecuteScalarAsync();
      return Convert.ToInt32(result) > 0;
    }

    private static UserModel MapUser(MySqlDataReader reader)
    {
      return new UserModel
      {
        Id = reader.GetInt32("Id"),
        Email = reader.GetString("Email"),
        Password = reader.GetString("Password"),
        RoleId = reader.GetInt32("RoleId"),
        IsActive = reader.GetBoolean("IsActive"),
        CreatedAt = reader.GetDateTime("CreatedAt"),
        CompanyId = reader.GetGuid("CompanyId").ToString(),
        PublicId = reader.GetGuid("PublicId")
      };
    }
  }
}
