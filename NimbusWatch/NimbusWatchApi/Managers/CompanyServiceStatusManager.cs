using ClassLibrary.Dtos;
using NimbusWatchApi.Database;
using NimbusWatchApi.Models;

namespace NimbusWatchApi.Managers
{
  public interface ICompanyServiceStatusManager
  {
    public Task<List<ServerServiceStatusDto>> GetAllServiceStatusesAsync(string companyId);
    public Task<List<ServerServiceStatusDto>> GetServerServicesStatusesAsync(string companyId, int serverId);
    public Task<bool> AddServiceReportAsync(string companyId, ServiceReportDto report);
  }

  public class CompanyServiceStatusManager : ICompanyServiceStatusManager
  {
    private readonly IDatabaseManager databaseManager;

    public CompanyServiceStatusManager(IDatabaseManager databaseManager)
    {
      this.databaseManager = databaseManager;
    }

    public async Task<bool> AddServiceReportAsync(string companyId, ServiceReportDto report)
    {
      try
      {
        var result = false;
        foreach (var item in report.Statuses)
        {
          var model = new ServiceStatusesModel
          {
            ServerId = report.ServerId,
            Name = item.Name,
            Status = item.Status,
            CreatedAt = DateTime.UtcNow,
          };

          result = await databaseManager.CompanyServiceStatus.AddStatus(companyId, model);

          if (!result)
          {
            break;
          }
        }

        return result;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<List<ServerServiceStatusDto>> GetAllServiceStatusesAsync(string companyId)
    {
      try
      {
        var services = await databaseManager.CompanyServiceStatus.GetStatuses(companyId);

        var result = new List<ServerServiceStatusDto>();

        foreach (var service in services)
        {
          var dto = new ServerServiceStatusDto
          {
            Name = service.Name,
            Status = service.Status,
            CreatedAt = service.CreatedAt,
            Id = service.Id,
            ServerId = service.ServerId,
          };

          result.Add(dto);
        }

        return result;
      }
      catch (Exception)
      {
        return [];
      }
    }

    public async Task<List<ServerServiceStatusDto>> GetServerServicesStatusesAsync(string companyId, int serverId)
    {
      try
      {
        var services = await databaseManager.CompanyServiceStatus.GetStatusesByServerId(companyId, serverId);

        var result = new List<ServerServiceStatusDto>();

        foreach (var service in services)
        {
          var dto = new ServerServiceStatusDto
          {
            Name = service.Name,
            Status = service.Status,
            CreatedAt = service.CreatedAt,
            Id = service.Id,
            ServerId = service.ServerId,
          };

          result.Add(dto);
        }

        return result;
      }
      catch (Exception)
      {
        return [];
      }
    }
  }
}
