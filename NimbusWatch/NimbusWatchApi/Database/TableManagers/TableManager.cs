using MySqlConnector;

namespace NimbusWatchApi.Database.TableManagers
{
  public abstract class TableManager
  {
    private readonly string _connectionString;

    protected TableManager(string connectionString)
    {
      _connectionString = connectionString;
    }

    internal async Task<MySqlConnection> OpenConnection()
    {
      var connection = new MySqlConnection(_connectionString);
      await connection.OpenAsync();
      return connection;
    }
  }
}
