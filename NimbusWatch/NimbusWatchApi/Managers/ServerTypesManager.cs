using ClassLibrary.Dtos;
using NimbusWatchApi.Database;

namespace NimbusWatchApi.Managers
{
  public interface IServerTypesManager
  {
    Task<List<ServerTypesDto>> GetAllServerTypes();
    Task<bool> AddServerType(ServerTypesDto serverType);
    Task<bool> RemoveServerType(int serverTypeId);
    Task<bool> DoesServerTypeExist(ServerTypesDto serverType);
    Task<ServerTypesDto?> GetServerType(int serverTypeId);
  }

  public class ServerTypesManager : IServerTypesManager
  {
    private readonly IDatabaseManager databaseManager;

    public ServerTypesManager(IDatabaseManager databaseManager)
    {
      this.databaseManager = databaseManager;
    }

    public async Task<bool> AddServerType(ServerTypesDto serverType)
    {
      try
      {
        var result = await databaseManager.ServerTypes.CreateServerType(serverType.ServerTypeName);

        return result;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<bool> DoesServerTypeExist(ServerTypesDto serverType)
    {
      try
      {
        var result = await databaseManager.ServerTypes.DoesServerTypeExist(serverType.ServerTypeName);

        return result;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<List<ServerTypesDto>> GetAllServerTypes()
    {
      try
      {
        var result = await databaseManager.ServerTypes.GetAllServerTypes();

        var serverTypes = new List<ServerTypesDto>();
        foreach (var serverType in result)
        {
          serverTypes.Add(new ServerTypesDto
          {
            Id = serverType.Id,
            ServerTypeName = serverType.ServerTypeName
          });
        }

        return serverTypes;
      }
      catch (Exception)
      {
        return new List<ServerTypesDto>();
      }
    }

    public async Task<ServerTypesDto?> GetServerType(int serverTypeId)
    {
      try
      {
        var serverType = await databaseManager.ServerTypes.GetServerTypeById(serverTypeId);

        if (serverType == null)
          return null;

        return new ServerTypesDto
        {
          Id = serverType.Id,
          ServerTypeName = serverType.ServerTypeName
        };
      }
      catch (Exception)
      {
        return null;
      }
    }

    public async Task<bool> RemoveServerType(int serverTypeId)
    {
      try
      {
        var result = await databaseManager.ServerTypes.DeleteServerType(serverTypeId);

        return result;
      }
      catch (Exception)
      {
        return false;
      }
    }
  }
}
