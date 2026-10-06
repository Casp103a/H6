using ClassLibrary.Dtos;
using ClassLibrary.Services;
using NimbusWatchApi.Database;
using NimbusWatchApi.Models;

namespace NimbusWatchApi.Managers
{
  public interface IUserManager
  {
    Task<bool?> CreateAsync(RegisterUserDto user);
    Task<bool?> UpdateAsync(UserModel user, bool passwordChanged = false);
    Task<bool?> DeleteAsync(UserModel user);
    Task<List<UserInfoDto>> GetAllUsers(string companyId);
    Task<UserInfoDto?> GetUser(int id);
    Task<UserInfoDto?> GetUser(string email);
    Task<UserModel?> GetUserModel(string email);
    Task<UserModel?> GetUserModel(int id);
    Task<bool> CheckPasswordSignInAsync(LoginRequestDto login, string password);
    Task<bool> CheckIfCompanyHasAUser(string companyId);
  }

  internal class UserManager : IUserManager
  {
    private readonly IDatabaseManager databaseManager;
    private readonly IEncryptingService encryptionService;

    public UserManager(IDatabaseManager databaseManager, IEncryptingService encryptionService)
    {
      this.databaseManager = databaseManager;
      this.encryptionService = encryptionService;
    }

    public async Task<bool?> CreateAsync(RegisterUserDto userDto)
    {
      try
      {
        if (userDto == null || string.IsNullOrEmpty(userDto.Email) || string.IsNullOrEmpty(userDto.Password))
        {
          return null;
        }

        var doesUserExist = await databaseManager.Users.GetUser(userDto.Email);

        if (doesUserExist != null)
        {
          return false;
        }

        var userModel = new UserModel
        {
          Email = userDto.Email,
          Password = encryptionService.Encrypt(userDto.Password),
          RoleId = userDto.RoleId,
          IsActive = true,
          CreatedAt = DateTime.UtcNow,
          CompanyId = userDto.CompanyId,
          PublicId = Guid.NewGuid()
        };

        await databaseManager.Users.AddUser(userModel);

        return true;
      }
      catch (Exception)
      {
        return null;
      }
    }

    public async Task<bool?> UpdateAsync(UserModel? user, bool passwordChanged = false)
    {
      try
      {
        if (user == null || string.IsNullOrEmpty(user.Email))
        {
          return null;
        }

        if (passwordChanged)
          user.Password = encryptionService.Encrypt(user.Password);

        await databaseManager.Users.UpdateUser(user);
        return true;
      }
      catch (Exception)
      {
        return null;
      }
    }

    public async Task<bool?> DeleteAsync(UserModel user)
    {
      try
      {
        await databaseManager.Users.DeleteUser(user.Id);
        return true;
      }
      catch (Exception)
      {
        return null;
      }
    }

    public async Task<List<UserInfoDto>> GetAllUsers(string companyId)
    {
      try
      {
        var users = await databaseManager.Users.GetAllUsers(companyId);
        return users.Select(u => new UserInfoDto
        {
          Id = u.Id,
          Email = u.Email,
          RoleId = u.RoleId,
          PublicId = u.PublicId
        }).ToList();
      }
      catch (Exception)
      {
        return new List<UserInfoDto>();
      }
    }

    public async Task<bool> CheckPasswordSignInAsync(LoginRequestDto login, string password)
    {
      try
      {
        var user = await databaseManager.Users.GetUser(login.Email);
        if (user == null) return false;
        return encryptionService.Decrypt(user.Password) == password;
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
        Console.WriteLine(ex.StackTrace);
        Console.WriteLine(ex.InnerException?.Message);
        Console.WriteLine(ex.InnerException?.StackTrace);
        return false;
      }
    }

    public async Task<UserInfoDto?> GetUser(int id)
    {
      try
      {
        var user = await databaseManager.Users.GetUser(id);
        if (user == null) return null;
        return new UserInfoDto
        {
          Id = user.Id,
          Email = user.Email,
          RoleId = user.RoleId,
          PublicId = user.PublicId
        };
      }
      catch (Exception)
      {
        return null;
      }
    }

    public async Task<UserInfoDto?> GetUser(string email)
    {
      try
      {
        var user = await databaseManager.Users.GetUser(email);
        if (user == null) return null;
        return new UserInfoDto
        {
          Id = user.Id,
          Email = user.Email,
          RoleId = user.RoleId,
          PublicId = user.PublicId
        };
      }
      catch (Exception)
      {
        return null;
      }
    }

    public async Task<UserModel?> GetUserModel(string email)
    {
      try
      {
        var user = await databaseManager.Users.GetUser(email);
        return user;
      }
      catch (Exception)
      {
        return null;
      }
    }

    public async Task<UserModel?> GetUserModel(int id)
    {
      try
      {
        var user = await databaseManager.Users.GetUser(id);
        return user;
      }
      catch (Exception)
      {
        return null;
      }
    }

    public async Task<bool> CheckIfCompanyHasAUser(string companyId)
    {
      try
      {
        var result = await databaseManager.Users.CheckIfCompanyHasAUser(companyId);
        return result;
      }
      catch (Exception)
      {
        return false;
      }
    }
  }
}
