using MySqlConnector;
using NimbusWatchApi.Models;

namespace NimbusWatchApi.Database.TableManagers
{
  public interface IUserProfileTableManager
  {
    Task<UserProfileModel?> AddUserProfile(UserProfileModel profile);
    Task<bool> UpdateUserProfile(UserProfileModel profile);
    Task<UserProfileModel?> GetUserProfileByUserId(int userId);
  }

  public class UserProfileTableManager : TableManager, IUserProfileTableManager
  {
    public UserProfileTableManager(string connectionString) : base(connectionString)
    {
    }

    public async Task<UserProfileModel?> AddUserProfile(UserProfileModel profile)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        INSERT INTO userprofiles (UserId, FullName, PhoneNumber)
        VALUES (@UserId, @FullName, @PhoneNumber);
        """;
      cmd.Parameters.AddWithValue("@UserId", profile.UserId);
      cmd.Parameters.AddWithValue("@FullName", profile.FullName);
      cmd.Parameters.AddWithValue("@PhoneNumber", profile.PhoneNumber);

      var result = await cmd.ExecuteNonQueryAsync();

      if (result > 0)
      {
        profile.Id = (int)cmd.LastInsertedId;
        return profile;
      }
      else
      {
        return null;
      }
    }

    public async Task<bool> UpdateUserProfile(UserProfileModel profile)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = """
        UPDATE userprofiles
        SET FullName = @FullName, PhoneNumber = @PhoneNumber
        WHERE UserId = @UserId;
        """;
      cmd.Parameters.AddWithValue("@UserId", profile.UserId);
      cmd.Parameters.AddWithValue("@FullName", profile.FullName);
      cmd.Parameters.AddWithValue("@PhoneNumber", profile.PhoneNumber);

      var result = await cmd.ExecuteNonQueryAsync();
      return result > 0;
    }

    public async Task<UserProfileModel?> GetUserProfileByUserId(int userId)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = "SELECT Id, UserId, FullName, PhoneNumber FROM userprofiles WHERE UserId = @UserId;";
      cmd.Parameters.AddWithValue("@UserId", userId);

      await using var reader = await cmd.ExecuteReaderAsync();
      if (await reader.ReadAsync())
      {
        return MapUserProfile(reader);
      }
      else
      {
        return null;
      }
    }

    private UserProfileModel MapUserProfile(MySqlDataReader reader)
    {
      return new UserProfileModel
      {
        Id = reader.GetInt32("Id"),
        UserId = reader.GetInt32("UserId"),
        FullName = reader.GetString("FullName"),
        PhoneNumber = reader.GetString("PhoneNumber"),
      };
    }
  }
}
