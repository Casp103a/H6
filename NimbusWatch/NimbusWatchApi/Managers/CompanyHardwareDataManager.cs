using ClassLibrary.Dtos;
using NimbusWatchApi.Database;
using NimbusWatchApi.Models;

namespace NimbusWatchApi.Managers
{
  public interface ICompanyHardwareDataManager
  {
    Task<bool> AddAsync(CompanyHardwareDataDto data);
    //Task<bool> UpdateAsync(CompanyHardwareData data);
    //Task<bool> DeleteAsync(int id);
    //Task<CompanyHardwareData?> GetByIdAsync(int id);
    Task<List<CompanyHardwareDataDto>> GetAllByCompanyIdAsync(string companyId);
    Task<List<CompanyHardwareDataDto>> GetAllByServiceIdAsync(string companyId, int serviceId);
  }

  public class CompanyHardwareDataManager : ICompanyHardwareDataManager
  {
    private readonly IDatabaseManager databaseManager;

    public CompanyHardwareDataManager(IDatabaseManager databaseManager)
    {
      this.databaseManager = databaseManager;
    }

    public async Task<bool> AddAsync(CompanyHardwareDataDto data)
    {
      try
      {
        var companyHardwareData = new CompanyHardwareData
        {
          CompanyId = data.CompanyId,
          ServiceId = data.ServiceId,
          CpuUsages = data.CpuUsages,
          DiskTotalSpace = data.DiskTotalSpace,
          DiskRemainingSpace = data.DiskRemainingSpace,
          RamUsage = data.RamUsage,
          RamTotal = data.RamTotal,
          NetworkBytesSent = data.NetworkBytesSent,
          NetworkBytesReceived = data.NetworkBytesReceived,
          CreatedAt = DateTime.UtcNow
        };

        var result = await databaseManager.CompanyHardwareData.AddCompanyHardwareData(companyHardwareData);
        return result;
      }
      catch (Exception)
      {
        return false;
      }
    }

    //public Task<bool> DeleteAsync(int id)
    //{
    //  throw new NotImplementedException();
    //}

    public async Task<List<CompanyHardwareDataDto>> GetAllByCompanyIdAsync(string companyId)
    {
      try
      {
        var result = await databaseManager.CompanyHardwareData.GetCompanyHardwareDataByCompanyId(companyId);
        return result.Select(d => new CompanyHardwareDataDto
        {
          CompanyId = d.CompanyId,
          ServiceId = d.ServiceId,
          CpuUsages = d.CpuUsages,
          DiskTotalSpace = d.DiskTotalSpace,
          DiskRemainingSpace = d.DiskRemainingSpace,
          RamUsage = d.RamUsage,
          RamTotal = d.RamTotal,
          NetworkBytesSent = d.NetworkBytesSent,
          NetworkBytesReceived = d.NetworkBytesReceived,
          CreatedAt = d.CreatedAt
        }).ToList();
      }
      catch (Exception)
      {

        return new List<CompanyHardwareDataDto>();
      }
    }

    public async Task<List<CompanyHardwareDataDto>> GetAllByServiceIdAsync(string companyId, int serviceId)
    {
      try
      {
        var result = await databaseManager.CompanyHardwareData.GetCompanyHardwareDataByServiceId(companyId, serviceId);
        return result.Select(d => new CompanyHardwareDataDto
        {
          CompanyId = d.CompanyId,
          ServiceId = d.ServiceId,
          CpuUsages = d.CpuUsages,
          DiskTotalSpace = d.DiskTotalSpace,
          DiskRemainingSpace = d.DiskRemainingSpace,
          RamUsage = d.RamUsage,
          RamTotal = d.RamTotal,
          NetworkBytesSent = d.NetworkBytesSent,
          NetworkBytesReceived = d.NetworkBytesReceived,
          CreatedAt = d.CreatedAt
        }).ToList();
      }
      catch (Exception)
      {
        return new List<CompanyHardwareDataDto>();
      }
    }

    //public Task<CompanyHardwareData?> GetByIdAsync(int id)
    //{
    //  throw new NotImplementedException();
    //}

    //public Task<bool> UpdateAsync(CompanyHardwareData data)
    //{
    //  throw new NotImplementedException();
    //}
  }
}
