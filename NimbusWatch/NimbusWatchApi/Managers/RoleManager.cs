using ClassLibrary.Dtos;
using NimbusWatchApi.Database;

namespace NimbusWatchApi.Managers
{
  public interface IRoleManager
  {
    Task<List<RoleDto>> GetAllRoles();
    Task<bool> AddRole(CreateRoleDto role);
    Task<bool> RemoveRole(int roleId);
    Task<bool> DoesRoleExist(CreateRoleDto role);
  }

  public class RoleManager : IRoleManager
  {
    private readonly IDatabaseManager databaseManager;

    public RoleManager(IDatabaseManager databaseManager)
    {
      this.databaseManager = databaseManager;
    }

    public async Task<bool> AddRole(CreateRoleDto role)
    {
      try
      {
        var result = await databaseManager.Roles.CreateRole(role.Name);

        return result;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<bool> DoesRoleExist(CreateRoleDto role)
    {
      try
      {
        var result = await databaseManager.Roles.DoesRoleExist(role.Name);

        return result;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<List<RoleDto>> GetAllRoles()
    {
      try
      {
        var result = await databaseManager.Roles.GetAllRoles();

        var roles = new List<RoleDto>();
        foreach (var role in result)
        {
          roles.Add(new RoleDto
          {
            Id = role.Id,
            Name = role.RoleName
          });
        }

        return roles;
      }
      catch (Exception)
      {
        return new List<RoleDto>();
      }
    }

    public async Task<bool> RemoveRole(int roleId)
    {
      try
      {
        var result = await databaseManager.Roles.DeleteRole(roleId);

        return result;
      }
      catch (Exception)
      {
        return false;
      }
    }
  }
}
