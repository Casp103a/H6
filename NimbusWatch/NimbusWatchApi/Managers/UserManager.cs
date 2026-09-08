using ClassLibrary.Dtos;
using NimbusWatchApi.Database;
using NimbusWatchApi.Models;
using NimbusWatchApi.Services;

namespace NimbusWatchApi.Managers
{
  public interface IUserManager
  {
    Task<bool?> CreateAsync(RegisterUserDto user);
    Task<bool?> UpdateAsync(UserModel user);
    Task<bool?> DeleteAsync(UserModel user);
    Task<List<UserModel>> GetAllUsers();
    Task<UserModel?> GetUser(int id);
    Task<UserModel?> GetUser(string email);
    Task<bool> CheckPasswordSignInAsync(LoginRequestDto login, string password);
  }

  internal class UserManager : IUserManager
  {
    private readonly IDatabaseManager databaseManager;
    private readonly IPasswordService passwordService;

    public UserManager(IDatabaseManager databaseManager, IPasswordService passwordService)
    {
      this.databaseManager = databaseManager;
      this.passwordService = passwordService;
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
          Password = passwordService.Encrypt(userDto.Password),
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

    public async Task<bool?> UpdateAsync(UserModel? user)
    {
      try
      {
        if (user == null || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Password))
        {
          return null;
        }

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

    public async Task<List<UserModel>> GetAllUsers()
    {
      try
      {
        return await databaseManager.Users.GetAllUsers();
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
        return passwordService.Decrypt(user.Password) == password;
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
