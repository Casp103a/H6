using ClassLibrary.Dtos;
using NimbusWatchApi.Database;
using NimbusWatchApi.Models;

namespace NimbusWatchApi.Managers
{
  public interface IUserProfileManager
  {
    Task<UserProfileDto?> CreateUserProfile(int userId, UserProfileRequestsDto request);
    Task<UserProfileDto?> UpdateUserProfile(int userId, UserProfileRequestsDto request);
    Task<UserProfileDto?> GetUserProfile(int userId);
  }

  public class UserProfileManager : IUserProfileManager
  {
    private readonly IDatabaseManager databaseManager;

    public UserProfileManager(IDatabaseManager databaseManager)
    {
      this.databaseManager = databaseManager;
    }

    public async Task<UserProfileDto?> CreateUserProfile(int userId, UserProfileRequestsDto request)
    {
      try
      {
        var existing = await databaseManager.UserProfiles.GetUserProfileByUserId(userId);

        if (existing != null)
          return null;

        var result = await databaseManager.UserProfiles.AddUserProfile(new UserProfileModel
        {
          UserId = userId,
          FullName = request.FullName,
          PhoneNumber = request.PhoneNumber
        });

        if (result == null)
          return null;

        return MapToDto(result);
      }
      catch (Exception)
      {
        return null;
      }
    }

    public async Task<UserProfileDto?> UpdateUserProfile(int userId, UserProfileRequestsDto request)
    {
      try
      {
        var existing = await databaseManager.UserProfiles.GetUserProfileByUserId(userId);

        if (existing == null)
          return null;

        existing.FullName = request.FullName;
        existing.PhoneNumber = request.PhoneNumber;

        var success = await databaseManager.UserProfiles.UpdateUserProfile(existing);

        if (!success)
          return null;

        return MapToDto(existing);
      }
      catch (Exception)
      {
        return null;
      }
    }

    public async Task<UserProfileDto?> GetUserProfile(int userId)
    {
      try
      {
        var result = await databaseManager.UserProfiles.GetUserProfileByUserId(userId);

        if (result == null)
          return null;

        return MapToDto(result);
      }
      catch (Exception)
      {
        return null;
      }
    }

    private static UserProfileDto MapToDto(UserProfileModel model)
    {
      return new UserProfileDto
      {
        Id = model.Id,
        UserId = model.UserId,
        FullName = model.FullName,
        PhoneNumber = model.PhoneNumber
      };
    }
  }
}
