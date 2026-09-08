using MySqlConnector;
using NimbusWatchApi.Database.TableManagers;

namespace NimbusWatchApi.Database
{
  public interface IDatabaseManager
  {
    IRefreshTokenTableManager RefreshTokens { get; }
    IUserTableManager Users { get; }
  }

  public class DatabaseManager : IDatabaseManager
  {
    // Update these values for your environment.
    private const string ConnectionString =
      "Server=127.0.0.1;Port=3306;Database=nimbuswatch;User ID=root;Password=your_password;";

    public DatabaseManager()
    {
      Users = new UserTableManager(ConnectionString);
      RefreshTokens = new RefreshTokenTableManager(ConnectionString);
    }

    public IRefreshTokenTableManager RefreshTokens { get; set; }
    public IUserTableManager Users { get; set; }


    private async Task SetupDatabase()
    {
      if (!await TableExists("users"))
        await CreateUserTable();

      if (!await TableExists("refreshtokens"))
        await CreateRefreshTokenTable();
    }

    private async Task<MySqlConnection> OpenConnection()
    {
      var connection = new MySqlConnection(ConnectionString);
      await connection.OpenAsync();
      return connection;
    }

    private async Task<bool> TableExists(string tableName)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();
      cmd.CommandText = """
        SELECT COUNT(*)
        FROM information_schema.tables
        WHERE table_schema = DATABASE()
          AND table_name = @name;
        """;
      cmd.Parameters.AddWithValue("@name", tableName);

      var result = await cmd.ExecuteScalarAsync();
      return result != null && Convert.ToInt64(result) > 0;
    }

    private async Task ExecuteNonQuery(string sql)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();
      cmd.CommandText = sql;
      await cmd.ExecuteNonQueryAsync();
    }

    private async Task CreateRefreshTokenTable()
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();
      cmd.CommandText = """
        CREATE TABLE refreshtokens (
        ID CHAR(36) PRIMARY KEY,
        RefreshToken VARCHAR(255) NOT NULL,
        RefreshTokenExpiry DATETIME NOT NULL,
        RefreshTokenIsUsed BOOLEAN NOT NULL,
        UserId INT NOT NULL,
        CONSTRAINT FK_RefreshTokens_Users_UserId
          FOREIGN KEY (UserId) REFERENCES users(Id)
          ON DELETE CASCADE
        );
        """;
      await cmd.ExecuteNonQueryAsync();
    }

    private async Task CreateUserTable()
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();
      cmd.CommandText = """
        CREATE TABLE users (
        Id INT AUTO_INCREMENT PRIMARY KEY,
        Email VARCHAR(255) NOT NULL UNIQUE,
        Password VARCHAR(255) NOT NULL,
        RoleId INT NOT NULL,
        IsActive BOOLEAN NOT NULL,
        CreatedAt DATETIME NOT NULL,
        CompanyId INT NOT NULL
        );
        """;
      await cmd.ExecuteNonQueryAsync();
    }
  }
}
