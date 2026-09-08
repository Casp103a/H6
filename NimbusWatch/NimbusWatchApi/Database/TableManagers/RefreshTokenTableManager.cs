using MySqlConnector;
using NimbusWatchApi.Models;

namespace NimbusWatchApi.Database.TableManagers
{
  public interface IRefreshTokenTableManager
  {
    Task<bool> AddRefreshToken(RefreshTokens refreshToken);
    Task<bool> UpdateRefreshToken(Guid tokenId, bool hasBeenUsed);
    Task<bool> RemoveRefreshToken(Guid tokenId);
    Task<List<RefreshTokens>> GetAllRefreshTokens();
  }

  public class RefreshTokenTableManager : IRefreshTokenTableManager
  {
    private readonly string connectionString;

    public RefreshTokenTableManager(string connectionString)
    {
      this.connectionString = connectionString;
    }

    private async Task<MySqlConnection> OpenConnection()
    {
      var connection = new MySqlConnection(connectionString);
      await connection.OpenAsync();
      return connection;
    }

    public async Task<bool> AddRefreshToken(RefreshTokens refreshToken)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        INSERT INTO refreshtokens (ID, RefreshToken, RefreshTokenExpiry, RefreshTokenIsUsed, UserId)
        VALUES (@ID, @RefreshToken, @RefreshTokenExpiry, @RefreshTokenIsUsed, @UserId);
        """;

      cmd.Parameters.AddWithValue("@ID", refreshToken.ID.ToString());
      cmd.Parameters.AddWithValue("@RefreshToken", refreshToken.RefreshToken);
      cmd.Parameters.AddWithValue("@RefreshTokenExpiry", refreshToken.RefreshTokenExpiry);
      cmd.Parameters.AddWithValue("@RefreshTokenIsUsed", refreshToken.RefreshTokenIsUsed);
      cmd.Parameters.AddWithValue("@UserId", refreshToken.UserId);

      return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> UpdateRefreshToken(Guid tokenId, bool hasBeenUsed)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        UPDATE refreshtokens
        SET RefreshTokenIsUsed = @RefreshTokenIsUsed
        WHERE ID = @ID;
        """;

      cmd.Parameters.AddWithValue("@ID", tokenId.ToString());
      cmd.Parameters.AddWithValue("@RefreshTokenIsUsed", hasBeenUsed);

      return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> RemoveRefreshToken(Guid tokenId)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        DELETE FROM refreshtokens
        WHERE ID = @ID;
        """;

      cmd.Parameters.AddWithValue("@ID", tokenId.ToString());

      return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<RefreshTokens>> GetAllRefreshTokens()
    {
      var tokens = new List<RefreshTokens>();

      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        SELECT ID, RefreshToken, RefreshTokenExpiry, RefreshTokenIsUsed, UserId
        FROM refreshtokens;
        """;

      await using var reader = await cmd.ExecuteReaderAsync();

      while (await reader.ReadAsync())
      {
        tokens.Add(MapRefreshToken(reader));
      }

      return tokens;
    }

    private static RefreshTokens MapRefreshToken(MySqlDataReader reader)
    {
      return new RefreshTokens
      {
        ID = Guid.Parse(reader.GetString("ID")),
        RefreshToken = reader.GetString("RefreshToken"),
        RefreshTokenExpiry = reader.GetDateTime("RefreshTokenExpiry"),
        RefreshTokenIsUsed = reader.GetBoolean("RefreshTokenIsUsed"),
        UserId = reader.GetInt32("UserId")
      };
    }
  }
}
