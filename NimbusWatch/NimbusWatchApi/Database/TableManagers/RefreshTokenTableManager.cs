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
    Task<RefreshTokens?> GetRefreshToken(Guid tokenId, int userId);
    Task<bool> RotateRefreshToken(Guid usedTokenId, int userId, string expectedRefreshTokenHash, RefreshTokens newRefreshToken);
    Task<int> RevokeAllForUser(int userId);
  }

  public class RefreshTokenTableManager : TableManager, IRefreshTokenTableManager
  {
    public RefreshTokenTableManager(string connectionString) : base(connectionString)
    {
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

      return await cmd.ExecuteNonQueryAsync() == 1;
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

      return await cmd.ExecuteNonQueryAsync() == 1;
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

      return await cmd.ExecuteNonQueryAsync() == 1;
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

    public async Task<RefreshTokens?> GetRefreshToken(Guid tokenId, int userId)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        SELECT ID, RefreshToken, RefreshTokenExpiry, RefreshTokenIsUsed, UserId
        FROM refreshtokens
        WHERE ID = @ID AND UserId = @UserId
        LIMIT 1;
        """;

      cmd.Parameters.AddWithValue("@ID", tokenId.ToString());
      cmd.Parameters.AddWithValue("@UserId", userId);

      await using var reader = await cmd.ExecuteReaderAsync();

      if (await reader.ReadAsync())
      {
        return MapRefreshToken(reader);
      }

      return null;
    }

    public async Task<bool> RotateRefreshToken(Guid usedTokenId, int userId, string expectedRefreshTokenHash, RefreshTokens newRefreshToken)
    {
      await using var connection = await OpenConnection();
      await using var transaction = await connection.BeginTransactionAsync();

      try
      {
        await using var updateCmd = connection.CreateCommand();
        updateCmd.Transaction = transaction;
        updateCmd.CommandText = """
          UPDATE refreshtokens
          SET RefreshTokenIsUsed = TRUE
          WHERE ID = @UsedTokenId
            AND UserId = @UserId
            AND RefreshToken = @ExpectedRefreshTokenHash
            AND RefreshTokenIsUsed = FALSE
            AND RefreshTokenExpiry > @UtcNow;
          """;

        updateCmd.Parameters.AddWithValue("@UsedTokenId", usedTokenId.ToString());
        updateCmd.Parameters.AddWithValue("@UserId", userId);
        updateCmd.Parameters.AddWithValue("@ExpectedRefreshTokenHash", expectedRefreshTokenHash);
        updateCmd.Parameters.AddWithValue("@UtcNow", DateTime.UtcNow);

        var updatedRows = await updateCmd.ExecuteNonQueryAsync();
        if (updatedRows != 1)
        {
          await transaction.RollbackAsync();
          return false;
        }

        await using var insertCmd = connection.CreateCommand();
        insertCmd.Transaction = transaction;
        insertCmd.CommandText = """
          INSERT INTO refreshtokens (ID, RefreshToken, RefreshTokenExpiry, RefreshTokenIsUsed, UserId)
          VALUES (@ID, @RefreshToken, @RefreshTokenExpiry, @RefreshTokenIsUsed, @UserId);
          """;

        insertCmd.Parameters.AddWithValue("@ID", newRefreshToken.ID.ToString());
        insertCmd.Parameters.AddWithValue("@RefreshToken", newRefreshToken.RefreshToken);
        insertCmd.Parameters.AddWithValue("@RefreshTokenExpiry", newRefreshToken.RefreshTokenExpiry);
        insertCmd.Parameters.AddWithValue("@RefreshTokenIsUsed", newRefreshToken.RefreshTokenIsUsed);
        insertCmd.Parameters.AddWithValue("@UserId", newRefreshToken.UserId);

        var insertedRows = await insertCmd.ExecuteNonQueryAsync();
        if (insertedRows != 1)
        {
          await transaction.RollbackAsync();
          return false;
        }

        await transaction.CommitAsync();
        return true;
      }
      catch
      {
        await transaction.RollbackAsync();
        return false;
      }
    }

    public async Task<int> RevokeAllForUser(int userId)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        UPDATE refreshtokens
        SET RefreshTokenIsUsed = TRUE
        WHERE UserId = @UserId
          AND RefreshTokenIsUsed = FALSE;
        """;

      cmd.Parameters.AddWithValue("@UserId", userId);

      // Returns the number of sessions that were revoked (0 is a valid result).
      return await cmd.ExecuteNonQueryAsync();
    }

    private static RefreshTokens MapRefreshToken(MySqlDataReader reader)
    {
      return new RefreshTokens
      {
        ID = reader.GetGuid("ID"),
        RefreshToken = reader.GetString("RefreshToken"),
        RefreshTokenExpiry = reader.GetDateTime("RefreshTokenExpiry"),
        RefreshTokenIsUsed = reader.GetBoolean("RefreshTokenIsUsed"),
        UserId = reader.GetInt32("UserId")
      };
    }
  }
}
