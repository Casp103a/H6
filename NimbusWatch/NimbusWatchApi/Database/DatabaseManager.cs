using ClassLibrary.Emuns;
using MySqlConnector;
using NimbusWatchApi.Database.TableManagers;

namespace NimbusWatchApi.Database
{
  public interface IDatabaseManager
  {
    IRefreshTokenTableManager RefreshTokens { get; }
    IUserTableManager Users { get; }
    ICompanyTableManager Companys { get; }
    IRoleTableManager Roles { get; }
    IServiceClientTableManager ServiceClients { get; }
    ICompanyHardwareDataTableManager CompanyHardwareData { get; }
  }

  public class DatabaseManager : IDatabaseManager
  {
    private readonly string _connectionString;

    public DatabaseManager(string connectionString)
    {
      _connectionString = connectionString;
      Users = new UserTableManager(_connectionString);
      RefreshTokens = new RefreshTokenTableManager(_connectionString);
      Companys = new CompanyTableManager(_connectionString);
      Roles = new RoleTableManager(_connectionString);
      ServiceClients = new ServiceClientTableManager(_connectionString);
      CompanyHardwareData = new CompanyHardwareDataTableManager(_connectionString);

      SetupDatabase().GetAwaiter().GetResult();
    }

    public IRefreshTokenTableManager RefreshTokens { get; }
    public IUserTableManager Users { get; }
    public ICompanyTableManager Companys { get; }
    public IRoleTableManager Roles { get; }
    public IServiceClientTableManager ServiceClients { get; }
    public ICompanyHardwareDataTableManager CompanyHardwareData { get; }

    private async Task SetupDatabase()
    {
      if (!await TableExists("users"))
        await CreateUserTable();

      if (!await TableExists("refreshtokens"))
        await CreateRefreshTokenTable();

      if (!await TableExists("companys"))
        await CreateCompanyTable();

      if (!await TableExists("roles"))
        await CreateRoleTable();

      if (!await TableExists("serviceclients"))
        await CreateServiceClientTable();

      if (!await TableExists("companyhardwaredata"))
        await CreateCompanyHardwareDataTable();

      await SetupRoles();
    }

    private async Task<MySqlConnection> OpenConnection()
    {
      var connection = new MySqlConnection(_connectionString);
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
        CompanyId VARCHAR(36) NOT NULL
        );
        """;
      await cmd.ExecuteNonQueryAsync();
    }

    private async Task CreateCompanyTable()
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();
      cmd.CommandText = """
        CREATE TABLE companys (
        Id CHAR(36) PRIMARY KEY,
        Name VARCHAR(255) NOT NULL UNIQUE,
        CreatedAt DATETIME NOT NULL
        );
        """;
      await cmd.ExecuteNonQueryAsync();
    }

    private async Task CreateRoleTable()
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();
      cmd.CommandText = """
        CREATE TABLE roles (
        Id INT AUTO_INCREMENT PRIMARY KEY,
        RoleName VARCHAR(255) NOT NULL UNIQUE
        );
        """;
      await cmd.ExecuteNonQueryAsync();
    }

    private async Task CreateServiceClientTable()
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();
      cmd.CommandText = """
        CREATE TABLE serviceclients (
        Id INT AUTO_INCREMENT PRIMARY KEY,
        ClientId CHAR(36) NOT NULL UNIQUE,
        ClientSecretHash VARCHAR(255) NOT NULL,
        ServerName VARCHAR(255) NOT NULL,
        IpAddress VARCHAR(45) NOT NULL,
        RoleId INT NOT NULL,
        IsActive BOOLEAN NOT NULL,
        CreatedAt DATETIME NOT NULL,
        UpdatedAt DATETIME NOT NULL,
        CONSTRAINT FK_ServiceClients_Roles_RoleId
          FOREIGN KEY (RoleId) REFERENCES roles(Id)
          ON DELETE CASCADE
        );
        """;
      await cmd.ExecuteNonQueryAsync();
    }

    private async Task CreateCompanyHardwareDataTable()
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();
      cmd.CommandText = """
        CREATE TABLE companyhardwaredata (
        Id INT AUTO_INCREMENT PRIMARY KEY,
        CompanyId CHAR(36) NOT NULL,
        ServiceId INT NOT NULL,
        CpuUsages INT NOT NULL,
        DiskTotalSpace DOUBLE NOT NULL,
        DiskRemainingSpace DOUBLE NOT NULL,
        RamUsage DOUBLE NOT NULL,
        RamTotal DOUBLE NOT NULL,
        CONSTRAINT FK_CompanyHardwareData_Companys_CompanyId
          FOREIGN KEY (CompanyId) REFERENCES companys(Id)
          ON DELETE CASCADE,
        CONSTRAINT FK_CompanyHardwareData_ServiceClients_ServiceId
          FOREIGN KEY (ServiceId) REFERENCES serviceclients(Id)
          ON DELETE CASCADE
        );
        """;
      await cmd.ExecuteNonQueryAsync();
    }

    private async Task SetupRoles()
    {
      var roles = Enum.GetValues(typeof(RoleEnums)).Cast<RoleEnums>().Select(r => r.ToString()).ToList();
      foreach (var role in roles)
      {
        if (!await Roles.DoesRoleExist(role))
        {
          await Roles.CreateRole(role);
        }
      }
    }
  }
}
