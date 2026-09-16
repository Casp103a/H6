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
    Task<List<UserModel>> GetAllUsers(string companyId);
    Task<UserModel?> GetUser(int id);
    Task<UserModel?> GetUser(string email);
    Task<bool> CheckPasswordSignInAsync(LoginRequestDto login, string password);
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
          CompanyId = userDto.CompanyId
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

    public async Task<List<UserModel>> GetAllUsers(string companyId)
    {
      try
      {
        return await databaseManager.Users.GetAllUsers(companyId);
      }
      catch (Exception)
      {
        return new List<UserModel>();
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

    public async Task<UserModel?> GetUser(int id)
    {
      try
      {
        var user = await databaseManager.Users.GetUser(id);
        return user;
      }
      catch (Exception)
      {
        throw;
      }
    }

    public async Task<UserModel?> GetUser(string email)
    {
      try
      {
        var user = await databaseManager.Users.GetUser(email);
        return user;
      }
      catch (Exception)
      {
        throw;
      }
    }
  }
}
